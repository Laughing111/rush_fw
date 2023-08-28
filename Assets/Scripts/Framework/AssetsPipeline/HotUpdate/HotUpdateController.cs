using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AssetBundleDataConfig;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>
    /// 正在等待下载的模块
    /// </summary>
    public class WaitDownloadModule
    {
        public string ModuleName;

        public Action<string> OnStartUpdate;

        public Action<string> OnFinishUpdate;
        
        public Action<string,float> OnUpdateProgress;

        public bool ValidVersion;
    }
    
    /// <summary>
    /// 热更控制器
    /// </summary>
    public class HotUpdateController:IHotAssetControl
    {

        /// <summary>
        /// 最大并发下载线程个数
        /// </summary>
        private int maxDownloadThreadCount;

        /// <summary>
        /// 所有热更资源模块
        /// </summary>
        private Dictionary<string, HotAssetModule> allAssetModulesMap = new Dictionary<string, HotAssetModule>();
        
        
        /// <summary>
        /// 正在热更下载资源的模块
        /// </summary>
        private Dictionary<string, HotAssetModule> downLoadingAssetModulesMap = new Dictionary<string, HotAssetModule>();
        
        
        /// <summary>
        /// 正在下载的热更模块列表
        /// </summary>
        private List<HotAssetModule> downloadingAssetMoudleList = new List<HotAssetModule>();
        
        /// <summary>
        /// 等待热更下载资源的模块队列
        /// </summary>
        private Queue<WaitDownloadModule> waitDownloadModulesQueue = new Queue<WaitDownloadModule>();

        private MonoBehaviour monoAgent;

        /// <summary>
        /// AB包加载完成
        /// </summary>
        public static Action<HotPatchInfo> OnDownloadBundleFinished;

        public HotUpdateController(MonoBehaviour agent)
        {
            monoAgent = agent;
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
            //如果是内嵌包 直接完成
            if (ABSettings.Ins.BundleHotType == BundleHotUpdateType.Builtin)
            {
                onUpdateFinish?.Invoke(moduleName);
                return;
            }

            //最大线程数
            maxDownloadThreadCount = ABSettings.Ins.BundleDownloadMaxThread;

            var module = GetOrCreateAssetModule(moduleName);
            //判断是否有闲置的线程
            if (downLoadingAssetModulesMap.Count < maxDownloadThreadCount)
            {
                if (!downLoadingAssetModulesMap.ContainsKey(moduleName))
                {
                    downLoadingAssetModulesMap.Add(moduleName,module);
                    downloadingAssetMoudleList.Add(module);
                }

                module.OnDownLoadAllAssetsFinish += HotModuleUpdateFinish;
                
                //启动下载
                module.StartHotUpdateAssets(() =>
                {
                    //开始热更
                    //负载均衡
                    MultiThreadBalancing();
                    onStartUpdate?.Invoke(moduleName);
                    
                },onUpdateFinish,validVersion);
            }
            else
            {
                //把热更模块添加到等待更新的队列
                //gc 优化
                waitDownloadModulesQueue.Enqueue(new WaitDownloadModule()
                {
                    ModuleName = moduleName,
                    OnStartUpdate = onStartUpdate,
                    OnFinishUpdate = onUpdateFinish,
                    ValidVersion = validVersion
                });
                waitUpdate?.Invoke(moduleName);
            }

        }

        /// <summary>
        /// 校验资源版本
        /// </summary>
        /// <param name="moduleName">热更模块名</param>
        /// <param name="validCallBack">校验回调，是否需要热更，热更资源的大小</param>
        public void ValidAssetsVersion(string moduleName, Action<bool, float> validCallBack)
        {
            var module = GetOrCreateAssetModule(moduleName);
            module.ValidAssetsVersion(validCallBack);
        }

        
        /// <summary>
        /// 获取热更资源模块
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public HotAssetModule GetHotAssetModule(string moduleName)
        {
            if (allAssetModulesMap.TryGetValue(moduleName, out var module))
            {
                return module;
            }

            return null;
        }

        private List<HotAssetModule> tempUpdateList = new List<HotAssetModule>();
        /// <summary>
        /// 主线程更新
        /// </summary>
        public void OnMainThreadUpdate()
        {
            tempUpdateList.Clear();
            tempUpdateList.AddRange(downloadingAssetMoudleList);
            for (int i = 0; i < tempUpdateList.Count; i++)
            {
                tempUpdateList[i].OnMainThreadUpdate();
            }

        }

        /// <summary>
        /// 多线程负载均衡
        /// </summary>
        private void MultiThreadBalancing()
        {
            //正在下载的热更模块
            int count = downLoadingAssetModulesMap.Count;
            //计算多线程均衡后的分配个数
            //如果最大：3 
            //1. 正在下载：1 ，则并发为 3/1 = 3 (偶数)
            //2. 正在下载：2 ，则并发：3/2 = 1.5 向上取整 2,1 （奇数）
            //3. 正在下载：3 ，并发 3/3 = 1 (偶数）
            var threadCount = maxDownloadThreadCount * 1.0f / count;
            //主下载线程数
            int mainThreadCount = 0;
            //int 向下强转
            int threadBalancingCount = (int)threadCount;

            if (threadBalancingCount < threadCount)
            {
                //向上取整 分配给主模块
                mainThreadCount = Mathf.CeilToInt(threadCount);
                //向下取整 分配给其他模块
                threadBalancingCount = Mathf.FloorToInt(threadCount);
            }
            
            //多线程均衡
            int i = 0;
            foreach (var item in downLoadingAssetModulesMap.Values)
            {
                if (mainThreadCount > 0 && i == 0)
                {
                    //设置主下载线程数
                    item.SetDownloadThreadCount(mainThreadCount);
                }
                else
                {
                    item.SetDownloadThreadCount(threadBalancingCount);
                }

                i++;
            }

        }

        /// <summary>
        /// 热更完成
        /// </summary>
        private void HotModuleUpdateFinish(string moduleName)
        {
            //移除下载完成的模块
            if (downLoadingAssetModulesMap.TryGetValue(moduleName,out var module))
            {
                downloadingAssetMoudleList.Remove(module);
                downLoadingAssetModulesMap.Remove(moduleName);
            }
            //线程空闲下来了
            //等待下载的队列中 是否有等待热更的模块
            if (waitDownloadModulesQueue.Count > 0)
            {
                var waitModule = waitDownloadModulesQueue.Dequeue();
                HotUpdateAsset(waitModule.ModuleName,waitModule.OnStartUpdate,waitModule.OnFinishUpdate,null,waitModule.ValidVersion);
            }
            else
            {
                //如果没有等待的模块
                //处理负载均衡，分配线程给其他正在热更的模块，加快更新
                MultiThreadBalancing();
            }
        }

        private HotAssetModule GetOrCreateAssetModule(string moduleName)
        {
            if (allAssetModulesMap.TryGetValue(moduleName,out var module))
            {
                return module;
            }

            module = new HotAssetModule(moduleName,monoAgent);
            allAssetModulesMap.Add(moduleName,module);
            return module;
        }


    } 
}

