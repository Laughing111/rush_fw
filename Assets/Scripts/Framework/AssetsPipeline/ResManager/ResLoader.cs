using System;
using System.Collections.Generic;
using AssetBundleDataConfig;
using UnityEngine;
using UnityEngine.U2D;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;

namespace Game.Runtime
{

    public class CacheObject:IReusableClass
    {
        public uint Crc;

        public string Path;

        public int InstanceId;

        public GameObject Obj;

        public void Release()
        {
            Crc = 0;
            InstanceId = 0;
            Path = null;
            Obj = null;
        }

        public uint MaxStore => 300;
        public void ReSet()
        {
            Release();
        }
    }

    /// <summary>
    /// 加载回调类
    /// </summary>
    public class LoadObjectCallBack:IReusableClass
    {
        public string Path;
        public uint Crc;
        public Object Param1;
        public Object Param2;
        public Action<GameObject, Object, Object> OnCompleted;

        public uint MaxStore => 300;
        public void ReSet()
        {
            Path = null;
            Crc = 0;
            Param1 = null;
            Param2 = null;
            OnCompleted = null;
        }
    }
    public class ResLoader:IResLoader
    {
        /// <summary>
        /// 已经加载过的资源池
        /// key 资源路径crc
        /// value 资源对象
        /// </summary>
        private Dictionary<uint, BundleItem> alreadyLoadedAssetsMap = new Dictionary<uint, BundleItem>();

        /// <summary>
        /// 实例对象池
        /// </summary>
        private Dictionary<uint, List<CacheObject>> ObjectInstanceCacheMap = new Dictionary<uint, List<CacheObject>>();

        
        /// <summary>
        /// 所有对象的缓存池
        /// </summary>
        private Dictionary<int, CacheObject> allObjectInstanceCacheMap = new Dictionary<int, CacheObject>();

        /// <summary>
        /// 异步加载的任务列表
        /// </summary>
        private List<long> asyncLoadingTaskList = new List<long>();

        /// <summary>
        /// 异步加载的唯一id
        /// </summary>
        private long asyncGuid;

        private long asyncTaskGuid
        {
            get
            {
                if (asyncGuid > long.MaxValue)
                {
                    asyncGuid = 0;
                }

                return asyncGuid++;
            }
        }

        /// <summary>
        /// 加载对象的回调
        /// </summary>
        private Dictionary<long, LoadObjectCallBack> loadObjectCallBackDict = new();

        /// <summary>
        /// 等待加载的资源列表
        /// </summary>
        private List<HotPatchInfo> waitLoadAssetsList = new List<HotPatchInfo>();

        /// <summary>
        /// 预制件后缀
        /// </summary>
        private const string PrefabExt = ".prefab";

        public void Init()
        {
            HotUpdateController.OnDownloadBundleFinished += OnAssetsDownloadFinished;
        }

        /// <summary>
        /// 预加载并实例化GameObject
        /// </summary>
        /// <param name="path"></param>
        /// <param name="count"></param>
        public void PreLoadObj(string path, int count = 1)
        {
            var lst = ListPool.Get<GameObject>();
            for (int i = 0; i < count; i++)
            {
                var objIns = Instantiate(path, null, Vector3.zero, Vector3.one, Quaternion.identity);
                lst.Add(objIns);
            }

            //回收到对象池
            foreach (var obj in lst)
            {
                Release(obj);
            }
            ListPool.Put(lst);
        }

        /// <summary>
        /// 预加载资源
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        public void PreLoadRes<T>(string path) where T : Object
        {
            LoadRes<T>(path);
        }

        /// <summary>
        /// 移除对象加载回调
        /// </summary>
        /// <param name="loadId"></param>
        public void RemoveObjectLoadCallBack(long loadId)
        {
            if (loadId == -1) return;

            if (loadObjectCallBackDict.TryGetValue(loadId,out var cb))
            {
                ClassPool.Put(cb);
                loadObjectCallBackDict.Remove(loadId);
            }
        }

        /// <summary>
        /// 释放对象占用内存
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="destroy"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Release(GameObject obj, bool destroy = false)
        {
            int instanceId = obj.GetInstanceID();
            //对象池中没记录，说明不是用API创建
            if(!allObjectInstanceCacheMap.TryGetValue(instanceId,out CacheObject cache))
            {
                Debug.LogErrorFormat("Release Obj Failed! Obj may instantiate by GameObject.Instantiate..");
                return;
            }

            if (destroy)
            {
                GameObject.Destroy(obj);
                if (allObjectInstanceCacheMap.ContainsKey(instanceId))
                {
                    allObjectInstanceCacheMap.Remove(instanceId);
                }

                //获取该物体所在的对象池
                if (ObjectInstanceCacheMap.TryGetValue(cache.Crc, out var cacheLst) && cacheLst != null && cacheLst.Count > 0)
                {
                    if (cacheLst.Contains(cache))
                    {
                        cacheLst.Remove(cache);
                    }
                    
                    
                }
                //如果该对象池不存在 或已经全部释放了 就卸载该对象的AB包
                else
                {
                    if (alreadyLoadedAssetsMap.TryGetValue(cache.Crc, out var bundleItem))
                    {
                        AssetBundleManager.Instance.ReleaseAssets(bundleItem,true);
                    }
                    else
                    {
                        Debug.LogErrorFormat("already load asset can not found bundleItem,{0}",cache.Path);
                    }
                }
                
                ClassPool.Put(cache);
            }
            else
            {
                //回收到对象池
                //获取该物体所在的对象池
                if (!ObjectInstanceCacheMap.TryGetValue(cache.Crc, out var cacheLst))
                {
                    cacheLst = new List<CacheObject>();
                    ObjectInstanceCacheMap.Add(cache.Crc,cacheLst);
                }
                cacheLst.Add(cache);
                //回收mono对象到指定节点下
                if (cache.Obj != null)
                {
                    cache.Obj.transform.SetParent(AssetsPipeLine.Instance.CacheRoot);
                }
                else
                {
                    Debug.LogErrorFormat("cache obj is null,release failed,{0}",cache.Path);
                }
            }
            
        }

        
        /// <summary>
        /// 释放图片所占用的资源
        /// </summary>
        /// <param name="texture"></param>
        public void Release(Texture texture)
        {
            Resources.UnloadAsset(texture);
        }

        /// <summary>
        /// 加载图片资源
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Sprite LoadSprite(string path)
        {
            if (!path.EndsWith(".png"))
            {
                path += ".png";
            }

            return LoadRes<Sprite>(path);
        }

        /// <summary>
        /// 加载Texture
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Texture LoadTexture(string path)
        {
            if (!path.EndsWith(".jpg"))
            {
                path += ".jpg";
            }

            return LoadRes<Texture>(path);
        }

        /// <summary>
        /// 加载Text资源文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public TextAsset LoadTextAsset(string path)
        {
            return LoadRes<TextAsset>(path);
        }

        /// <summary>
        /// 从图集中加载指定名称的图片
        /// </summary>
        /// <param name="atlasPath"></param>
        /// <param name="spriteName"></param>
        /// <returns></returns>
        public Sprite LoadAtlasSprite(string atlasPath, string spriteName)
        {
            if (!atlasPath.Contains(".spriteatlas"))
            {
                atlasPath += ".spriteatlas";
            }

            var atlas = LoadRes<SpriteAtlas>(atlasPath);
            if (atlas == null)
            {
                Debug.LogErrorFormat("atlas load failed,{0}",atlasPath);
                return null;
            }

            return LoadSpriteFromAtlas(atlas, spriteName);
        }

        /// <summary>
        /// 从图集中加载指定名称的图片
        /// </summary>
        /// <param name="atlas"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private Sprite LoadSpriteFromAtlas(SpriteAtlas atlas,string name)
        {
            Sprite sprite = atlas.GetSprite(name);
            if (sprite == null)
            {
                Debug.LogErrorFormat("Load Sprite From Atlas Failed,{0}",name);
                return null;
            }

            return sprite;
        }

        /// <summary>
        /// 异步加载图片
        /// </summary>
        /// <param name="path"></param>
        /// <param name="onLoadCompleted"></param>
        /// <param name="param1"></param>
        /// <returns></returns>
        public long LoadTextureAsync(string path, Action<Texture, Object> onLoadCompleted, Object param1)
        {
            if (!path.EndsWith(".jpg"))
            {
                path += ".jpg";
            }

            long guid = asyncTaskGuid;
            asyncLoadingTaskList.Add(guid);
            LoadResAsync<Texture>(path, (obj) =>
            {
                if (asyncLoadingTaskList.Contains(guid))
                {
                    asyncLoadingTaskList.Remove(guid);
                    onLoadCompleted?.Invoke(obj,param1);
                }
                if (obj == null)
                {
                    Debug.LogErrorFormat("Load Texture Async Failed,{0}",path);   
                }
            });
            return guid;
        }

        /// <summary>
        /// 异步加载Sprite
        /// </summary>
        /// <param name="path"></param>
        /// <param name="image"></param>
        /// <param name="setNativeSize"></param>
        /// <param name="onLoadCompleted"></param>
        /// <returns></returns>
        public long LoadSpriteAsync(string path, Image image, bool setNativeSize = false, Action<Sprite> onLoadCompleted = null)
        {
            if (!path.EndsWith(".png"))
            {
                path += ".png";
            }

            long guid = asyncTaskGuid;
            asyncLoadingTaskList.Add(guid);
            LoadResAsync<Sprite>(path, (obj) =>
            {
                if (asyncLoadingTaskList.Contains(guid))
                {
                    asyncLoadingTaskList.Remove(guid);

                    if (obj != null && image != null)
                    {
                        image.sprite = obj;
                        if (setNativeSize)
                        {
                            image.SetNativeSize();
                        }
                    }
                    else
                    {
                        Debug.LogErrorFormat("Load Texture Async Failed,{0}",path);   
                    }
                    
                    onLoadCompleted?.Invoke(obj);
                }
            });
            return guid;
        }

        /// <summary>
        /// 清理所有异步加载任务
        /// </summary>
        public void ClearAllAsyncLoadTask()
        {
            asyncLoadingTaskList.Clear();
        }

        /// <summary>
        /// 清理加载的资源，释放内存
        /// </summary>
        /// <param name="absoluteClean">深度清理，true:消耗所有由AB包加载和生成的对象，彻底释放内存占用
        /// false: 销毁对象池中的对象，但不销毁由AB包克隆出的正在使用的对象，具体的内存释放根据引用计数，选择性释放</param>
        public void ClearResourcesAssets(bool absoluteClean)
        {
            if (absoluteClean)
            {
                foreach (var item in allObjectInstanceCacheMap)
                {
                    if (item.Value.Obj != null)
                    {
                        //消耗OBJ
                        GameObject.Destroy(item.Value.Obj);
                        //回收
                        ClassPool.Put(item.Value);
                    }
                }
                allObjectInstanceCacheMap.Clear();
                ObjectInstanceCacheMap.Clear();
                ClearAllAsyncLoadTask();
            }
            else
            {
                foreach (var objList in ObjectInstanceCacheMap.Values)
                {
                    if (objList != null && objList.Count > 0)
                    {
                        foreach (var cacheItem in objList)
                        {
                            if (cacheItem != null)
                            {
                                GameObject.Destroy(cacheItem.Obj);
                                //回收
                                ClassPool.Put(cacheItem);
                            }
                        }
                    }
                }
            
                ObjectInstanceCacheMap.Clear();
            }


            //释放AB包占用的内存
            foreach (var item in alreadyLoadedAssetsMap)
            {
                AssetBundleManager.Instance.ReleaseAssets(item.Value,absoluteClean);
            }

            
            //清理列表
            foreach (var cbItme in loadObjectCallBackDict)
            {
                ClassPool.Put(cbItme.Value);
            }
            loadObjectCallBackDict.Clear();
            alreadyLoadedAssetsMap.Clear();

            //释放未使用的资源（未使用的资源是指没有被引用的资源）
            Resources.UnloadUnusedAssets();
            //触发GC
            System.GC.Collect();
        }

        #region 对象加载

        /// <summary>
        /// 资源下载完成的回调
        /// </summary>
        /// <param name="info"></param>
        private void OnAssetsDownloadFinished(HotPatchInfo info)
        {
            Debug.LogFormat("ResMgr AssetDownLoadFinish:{0}",info.ABName);
            //处理比AssetBundle配置 先下载下来的AB加载
            if (info.ABName.Contains(HotUpdateDefine.ABConfigTag))
            {
                //AB配置包下载成功了
                Debug.LogFormat("Handle waitloadList Count: {0}",waitLoadAssetsList.Count);
                var infoArry = waitLoadAssetsList.ToArray();
                waitLoadAssetsList.Clear();
                foreach (var infoItem in infoArry)
                {
                    OnAssetsDownloadFinished(infoItem);
                }

                return;
            }
            
            if (loadObjectCallBackDict.Count <= 0)
            {
                return;
            }
            
            var assetsItemList = ListPool.Get<BundleItem>();
            AssetBundleManager.Instance.GetBundleItemByABName(info.ABName, assetsItemList);
            //如果配置文件未加载
            //(多线程下载，有可能AB资源包 比 AB配置包 下载速度快)
            if (assetsItemList.Count <= 0)
            {
                for (int i = 0; i < waitLoadAssetsList.Count; i++)
                {
                    //去重
                    if (waitLoadAssetsList[i].ABName == info.ABName)
                    {
                        return;
                    }
                }
                    
                waitLoadAssetsList.Add(info);
                return;
            }

            var removeLst = ListPool.Get<long>();
            //遍历对象加载回调 触发资源加载
            foreach (var cbItem in loadObjectCallBackDict)
            {
                //等待回调中的资源 是否在列表中
                if (ListContainsAsset(assetsItemList, cbItem.Value.Crc))
                {
                    Debug.LogFormat("ResMgr Asset Download Finish!,load obj path {0}",cbItem.Value.Path);

                    var objIns = Instantiate(cbItem.Value.Path,null,Vector3.zero,Vector3.one, Quaternion.identity,cbItem.Value.Crc);
                    
                    cbItem.Value.OnCompleted?.Invoke(objIns,cbItem.Value.Param1,cbItem.Value.Param2);
                    
                    removeLst.Add(cbItem.Key);
                }
            }
            
            ListPool.Put(assetsItemList);

            //移除回调
            foreach (var loadId in removeLst)
            {
                var cb = loadObjectCallBackDict[loadId];
                ClassPool.Put(cb);
                loadObjectCallBackDict.Remove(loadId);
                
            }
            ListPool.Put(removeLst);
        }

        private bool ListContainsAsset(List<BundleItem> assetsItemLst,uint crc)
        {
            foreach (var item in assetsItemLst)
            {
                if (item.Crc == crc)
                {
                    return true;
                }
            }

            return false;
        } 

        /// <summary>
        /// 同步克隆GameObj
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parent"></param>
        /// <param name="localPos"></param>
        /// <param name="localScale"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public GameObject Instantiate(string path, Transform parent, Vector3 localPos, Vector3 localScale, Quaternion rotation, uint crc = 0)
        {
            path = path.EndsWith(PrefabExt) ? path : $"{path}{PrefabExt}";
            if (crc <= 0)
            {
                crc = Crc32.GetCrc32(path);
            }
            //优先从对象池中查询
            var cacheObjIns = GetCacheObjFromPools(crc);
            if (cacheObjIns != null)
            {
                cacheObjIns.transform.SetParent(parent);
                cacheObjIns.transform.localPosition = localPos;
                cacheObjIns.transform.localScale = localScale;
                cacheObjIns.transform.rotation = rotation;
                return cacheObjIns;
            }
            
            //加载该对象
            var obj = LoadRes<GameObject>(path);
            if (obj != null)
            {
                cacheObjIns = InternalInstantiate(crc,path,obj,parent);
                cacheObjIns.transform.localPosition = localPos;
                cacheObjIns.transform.localScale = localScale;
                cacheObjIns.transform.rotation = rotation;
                return cacheObjIns;
            }
            else
            {
                Debug.LogErrorFormat("GameObj Load Failed!,{0}",path);
                return null;
            }

        }

        
        /// <summary>
        /// 异步克隆GameObject
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parent"></param>
        /// <param name="onCompleted"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        public void InstantiateAsync(string path,Action<GameObject,Object,Object> onCompleted,Object param1 = null,Object param2 = null)
        {
            path = path.EndsWith(PrefabExt) ? path : $"{path}{PrefabExt}";
            var crc = Crc32.GetCrc32(path);
            //优先从对象池中查询
            var cacheObjIns = GetCacheObjFromPools(crc);
            if (cacheObjIns != null)
            {
                onCompleted?.Invoke(cacheObjIns,param1,param2);
                return;
            }
            
            //获取异步加载唯一id
            var guid = asyncTaskGuid;
            asyncLoadingTaskList.Add(guid);
            
            //开始异步加载资源
            LoadResAsync<GameObject>(path, (objAsset) =>
            {
                if (objAsset != null)
                {
                    if (asyncLoadingTaskList.Contains(guid))
                    {
                        asyncLoadingTaskList.Remove(guid);
                        var insObj = InternalInstantiate(crc, path, objAsset, null);
                        onCompleted?.Invoke(insObj,param1,param2);
                    }
                }
                else
                {
                    asyncLoadingTaskList.Remove(guid);
                    Debug.LogErrorFormat("Async load GameObj is null,path:{0}",path);
                    onCompleted?.Invoke(null,param1,param2);
                }
            });
        }


        /// <summary>
        ///  实例化资源 或者 等待加载后实例化
        /// </summary>
        /// <param name="path"></param>
        /// <param name="onInstantiateCompleted"></param>
        /// <param name="onLoading"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <returns></returns>
        public long InstantiateAndLoad(string path, Action<GameObject, Object, Object> onInstantiateCompleted,Action onLoading, Object param1 = null, Object param2 = null)
        {
            path = path.EndsWith(PrefabExt) ? path : $"{path}{PrefabExt}";
            var crc = Crc32.GetCrc32(path);
            //优先从对象池中查询
            var cacheObjIns = GetCacheObjFromPools(crc);
            long loadId = -1;
            if (cacheObjIns != null)
            {
                onInstantiateCompleted?.Invoke(cacheObjIns,param1,param2);
            }

            var objIns = Instantiate(path, null, Vector3.zero, Vector3.one, Quaternion.identity, crc);
            if (objIns != null)
            {
                onInstantiateCompleted?.Invoke(objIns,param1,param2);
            }
            else
            {
                //资源没有下载完成 本地没有这个资源
                loadId = asyncTaskGuid;
                onLoading?.Invoke();
                var callBack = ClassPool.Get<LoadObjectCallBack>();
                callBack.Path = path;
                callBack.Crc = crc;
                callBack.Param1 = param1;
                callBack.Param2 = param2;
                callBack.OnCompleted = onInstantiateCompleted;
                loadObjectCallBackDict.Add(loadId,callBack);
            }

            return loadId;
        }
        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="crc"></param>
        /// <param name="path"></param>
        /// <param name="asset"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private GameObject InternalInstantiate(uint crc,string path,GameObject asset,Transform parent)
        {
            var ins = Object.Instantiate(asset,parent,false);

            var cache = ClassPool.Get<CacheObject>();
            cache.Obj = ins;
            cache.Path = path;
            cache.Crc = crc;
            cache.InstanceId = ins.GetInstanceID();
            
            allObjectInstanceCacheMap.Add(cache.InstanceId,cache);
            return ins;
        }
        
        
        /// <summary>
        /// 从对象池中取出对象
        /// </summary>
        /// <param name="crc"></param>
        /// <returns></returns>
        private GameObject GetCacheObjFromPools(uint crc)
        {
            if (ObjectInstanceCacheMap.TryGetValue(crc, out var objList))
            {
                if (objList != null && objList.Count > 0)
                {
                    var cache = objList[0];
                    objList.RemoveAt(0);
                    var obj = cache.Obj;
                    ClassPool.Put(cache);
                    return obj;
                }
            }

            return null;
        }

        #endregion
        
        
        #region 资源加载

        /// <summary>
        /// 同步加载资源，外部直接调用，仅加载不需要实例化的资源
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T LoadRes<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogErrorFormat("load fail,path is null!");
                return null;
            }

            var crc = Crc32.GetCrc32(path);
            //从缓存中获取bundleItem
            BundleItem item = GetCacheItem(crc);

            //如果Item中的资源对象已经加载
            if (item.AssetObject != null)
            {
                return item.AssetObject as T;
            }

            T obj = null;
#if UNITY_EDITOR
            if (ABSettings.Ins.AssetLoadMode == LoadAssetMode.Editor)
            {
                obj = LoadResForEditor<T>(path);
            }
#endif
            if (obj == null)
            {
                //加载资源对应的AB包
                item = AssetBundleManager.Instance.LoadAssetBundle(crc);
                if (item == null)
                {
                    Debug.LogErrorFormat("Asset Item is null,{0}",path);
                    return null;
                }

                if (item.Bundle != null)
                {
                    if (item.AssetObject == null)
                    {
                        obj = item.Bundle.LoadAsset<T>(item.AssetName);
                    }
                    else
                    {
                        obj = item.AssetObject as T;
                    }
                }
                else
                {
                    Debug.LogErrorFormat("AssetBundle is null-{0}",item.BundleName);
                    return null;
                }
            }
            
            item.AssetObject = obj;
            item.Path = path;
            item.Crc = crc;
            //缓存
            alreadyLoadedAssetsMap.Add(crc,item);
            return obj;
        }
        
        
        /// <summary>
        /// 异步加载资源,外部直接调用，仅加载不需要实例化的资源
        /// </summary>
        /// <param name="path"></param>
        /// <param name="onCompleted"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public void LoadResAsync<T>(string path,Action<T> onCompleted) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogErrorFormat("load fail,path is null!");
                onCompleted?.Invoke(null);
                return;
            }

            var crc = Crc32.GetCrc32(path);
            //从缓存中获取bundleItem
            BundleItem item = GetCacheItem(crc);

            //如果Item中的资源对象已经加载
            if (item.AssetObject != null)
            {
                onCompleted?.Invoke(item.AssetObject as T);
                return;
            }

            T obj = null;
#if UNITY_EDITOR
            if (ABSettings.Ins.AssetLoadMode == LoadAssetMode.Editor)
            {
                obj = LoadResForEditor<T>(path);
                onCompleted?.Invoke(obj);
            }
#endif
            if (obj == null)
            {
                //加载资源对应的AB包
                item = AssetBundleManager.Instance.LoadAssetBundle(crc);
                if (item == null)
                {
                    Debug.LogErrorFormat("Asset Item is null,{0}",path);
                    onCompleted?.Invoke(null);
                    return;
                }

                if (item.Bundle != null)
                {
                    if (item.AssetObject != null)
                    {
                        obj = item.AssetObject as T;
                        item.Path = path;
                        item.Crc = crc;
                        alreadyLoadedAssetsMap.Add(crc,item);
                        onCompleted?.Invoke(obj);
                    }
                    else
                    {
                        //AB包异步加载资源
                        AssetBundleRequest bundleRequest = item.Bundle.LoadAssetAsync<T>(item.AssetName);
                        bundleRequest.completed += (asyncOption) =>
                        {
                            //资源加载完成
                            T loadObj = (asyncOption as AssetBundleRequest)?.asset as T;
                            item.AssetObject = loadObj;
                            item.Path = path;
                            item.Crc = crc;
                            if (!alreadyLoadedAssetsMap.ContainsKey(crc))
                            {
                                alreadyLoadedAssetsMap.Add(crc,item);
                            }
                            onCompleted?.Invoke(loadObj);
                        };
                    }
                }
                else
                {
                    Debug.LogErrorFormat("AssetBundle is null-{0}",item.BundleName);
                    onCompleted?.Invoke(null);
                    return;
                }
            }
            
            item.AssetObject = obj;
            item.Path = path;
            item.Crc = crc;
            //缓存
            alreadyLoadedAssetsMap.Add(crc,item);
        }



        /// <summary>
        /// 从缓存中获取bundleItem
        /// </summary>
        /// <param name="crc"></param>
        /// <returns></returns>
        private BundleItem GetCacheItem(uint crc)
        {
            if (alreadyLoadedAssetsMap.TryGetValue(crc, out var item))
            {
                return item;
            }

            item = new BundleItem()
            {
                Crc = crc
            };

            return item;
        }


#if UNITY_EDITOR
        /// <summary>
        /// 编辑器下加载
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T LoadResForEditor<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogErrorFormat("load fail,path is null!");
                return null;
            }

            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }
#endif

        #endregion
        
    }
}

