using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleDataConfig
{
    /// <summary>
    /// 热更资源清单
    /// </summary>
    [Serializable]
    public class HotPatchManifest
    {
        /// <summary>
        /// 下载地址
        /// </summary>
        public string DownLoadUrl;

        public List<HotPatchAsset> PatchAssetList;

    }

    /// <summary>
    /// 热更补丁资源类 用于多个补丁管理（回退补丁等）
    /// </summary>
    [Serializable]
    public class HotPatchAsset
    {
        /// <summary>
        /// 补丁版本
        /// </summary>
        public int PatchVersion;

        public List<HotPatchInfo> PatchInfoList;
    }
    
    [Serializable]
    public class HotPatchInfo
    {
        /// <summary>
        /// AB包名
        /// </summary>
        public string ABName;

        /// <summary>
        /// MD5码
        /// </summary>
        public string MD5;

        /// <summary>
        /// 文件体积
        /// </summary>
        public float SizeK;
    }
}

