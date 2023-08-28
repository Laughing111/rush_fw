using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    public interface IHotAssetControl
    {
        /// <summary>
        /// 执行热更
        /// </summary>
        /// <param name="moduleName">热更模块名</param>
        /// <param name="onStartUpdate">开始热更</param>
        /// <param name="onUpdateFinish">热更结束</param>
        /// <param name="waitUpdate">等待热更</param>
        /// <param name="validVersion">是否校验资源版本</param>
        void HotUpdateAsset(string moduleName,Action<string> onStartUpdate,Action<string> onUpdateFinish,Action<string> waitUpdate,bool validVersion = true);

        /// <summary>
        /// 校验资源版本
        /// </summary>
        /// <param name="moduleName">热更模块名</param>
        /// <param name="validCallBack">校验回调，是否需要热更，热更资源的大小</param>
        void ValidAssetsVersion(string moduleName, Action<bool, float> validCallBack);

        /// <summary>
        /// 获取热更资源模块
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        HotAssetModule GetHotAssetModule(string moduleName);

        /// <summary>
        /// 主线程更新
        /// </summary>
        void OnMainThreadUpdate();
    }
}

