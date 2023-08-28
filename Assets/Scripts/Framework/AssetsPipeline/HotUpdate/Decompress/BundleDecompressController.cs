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
    public class BundleDecompressController : BaseDecompress
    {
        /// <summary>
        /// 资源内嵌路径
        /// </summary>
        private string streamingAssetsBundlePath;

        /// <summary>
        /// 资源解压路径
        /// </summary>
        private string decompressAssetsPath;

        /// <summary>
        /// 需要解压的资源文件列表
        /// </summary>
        private List<string> needDecompressAssetsList = new List<string>();

        /// <summary>
        /// 开始解压内嵌文件
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="onStart"></param>
        /// <returns></returns>
        public override BaseDecompress StartDecompressBuiltinPatch(string moduleName, Action onCompleted)
        {
            if (CalcDecompressFile(moduleName))
            {
                IsStartDecompress = true;
                AssetsPipeLine.Instance.StartCoroutine(UnPackToPersistentDataPath(onCompleted));

            }
            else
            {
                Debug.LogFormat("不需要解压文件.");
                onCompleted?.Invoke();
            }
            return this;
        }

        /// <summary>
        /// 计算需要解压的文件
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        private bool CalcDecompressFile(string moduleName)
        {
            streamingAssetsBundlePath = HotUpdateDefine.GetStreamingAssetsBuiltinBundlePath(moduleName);
            decompressAssetsPath = HotUpdateDefine.GetPersistentDecompressAssetsPath(moduleName);

            if (!Directory.Exists(decompressAssetsPath))
            {
                Directory.CreateDirectory(decompressAssetsPath);
            }
            
            //计算需要解压的文件大小
            var infoPath = HotUpdateDefine.GetBuiltinBundleInfoPath(moduleName);
            TextAsset assetContent = Resources.Load<TextAsset>(infoPath);
#if UNITY_ANDROID || UNITY_IOS
            if (assetContent == null)
            {
                Debug.LogErrorFormat("内嵌资源配置文件不存在，请检查！Resources/{0}",infoPath);
                return false;
            }

            needDecompressAssetsList.Clear();
            TotalSizeM = 0f;
            var builtinBundleInfoList = JsonConvert.DeserializeObject<List<BuiltinBundleInfo>>(assetContent.text);
            foreach (var info in builtinBundleInfoList)
            {
                //解压后的存储路径
                string localAssetFilePath = decompressAssetsPath + info.FileName;
                if (!File.Exists(localAssetFilePath) || MD5.GetMd5FromFile(localAssetFilePath) != info.MD5)
                {
                    //文件不存在 或者 MD5不一致
                    needDecompressAssetsList.Add(info.FileName);
                    TotalSizeM += info.SizeKB / 1024;
                }
            }

            return needDecompressAssetsList.Count > 0;
#else
            return false;
#endif
        }
        
        public override float GetDecompressProgress()
        {
            return AlreadyDecompressSizeM / TotalSizeM;
        }

        private IEnumerator UnPackToPersistentDataPath(Action onUnPack)
        {
            foreach (var fileName in needDecompressAssetsList)
            {

                string filePath;

#if UNITY_EDITOR_OSX || UNITY_IOS
                filePath = "file://" + streamingAssetsBundlePath + fileName;
#else
                filePath = streamingAssetsBundlePath + fileName;
#endif
                Debug.LogFormat("Start UnPack AssetBundle,filePath:{0}\r\n UnpackPath:{1}",filePath,decompressAssetsPath);
                
                //通过 UnityWebRequest 访问本地文件
                UnityWebRequest webRequest = UnityWebRequest.Get(filePath);
                //超时30s
                webRequest.timeout = 30;
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.LogErrorFormat("UnPack Error,{0}", webRequest.error);
                }
                else
                {
                    byte[] bytes = webRequest.downloadHandler.data;
                    var savePath = $"{decompressAssetsPath}{fileName}";
                    if (File.Exists(savePath))
                    {
                        File.Delete(savePath);
                    }
                    FileUtility.WriteFile(savePath,bytes);
                    AlreadyDecompressSizeM += bytes.Length / 1024f / 1024f;
                    Debug.LogFormat("Already Decompress Size M : {0}, Total Size M : {1}",AlreadyDecompressSizeM,TotalSizeM);
                    Debug.LogFormat("UnPack Finish! {0}",savePath);
                }
                
                webRequest.Dispose();
            }
            
            onUnPack?.Invoke();
            IsStartDecompress = false;
        }
    }
}

