using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AssetBundleDataConfig;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Runtime
{
    /// <summary>
    /// 热更资源模块
    /// </summary>
    public class HotAssetModule
    {
        public string ModuleName { get; private set; }

        /// <summary>
        /// 热更资源数量
        /// </summary>
        public int HotAssetCount
        {
            get
            {
                return allHotAssetList.Count;
            }
        }
        /// <summary>
        /// 下载所有资源完成
        /// </summary>
        public Action<string> OnDownLoadAllAssetsFinish;
        

        /// <summary>
        /// AB包的配置文件下载完成
        /// </summary>
        public Action<string> OnDownLoadABConfig;
        
        /// <summary>
        /// AB包资源下载完成
        /// </summary>
        public Action<string> OnDownLoadAB;

        /// <summary>
        /// 服务器清单
        /// </summary>
        private HotPatchManifest serverHotPathManifest;
        
        /// <summary>
        /// 本地清单
        /// </summary>
        private HotPatchManifest localHotPathManifest;

        private MonoBehaviour agent;

        /// <summary>
        /// 所有热更的资源列表
        /// </summary>
        private List<HotPatchInfo> allHotAssetList = new List<HotPatchInfo>();
        
        /// <summary>
        /// 需要下载的热更资源列表
        /// </summary>
        private List<HotPatchInfo> needDownLoadAssetList = new List<HotPatchInfo>();

        /// <summary>
        /// 需要热更文件的大小
        /// </summary>
        public float HotAssetSizeM
        {
            get;
            set;
        }

        /// <summary>
        /// 资源已经下载的大小Mb
        /// </summary>
        public float AssetDownloadSizeM
        {
            get;
            set;
        }

        /// <summary>
        /// 资源下载器
        /// </summary>
        private HotAssetDownLoader downLoader;

        /// <summary>
        /// 本地AB资源的存储目录
        /// </summary>
        private string localHotAssetSavePath;

        public HotAssetModule(string moduleName,MonoBehaviour mono)
        {
            ModuleName = moduleName;
            this.agent = mono;

            localHotAssetSavePath = HotUpdateDefine.GetPersistHotAssetsPath(moduleName);
        }

        /// <summary>
        /// 开始热更资源
        /// </summary>
        /// <param name="startDownLoadCallBack">开始下载的回调</param>
        /// <param name="onFinish">下载完成的回调</param>
        /// <param name="validVersion">是否校验版本</param>
        public void StartHotUpdateAssets(Action startDownLoadCallBack,Action<string> onFinish = null,bool validVersion = true)
        {
            OnDownLoadAllAssetsFinish += onFinish;
            if (validVersion)
            {
                //检测资源是否需要热更
                ValidAssetsVersion((needDownload, size) =>
                {
                    if (needDownload)
                    {
                        //需要热更 则开始下载
                        StartDownLoadPatchAssets(startDownLoadCallBack);
                    }
                    else
                    {
                        onFinish?.Invoke(ModuleName);
                    }
                });
            }
            else
            {
                //需要热更 则开始下载
                StartDownLoadPatchAssets(startDownLoadCallBack);
            }
        }

        /// <summary>
        /// 开始下载热更资源
        /// </summary>
        /// <param name="startDownLoadCallBack"></param>
        private void StartDownLoadPatchAssets(Action startDownLoadCallBack)
        {
            //优先下载AB配置文件，下载完成后，执行回调，让业务侧及时加载配置文件
            //热更下载完整，同样执行回调，供业务层加载刚下载完的资源
            List<HotPatchInfo> infoList = new List<HotPatchInfo>();
            for (int i = 0; i < needDownLoadAssetList.Count; i++)
            {
                var info = needDownLoadAssetList[i];
                //检测到配置文件，需要优先下载
                if (info.ABName.Contains(HotUpdateDefine.ABConfigTag))
                {
                    infoList.Insert(0,info);
                    continue;
                }
                
                infoList.Add(info);
            }

            //构建下载队列
            Queue<HotPatchInfo> downLoadQueue = new Queue<HotPatchInfo>();
            foreach (var info in infoList)
            {
                downLoadQueue.Enqueue(info);
            }
            
            downLoader = new HotAssetDownLoader(this, downLoadQueue, serverHotPathManifest.DownLoadUrl, localHotAssetSavePath,
                DownloadBundleSuccess, DownloadBundleFail, DownloadBundleFinish);
            
            //通过资源下载器 下载资源
            startDownLoadCallBack?.Invoke();
            
            //开始下载队列中的资源
            downLoader.StartThreadDownloadQueue();
            
        }

        /// <summary>
        /// 校验资源版本
        /// </summary>
        /// <param name="validCallBack"></param>
        public void ValidAssetsVersion(Action<bool,float> validCallBack)
        {
            needDownLoadAssetList.Clear();
            //1.1 启动下载
            agent.StartCoroutine(DownLoadHotAssetsManifest(() =>
            {
                // 服务器的当前资源清单 下载完成
                //1. 当前版本是否需要热更
                if (CheckModuleAssetLatest())
                {
                    var severFinalPatchAsset = serverHotPathManifest.PatchAssetList[^1];
                    var needDownload = CalcHotAssetList(localHotAssetSavePath,severFinalPatchAsset);
                    //执行回调 是否需要热更 及 热更体积
                    validCallBack?.Invoke(needDownload,HotAssetSizeM);
                }
                else
                {
                    validCallBack?.Invoke(false,0);
                }
                //2. 如果需要热更，开始计算需要下载的文件体积 开始下载
                //3. 如果不需要热更，说明文件最新，直接热更完成

            }));

        }

        /// <summary>
        /// 计算需要下载的文件列表
        /// </summary>
        /// <param name="abLocalSavePath">本地保存AB资源的目录</param>
        /// <param name="serverPatchAsset"></param>
        /// <returns></returns>
        private bool CalcHotAssetList(string abLocalSavePath,HotPatchAsset serverPatchAsset)
        {
            if (!Directory.Exists(abLocalSavePath))
            {
                Directory.CreateDirectory(abLocalSavePath);
            }
            foreach (var info in serverPatchAsset.PatchInfoList)
            {
                //获取本地AB包文件路径
                var filePath = abLocalSavePath + info.ABName;
                allHotAssetList.Add(info);
                //如果本地文件不存在 或者 md5 与服务器不一致
                if (!File.Exists(filePath) || info.MD5 != MD5.GetMd5FromFile(filePath))
                {
                    needDownLoadAssetList.Add(info);
                    //mb
                    HotAssetSizeM += info.SizeK;
                }
            }

            HotAssetSizeM /= 1024;

            //文件数量有效 才需要下载
            return needDownLoadAssetList.Count > 0;
        }

        /// <summary>
        /// 检测模块资源是否需要热更
        /// </summary>
        /// <returns></returns>
        private bool CheckModuleAssetLatest()
        {
            //如果服务器清单不存在，不需要热更
            if (serverHotPathManifest == null)
            {
                return false;
            }
            
            //如果本地清单不存在 说明第一次启动，需要热更
            var localManifestPath = HotUpdateDefine.GetPersistLocalHotPatchManifestPath(ModuleName);
            if(!File.Exists(localManifestPath))
            {
                return true;
            }
            
            //判断本地资源清单 版本号是否与服务器一致 不一致需要热更
            var manifestContent = File.ReadAllText(localManifestPath);
            var currentLocalManifest = JsonConvert.DeserializeObject<HotPatchManifest>(manifestContent);
            if (currentLocalManifest == null 
                || (currentLocalManifest.PatchAssetList.Count <= 0 && serverHotPathManifest.PatchAssetList.Count > 0))
            {
                return true;
            }
            
            //获取本地热更补丁的最后一个补丁
            var localFinalPatchAsset = currentLocalManifest.PatchAssetList[^1];
            //获取服务端热更补丁的最后一个补丁
            var severFinalPatchAsset = serverHotPathManifest.PatchAssetList[^1];

            if (localFinalPatchAsset != null && severFinalPatchAsset != null)
            {
                //不一致 需要热更
                return localFinalPatchAsset.PatchVersion != severFinalPatchAsset.PatchVersion;
            }

            //服务器最后一个补丁不为空 需要热更
            return severFinalPatchAsset != null;
        }

        /// <summary>
        /// 下载资源热更清单
        /// </summary>
        /// <returns></returns>
        public IEnumerator DownLoadHotAssetsManifest(Action onFinish)
        {
            //获取下载地址
            string url = ABSettings.Ins.AssetBundleDownloadUrl + $"/{HotUpdateDefine.GetHotPatchManifestPath(ModuleName)}";
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            //超时30s
            webRequest.timeout = 30;
            Debug.LogFormat("*** Request Get HotAssetsManifest Url {0}",url);
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError($"*** Request Get HotAssetsManifest Url Error :{webRequest.error}");
            }
            else
            {
                try
                {
                    Debug.LogFormat("*** Request Get HotAssetsManifest Success! module:{0}",ModuleName);
                    //把清单写到本地
                    var path = HotUpdateDefine.GetPersistServerHotPatchManifestPath(ModuleName);
                    FileUtility.WriteFile(path,webRequest.downloadHandler.data);
                    serverHotPathManifest = JsonConvert.DeserializeObject<HotPatchManifest>(webRequest.downloadHandler.text);

                }
                catch (Exception e)
                {
                    Debug.LogError($"服务端热更资源清单下载异常！文件不存在或配置出错，请检查！{e.ToString()}");
                }
            }
            onFinish?.Invoke();
        }

        #region 资源下载回调

        /// <summary>
        /// 资源下载成功
        /// </summary>
        /// <param name="info"></param>
        private void DownloadBundleSuccess(HotPatchInfo info)
        {
            var abNameWithoutExtension = info.ABName.Replace(HotUpdateDefine.BundleExtension, string.Empty);
            if (info.ABName.Contains(HotUpdateDefine.ABConfigTag))
            {
                OnDownLoadABConfig?.Invoke(abNameWithoutExtension);
                //如果下载成功 需要及时加载配置文件
                //todo 
            }
            else
            {
                OnDownLoadAB?.Invoke(abNameWithoutExtension);
            }

            HotUpdateController.OnDownloadBundleFinished?.Invoke(info);
        }
        
        /// <summary>
        /// 资源下载失败
        /// </summary>
        /// <param name="info"></param>
        private void DownloadBundleFail(HotPatchInfo info)
        {
            
        }
        
        /// <summary>
        /// 所有资源下载完毕
        /// </summary>
        /// <param name="info"></param>
        private void DownloadBundleFinish(HotPatchInfo info)
        {
            //本地清单是否存在
            var localManifestPath = HotUpdateDefine.GetPersistLocalHotPatchManifestPath(ModuleName);
            if (File.Exists(localManifestPath))
            {
                File.Delete(localManifestPath);
            }
            var serverManifestPath = HotUpdateDefine.GetPersistServerHotPatchManifestPath(ModuleName);
            //服务端清单文件 拷贝成本地
            File.Copy(serverManifestPath,localManifestPath);
            
            OnDownLoadAllAssetsFinish?.Invoke(ModuleName);
        }
        

        #endregion

        public void OnMainThreadUpdate()
        {
            downLoader?.OnMainThreadUpdate();
        }
        
        /// <summary>
        /// 设置下载线程个数
        /// </summary>
        /// <param name="count"></param>
        public void SetDownloadThreadCount(int count)
        {
            Debug.LogFormat("{0}-多线程负载均衡，分配的线程数-{1}",ModuleName,count.ToString());
            if (downLoader != null)
            {
                downLoader.maxThreadCount = count;
            }
        }

        /// <summary>
        /// 热更资源是否存在
        /// </summary>
        /// <param name="abName"></param>
        public bool ExistsHotAsset(string abName)
        {
            foreach (var info in allHotAssetList)
            {
                if (abName.Equals(info.ABName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

