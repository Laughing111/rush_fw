using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;
using UnityEngine.UI;

namespace Game.Runtime
{
    public class AssetsPipeLine : SingletonMono<AssetsPipeLine>
    {
        //缓存节点
        public Transform CacheRoot
        {
            get;
            set;
        }
        
        private IHotAssetControl hotUpdateCtrl;

        private BaseDecompress decompressCtrl;

        private IResLoader resLoader;

        /// <summary>
        /// 初始化资源热更
        /// </summary>
        public override void Init()
        {
            base.Init();
            CacheRoot = new GameObject("CacheRoot").transform;
            DontDestroyOnLoad(CacheRoot);
            hotUpdateCtrl = new HotUpdateController(this);
            decompressCtrl = new BundleDecompressController();
            resLoader = new ResLoader();
            resLoader.Init();
        }

        void Update()
        {
            hotUpdateCtrl?.OnMainThreadUpdate();
        }
        
        /// <summary>
        /// 执行热更
        /// </summary>
        /// <param name="moduleName">热更模块名</param>
        /// <param name="onStartUpdate">开始热更</param>
        /// <param name="onUpdateFinish">热更结束</param>
        /// <param name="waitUpdate">等待热更</param>
        /// <param name="validVersion">是否校验资源版本</param>
        public void HotUpdateAsset(string moduleName, Action<string> onStartUpdate, Action<string> onUpdateFinish, Action<string> waitUpdate, bool validVersion = true)
        {
            this.hotUpdateCtrl.HotUpdateAsset(moduleName,onStartUpdate,onUpdateFinish,waitUpdate,validVersion);
        }

        /// <summary>
        /// 校验资源版本
        /// </summary>
        /// <param name="moduleName">热更模块名</param>
        /// <param name="validCallBack">校验回调，是否需要热更，热更资源的大小</param>
        public void ValidAssetsVersion(string moduleName, Action<bool, float> validCallBack)
        {
            this.hotUpdateCtrl.ValidAssetsVersion(moduleName,validCallBack);
        }

        /// <summary>
        /// 获取热更资源模块
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public HotAssetModule GetHotAssetModule(string moduleName)
        {
            return this.hotUpdateCtrl.GetHotAssetModule(moduleName);
        }


        /// <summary>
        /// 开始解压内嵌资源补丁
        /// </summary>
        /// <returns></returns>
        public BaseDecompress StartDecompressBuiltinPatch(string moduleName, Action onCompleted)
        {
            return decompressCtrl.StartDecompressBuiltinPatch(moduleName, onCompleted);
        }

        /// <summary>
        /// 获取解压进度
        /// </summary>
        /// <returns></returns>
        public float GetDecompressProgress()
        {
            return decompressCtrl.GetDecompressProgress();
        }
        
        /// <summary>
        /// 预加载并实例化GameObject
        /// </summary>
        /// <param name="path"></param>
        /// <param name="count"></param>
        public void PreLoadObj(string path, int count = 1)
        {
            resLoader.PreLoadObj(path,count);
        }

        /// <summary>
        /// 预加载资源
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        public void PreLoadRes<T>(string path) where T : Object
        {
            resLoader.PreLoadRes<T>(path);
        }

        /// <summary>
        /// 移除对象加载回调
        /// </summary>
        /// <param name="loadId"></param>
        public void RemoveObjectLoadCallBack(long loadId)
        {
            resLoader.RemoveObjectLoadCallBack(loadId);
        }

        /// <summary>
        /// 释放对象占用内存
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="destroy"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Release(GameObject obj, bool destroy = false)
        {
            resLoader.Release(obj,destroy);
            
        }

        
        /// <summary>
        /// 释放图片所占用的资源
        /// </summary>
        /// <param name="texture"></param>
        public void Release(Texture texture)
        {
            resLoader.Release(texture);
        }

        /// <summary>
        /// 加载图片资源
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Sprite LoadSprite(string path)
        {
            return resLoader.LoadSprite(path);
        }

        /// <summary>
        /// 加载Texture
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Texture LoadTexture(string path)
        {
            return resLoader.LoadTexture(path);
        }

        /// <summary>
        /// 加载Text资源文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public TextAsset LoadTextAsset(string path)
        {
            return resLoader.LoadTextAsset(path);
        }

        /// <summary>
        /// 从图集中加载指定名称的图片
        /// </summary>
        /// <param name="atlasPath"></param>
        /// <param name="spriteName"></param>
        /// <returns></returns>
        public Sprite LoadAtlasSprite(string atlasPath, string spriteName)
        {
            return resLoader.LoadAtlasSprite(atlasPath, spriteName);
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
            return resLoader.LoadTextureAsync(path,onLoadCompleted,param1);
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
            return resLoader.LoadSpriteAsync(path,image,setNativeSize,onLoadCompleted);
        }

        /// <summary>
        /// 清理所有异步加载任务
        /// </summary>
        public void ClearAllAsyncLoadTask()
        {
            resLoader.ClearAllAsyncLoadTask();
        }

        /// <summary>
        /// 清理加载的资源，释放内存
        /// </summary>
        /// <param name="absoluteClean">深度清理，true:消耗所有由AB包加载和生成的对象，彻底释放内存占用
        /// false: 销毁对象池中的对象，但不销毁由AB包克隆出的正在使用的对象，具体的内存释放根据引用计数，选择性释放</param>
        public void ClearResourcesAssets(bool absoluteClean)
        {
            resLoader.ClearResourcesAssets(absoluteClean);
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
            return resLoader.Instantiate(path, parent, localPos, localScale, rotation, crc);
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
            resLoader.InstantiateAsync(path, onCompleted, param1, param2);
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
            return resLoader.InstantiateAndLoad(path, onInstantiateCompleted,onLoading, param1, param2);
        }
    }
}

