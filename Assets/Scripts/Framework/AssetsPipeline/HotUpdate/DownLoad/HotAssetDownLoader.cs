using System.Collections;
using System.Collections.Generic;
using AssetBundleDataConfig;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>
    /// 下载委托
    /// </summary>
    public delegate void DownLoadEvent(HotPatchInfo downLoadInfo);

    public class DownloadEventHandle
    {
        public DownLoadEvent onEvent;
        public HotPatchInfo info;
    }
    
    /// <summary>
    /// 多线程资源下载器
    /// </summary>
    public class HotAssetDownLoader
    {

        /// <summary>
        /// 最大下载线程个数
        /// </summary>
        public int maxThreadCount;
        /// <summary>
        /// 下载地址
        /// </summary>
        private string assetDownloadUrl;

        /// <summary>
        /// 资源存储路径
        /// </summary>
        private string hotAssetSavePath;

        /// <summary>
        /// 当前模块
        /// </summary>
        private HotAssetModule curHotPatchModule;

        /// <summary>
        /// 当前下载列表
        /// </summary>
        private Queue<HotPatchInfo> curDownLoadQueue;

        private DownLoadEvent onDownLoadSuccess;
        private DownLoadEvent onDownLoadFailed;
        private DownLoadEvent onDownLoadFinish;

        /// <summary>
        /// 当前所有正在下载的线程列表
        /// </summary>
        private List<DownLoadThread> allDownloadThreadList = new List<DownLoadThread>();

        /// <summary>
        /// 所有子线程中执行的回调
        /// </summary>
        private Queue<DownloadEventHandle> downLoadEventQueue = new Queue<DownloadEventHandle>();
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="module">资源模块</param>
        /// <param name="downloadLst">下载列表</param>
        /// <param name="url">下载路径</param>
        /// <param name="hotAssetSavePath">资源存储路径</param>
        /// <param name="onDownLoadSuccess">下载成功的回调</param>
        /// <param name="onDownLoadFailed">下载失败的回调</param>
        /// <param name="onDownLoadFinish">所有资源下载结束的回调</param>
        public HotAssetDownLoader(HotAssetModule module,Queue<HotPatchInfo> downloadQueue,string url,string hotAssetSavePath,
            DownLoadEvent onDownLoadSuccess,DownLoadEvent onDownLoadFailed,DownLoadEvent onDownLoadFinish)
        {
            curHotPatchModule = module;
            curDownLoadQueue = downloadQueue;
            assetDownloadUrl = url;
            this.hotAssetSavePath = hotAssetSavePath;
            this.onDownLoadSuccess = onDownLoadSuccess;
            this.onDownLoadFailed = onDownLoadFailed;
            this.onDownLoadFinish = onDownLoadFinish;
        }

        public void StartThreadDownloadQueue()
        {
            Debug.LogFormat("Start DownLoad AssetBundle,max thread count-{0}",maxThreadCount);
            //根据最大的线程下载个数 开始下载通道
            for (int i =0;i<maxThreadCount;i++)
            {
                if (curDownLoadQueue.Count > 0)
                {
                    StartDownloadNextBundle();
                }
            }
        }

        /// <summary>
        /// 开始下载下一个AB
        /// </summary>
        private void StartDownloadNextBundle()
        {
            var info = curDownLoadQueue.Dequeue();
            DownLoadThread downLoadthread = new DownLoadThread(curHotPatchModule, info,assetDownloadUrl,hotAssetSavePath);
            downLoadthread.StartDownLoad(DownloadSuccess,DownloadFail);

            lock (allDownloadThreadList)
            {
                allDownloadThreadList.Add(downLoadthread);
            }
        }

        
        private void DownloadNextBundle()
        {
            //如果当前下载的线程 大于最大限制个数，就关闭当前通道
            if (allDownloadThreadList.Count > maxThreadCount)
            {
                Debug.LogFormat("download next bundle expand max thread count,close this channel.");
                return;
            }

            if (curDownLoadQueue.Count>0)
            {
                StartDownloadNextBundle();
                if (allDownloadThreadList.Count < maxThreadCount)
                {
                    //计算出正在待机的线程下载通道 全部打开
                    int idleThreadCount = maxThreadCount - allDownloadThreadList.Count;
                    for (int i = 0; i < idleThreadCount; i++)
                    {
                        if (curDownLoadQueue.Count > 0)
                        {
                            StartDownloadNextBundle();
                        }
                    }
                }
            }
            else
            {
                //如果没有要下载的文件 也没有下载中的线程
                //说明所有文件都下载成功了
                if (allDownloadThreadList.Count <= 0)
                {
                    TriggerCallBackInMainThread(new DownloadEventHandle()
                    {
                        onEvent = onDownLoadFinish
                    });
                }
            }
        }

        /// <summary>
        /// 子线程调用 下载成功
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="info"></param>
        private void DownloadSuccess(DownLoadThread thread,HotPatchInfo info)
        {
            RemoveDownloadThread(thread);
            //子线程下载，回调也是在子线程执行
            var handle = new DownloadEventHandle()
            {
                onEvent = onDownLoadSuccess,
                info = info
            };
            TriggerCallBackInMainThread(handle);
            DownloadNextBundle();
        }
        
        /// <summary>
        /// 子线程调用 下载失败
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="info"></param>
        private void DownloadFail(DownLoadThread thread,HotPatchInfo info)
        {
            RemoveDownloadThread(thread);
            //子线程下载，回调也是在子线程执行
            var handle = new DownloadEventHandle()
            {
                onEvent = onDownLoadFailed,
                info = info
            };
            TriggerCallBackInMainThread(handle);
            DownloadNextBundle();
        }

        private void RemoveDownloadThread(DownLoadThread thread)
        {
            lock (allDownloadThreadList)
            {
                if (allDownloadThreadList.Contains(thread))
                {
                    allDownloadThreadList.Remove(thread);
                }
            }
            
        }

        private void TriggerCallBackInMainThread(DownloadEventHandle handle)
        {
            lock (downLoadEventQueue)
            {
                //gc 优化
                downLoadEventQueue.Enqueue(handle);
            }
        }

        /// <summary>
        /// 主线程执行回调
        /// </summary>
        public void OnMainThreadUpdate()
        {
            lock (downLoadEventQueue)
            {
                while (downLoadEventQueue.Count>0)  //if (downLoadEventQueue.Count>0)
                {
                   var eventItem = downLoadEventQueue.Dequeue();
                   eventItem.onEvent?.Invoke(eventItem.info);
                }
            }
        }
    } 
}

