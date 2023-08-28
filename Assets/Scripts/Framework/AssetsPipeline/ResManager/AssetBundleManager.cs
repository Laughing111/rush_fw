using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AssetBundleDataConfig;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Game.Runtime
{

    public class BundleItem
    {
        /// <summary>
        /// 加载路径
        /// </summary>
        public string Path;
        public uint Crc;
        /// <summary>
        /// AB包名
        /// </summary>
        public string BundleName;
        /// <summary>
        /// 资源名次
        /// </summary>
        public string AssetName;
        /// <summary>
        /// 所属模块名
        /// </summary>
        public string ModuleName;
        /// <summary>
        /// 依赖项
        /// </summary>
        public List<string> BundleDependence;
        /// <summary>
        /// Bundle对象
        /// </summary>
        public AssetBundle Bundle;
        /// <summary>
        /// 通过Bundle加载出来的资源对象
        /// </summary>
        public UnityEngine.Object AssetObject;
    }
    
    /// <summary>
    /// AB包缓存
    /// </summary>
    public class AssetBundleCache: IReusableClass
    {
        /// <summary>
        /// 引用计数
        /// </summary>
        public int ReferenceCount;

        /// <summary>
        /// AB包
        /// </summary>
        public AssetBundle Bundle;

        public void Release()
        {
            Bundle = null;
            ReferenceCount = 0;
        }

        public uint MaxStore => 200;
        public void ReSet()
        {
            Release();
        }
    }
    public class AssetBundleManager : Singleton<AssetBundleManager>
    {
        /// <summary>
        /// 当前模块的配置AB文件名
        /// </summary>
        private string bundleConfigFileName;
        
        /// <summary>
        /// 当前模块的配置AB的加载路径
        /// </summary>
        private string bundleConfigPath;

        /// <summary>
        /// 所有模块的Bundle资源池
        /// </summary>
        private Dictionary<uint, BundleItem> allBundleAssetMap = new Dictionary<uint, BundleItem>();

        /// <summary>
        /// 已经加载过的AB
        /// </summary>
        private Dictionary<string, AssetBundleCache> alreadyLoadedBundlesMap = new Dictionary<string, AssetBundleCache>();

        /// <summary>
        /// 获取配置AB的路径
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        private bool GetBundleConfigPath(string moduleName)
        {
            //获取当前模块的配置文件所在路径
            bundleConfigFileName = HotUpdateDefine.GetBundleConfigFileNameWithoutExtension(moduleName);
            bundleConfigPath = $"{HotUpdateDefine.GetPersistHotAssetsPath(moduleName)}{bundleConfigFileName}{HotUpdateDefine.BundleExtension}";

            if (!File.Exists(bundleConfigPath))
            {
                //如果热更目录不存在，去内嵌解压目录找
                bundleConfigPath = $"{HotUpdateDefine.GetPersistentDecompressAssetsPath(moduleName)}{bundleConfigFileName}{HotUpdateDefine.BundleExtension}";
                //如果依旧不存在 说明当前模块的AB包有误
                if (!File.Exists(bundleConfigPath))
                {
                    return false;
                }
            }

            return true;
        }
        
        /// <summary>
        /// 加载AB包配置文件
        /// </summary>
        public void LoadAssetBundleConfig(string moduleName)
        {
            //1. 缓存中获取
            if (alreadyLoadedBundlesMap.ContainsKey(moduleName))
            {
                Debug.LogErrorFormat("该模块配置文件已经加载：{0}",moduleName);
                return;
            }
            
            if (!GetBundleConfigPath(moduleName))
            {
                Debug.LogErrorFormat("AB包配置找不到，请检查！{0}",moduleName);
                return;
            }

            try
            {
                //加载配置AB
                
                AssetBundle configAB = null;
                //如果该AssetBundle已经加密，则需要解密
                if (ABSettings.Ins.EncryptToggle.IsEncrypt)
                {
                    var bytes = AES.AESFileByteDecrypt(bundleConfigPath, ABSettings.Ins.EncryptToggle.EncryptKey);
                    configAB= AssetBundle.LoadFromMemory(bytes);
                }
                else
                {
                    configAB = AssetBundle.LoadFromFile(bundleConfigPath);
                }
                
                var configText = configAB.LoadAsset<TextAsset>(bundleConfigFileName).text;
                //Debug.LogFormat("success load ab config，{0}，{1}",moduleName,configText);
                var configData = JsonConvert.DeserializeObject<AssetBundleConfig>(configText);

                //把所有AB信息缓存管理
                foreach (var info in configData.BundleInfoList)
                {
                    if (!allBundleAssetMap.ContainsKey(info.Crc))
                    {
                        var item = new BundleItem();
                        item.Path = info.Path;
                        item.Crc = info.Crc;
                        item.ModuleName = moduleName;
                        item.AssetName = info.AssetName;
                        item.BundleDependence = info.BundleDependencies;
                        item.BundleName = info.BundleName;
                        allBundleAssetMap.Add(item.Crc, item);
                    }
                    else
                    {
                        Debug.LogErrorFormat("AB包重复缓存！{0}", info.BundleName);
                    }
                }

                //释放配置AB
                configAB.Unload(false);
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("加载配置AB包失败，{0}",e);
            }
            
        }

        /// <summary>
        /// 根据AB包名 查询该bundle中有哪些资源
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="result"></param>
        public void GetBundleItemByABName(string bundleName, List<BundleItem> result)
        {
            foreach (var item in allBundleAssetMap.Values)
            {
                if (item.BundleName.Equals(bundleName))
                {
                    result.Add(item);
                }
            }
        }

        /// <summary>
        /// 通过资源路径的CRC 加载资源对应的AB包
        /// </summary>
        /// <param name="crc"></param>
        /// <returns></returns>
        public BundleItem LoadAssetBundle(uint crc)
        {
            //1. 缓存中获取
            if (!allBundleAssetMap.TryGetValue(crc,out var bundleItem))
            {
                Debug.LogErrorFormat("当前资源，不存在AB包-{0}中，加载失败！crc:{1}",bundleConfigFileName,crc);
                return null;
            }
            //可以直接加载
            //2. 如果bundle为空，说明需要加载ab
            if (bundleItem.Bundle == null)
            {
                //加载AB
                bundleItem.Bundle = InternalLoadAssetBundle(bundleItem.BundleName,bundleItem.ModuleName);
                //加载AB的依赖AB
                foreach (var abName in bundleItem.BundleDependence)
                {
                    //重复加载检测
                    if (abName.Equals(bundleItem.BundleName))
                    {
                        continue;
                    }

                    InternalLoadAssetBundle(abName, bundleItem.ModuleName);
                }
                
            }

            return bundleItem;
        }

        /// <summary>
        /// 通过AB包名 加载AB包
        /// </summary>
        /// <param name="abName"></param>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        private AssetBundle InternalLoadAssetBundle(string abName,string moduleName)
        {
            //优先从已经加载的AB缓存池中获取
            if (!alreadyLoadedBundlesMap.TryGetValue(abName, out var bundleCache))
            {
                bundleCache = ClassPool.Get<AssetBundleCache>();
                //获取AB加载路径
                var abHotFilePath = $"{HotUpdateDefine.GetPersistHotAssetsPath(moduleName)}{abName}";
                //获取热更模块
                var module = AssetsPipeLine.Instance.GetHotAssetModule(moduleName);
                bool isHotUpdate = true;
                if (module == null || module.HotAssetCount <= 0)
                {
                    //模块不存在，或者热更资源数量为0，则通过文件判断 是否是热更资源
                    isHotUpdate = File.Exists(abHotFilePath);
                }
                //如果模块存在，且有热更资源
                else 
                {
                    //判断资源是否存在于模块中
                    isHotUpdate = module.ExistsHotAsset(abName);
                }

                string bundlePath = isHotUpdate ? abHotFilePath : $"{HotUpdateDefine.GetPersistentDecompressAssetsPath(moduleName)}{abName}";

                //解密AB包
                if (ABSettings.Ins.EncryptToggle.IsEncrypt)
                {
                    var bytes = AES.AESFileByteDecrypt(bundlePath, ABSettings.Ins.EncryptToggle.EncryptKey);
                    //todo 优化成 LoadFromFile
                    bundleCache.Bundle = AssetBundle.LoadFromMemory(bytes);
                }
                else
                {
                    bundleCache.Bundle = AssetBundle.LoadFromFile(bundlePath);
                }

                if (bundleCache.Bundle == null)
                {
                    Debug.LogErrorFormat("AB包加载失败，请检查！{0}",bundlePath);
                    return null;
                }

                
                alreadyLoadedBundlesMap.Add(abName,bundleCache);
            }

            //引用计数++
            bundleCache.ReferenceCount++;

            return bundleCache.Bundle;
        }

        
        /// <summary>
        /// 释放AB包，并释放AB包占用的内存资源
        /// </summary>
        /// <param name="bundleItem"></param>
        /// <param name="unload"></param>
        public void ReleaseAssets(BundleItem bundleItem,bool unload)
        {
            //AssetBundle 释放策略
            //1. 以 AssetBundle.Unload(false) 为主
            //2. 对于非对象资源，text，texture,audio ，资源加载完成，就可以直接Unload(false),释放AB镜像文件
            //3. 对于对象资源，gameobject，做引用计数管理，资源加载完成，Unload(false),释放AB镜像，gameObject 会存放到资源对象池
            
            //1. 以AssetBundle.Unload(true) 为主
            //2. 加载AB时，建立缓存，后续加载的所有资源对象 全部通过缓存AB 进行加载
            //3. 在跳转场景时，通过Unload(true) 彻底释放所有资源和内存占用
            
            if (bundleItem == null)
            {
                Debug.LogErrorFormat("assetBundle is null,release fail!");
                return;
            }

            bundleItem.AssetObject = null;

            //释放AB
            ReleaseAssetBundle(bundleItem.BundleName, unload);

            //卸载依赖AB
            if (bundleItem.BundleDependence != null && bundleItem.BundleDependence.Count > 0)
            {
                //根据引用计数释放AB
                foreach (var abName in bundleItem.BundleDependence)
                {
                    ReleaseAssetBundle(abName, unload);
                }
            }
        }

        /// <summary>
        /// 释放AB包
        /// </summary>
        /// <param name="bundleItem"></param>
        /// <param name="unload"></param>
        public void ReleaseAssetBundle(string abName,bool unload)
        {
            //如果AB包包名不为空
            if (string.IsNullOrEmpty(abName))
            {
                return;
            }
            if (!alreadyLoadedBundlesMap.TryGetValue(abName, out var abCache))
            {
                return;
            }
            if (abCache.Bundle == null)
            {
                return;
            }
            //引用计数--
            abCache.ReferenceCount--;
            //如果引用计数为0，直接释放
            if (abCache.ReferenceCount <= 0)
            {
                abCache.Bundle.Unload(unload);
                alreadyLoadedBundlesMap.Remove(abName);
                //回收对象池
                ClassPool.Put(abCache);
            }
            
            
        }
    }
}

