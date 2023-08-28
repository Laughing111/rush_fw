using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>
    /// 热更管理类
    /// </summary>
    public class HotUpdateManager:Singleton<HotUpdateManager>
    {
        /// <summary>
        /// 热更并解压Bundle文件
        /// </summary>
        public void HotUpdateAndUnpackBundle(string moduleName)
        {
            //开始解压游戏内嵌资源
            AssetsPipeLine.Instance.StartDecompressBuiltinPatch(moduleName, () =>
            {
                //说明资源开始解压
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    InstantiateResObj<TestDownload>("Canvas").InitView("当前无网络，请检查网络后重试！",null,null);
                }
                else
                {
                    ValidAssets(moduleName);
                }
            });
            
        }
        
        public void ValidAssets(string moduleName)
        {
            AssetsPipeLine.Instance.ValidAssetsVersion(moduleName, (needUpdate, sizeM) =>
            {
                if (needUpdate)
                {
                    // //当用户使用的是流量，询问用户，是否需要更新资源
                    // if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork
                    //     || Application.platform == RuntimePlatform.WindowsEditor 
                    //     || Application.platform == RuntimePlatform.WindowsPlayer
                    //     || Application.platform == RuntimePlatform.OSXEditor
                    //     || Application.platform == RuntimePlatform.OSXPlayer)
                    // {
                        //弹出选择弹窗，让用户决定是否更新
                        InstantiateResObj<TestDownload>("Canvas").InitView($"检测到{sizeM:F2}M 资源需要更新，是否需要更新？",
                            () =>
                            {
                                //确认更新回调
                                StartHotAssets(moduleName);
                            },
                            () =>
                            {
                                //退出游戏回调
                            });
                    // }
                    // else
                    // {
                    //     StartHotAssets(moduleName);
                    // }
                }
                else
                {
                    //如果不需要热更 直接进入游戏
                    OnHotFinishCallBack(moduleName);
                }
            });
        }

        /// <summary>
        /// 开始热更资源
        /// </summary>
        /// <param name="abName"></param>
        public void StartHotAssets(string moduleName)
        {
            AssetsPipeLine.Instance.HotUpdateAsset(moduleName,OnStartUpdatePatch,OnHotFinishCallBack,null,false);
        }

        /// <summary>
        /// 热更完成的回调
        /// </summary>
        public void OnHotFinishCallBack(string moduleName)
        {
            AssetBundleManager.Instance.LoadAssetBundleConfig(moduleName);
            Debug.Log("热更成功！进入游戏！");
            AssetsPipeLine.Instance.InstantiateAsync("Assets/Res/Prefabs/Login.prefab",OnCompleted);
        }

        private void OnCompleted(GameObject arg1, Object arg2, Object arg3)
        {
            Debug.LogError("加载成功！");
        }

        /// <summary>
        /// 开始热更
        /// </summary>
        public void OnStartUpdatePatch(string moduleName)
        {
            
        }


        public T InstantiateResObj<T>(string prefabName) where T: MonoBehaviour
        {
            var asset = Resources.Load<GameObject>(prefabName);
            var objIns = Object.Instantiate(asset);
            return objIns.GetComponent<T>();
        }
    }
}

