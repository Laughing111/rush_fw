using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using AssetBundleDataConfig;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Runtime
{
    /// <summary>
    /// 资源下载线程
    /// </summary>
    public class DownLoadThread
    {
        private HotAssetModule curModule;
        
        private HotPatchInfo curInfo;

        private string downLoadUrl;

        private string savePath;

        /// <summary>
        /// 下载文件的大小
        /// </summary>
        private float downLoadByte;

        private Action<DownLoadThread, HotPatchInfo> onDownloadSuccess;
        private Action<DownLoadThread, HotPatchInfo> onDownloadFailed;

        /// <summary>
        /// 当前下载的次数
        /// </summary>
        private int curDownloadCount;

        /// <summary>
        /// 最大下载次数
        /// </summary>
        private const int maxDownLoadTimes = 3;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="module">资源模块</param>
        /// <param name="info">资源信息</param>
        /// <param name="url">下载地址</param>
        /// <param name="saveFilePath">资源存储路径</param>
        public DownLoadThread(HotAssetModule module, HotPatchInfo info, string url,string saveFilePath)
        {
            curModule = module;
            curInfo = info;
            downLoadUrl = $"{url}/{info.ABName}";
            savePath = $"{saveFilePath}/{info.ABName}";
        }

        /// <summary>
        /// 通过子线程开始下载资源
        /// </summary>
        /// <param name="onSuccess">下载成功</param>
        /// <param name="onFailed">下载失败</param>
        public void StartDownLoad(Action<DownLoadThread,HotPatchInfo> onSuccess,Action<DownLoadThread,HotPatchInfo> onFailed)
        {
            curDownloadCount++;
            this.onDownloadSuccess = onSuccess;
            this.onDownloadFailed = onFailed;

            //开启子线程 执行下载
            Task.Run(() =>
            {
                try
                {
                    Debug.LogFormat("Start Download {0}:{1}",curModule.ModuleName,downLoadUrl);
                    
                    HttpWebRequest request = WebRequest.Create(downLoadUrl) as HttpWebRequest;
                    request.Method = "GET";
                    //发起请求
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    
                    //创建本地文件流
                    var stream = File.Create(savePath);
                    using (var responseStream = response.GetResponseStream())
                    {
                        var buffer = new byte[512];
                        //从字节流中读取字节，读取到buffer
                        int size = responseStream.Read(buffer, 0, buffer.Length);

                        while (size > 0)
                        {
                            stream.Write(buffer,0,buffer.Length);
                            size = responseStream.Read(buffer, 0, buffer.Length);
                            downLoadByte += size;
                            curModule.AssetDownloadSizeM += size * 1.0f / 1024 / 1024;
                        }
                        
                        stream.Dispose();
                        stream.Close();
                        Debug.LogFormat("Download success,Module {0}:{1},save path {2}",curModule.ModuleName,downLoadUrl,savePath);
                        onDownloadSuccess?.Invoke(this,curInfo);   
                    }

                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("download assetbundle error!,url:{0},error:{1}",downLoadUrl,e);
                    if (curDownloadCount > maxDownLoadTimes)
                    {
                        onDownloadFailed?.Invoke(this,curInfo);
                    }
                    else
                    {
                        StartDownLoad(onDownloadSuccess,onDownloadFailed);
                        Debug.LogErrorFormat("download assetbundle error!now to retry - {2},url:{0},error:{1}",downLoadUrl,e,curDownloadCount);
                    }

                    throw;
                }
            });
        }
    }

}
