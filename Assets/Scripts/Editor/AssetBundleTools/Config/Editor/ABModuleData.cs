using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AssetBundleTools
{
    [Serializable]
    public class ABModuleData
    {
        /// <summary>
        /// Ab包的模块Id
        /// </summary>
        public long BundleId;

        /// <summary>
        /// 模块名称
        /// </summary>
        public string ModuleName;

        /// <summary>
        /// 是否打包
        /// </summary>
        public bool IsBuild;

        /// <summary>
        /// 上一次点击的时间
        /// </summary>
        public float LastClickTime;


        public string[] PrefabPath;


        public string[] RootFolderPath;


        public BundleFileInfo[] SingleBundlePath;

    }

    [Serializable]
    public class BundleFileInfo
    {
        [HideLabel] 
        public string ABName = "AssetBundle Name";

        
        [FolderPath]
        [HideLabel]  
        public string BundlePath = "Path...";
    }
}

