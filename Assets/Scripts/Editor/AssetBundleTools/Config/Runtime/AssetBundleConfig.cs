using System;
using System.Collections.Generic;

namespace AssetBundleDataConfig
{
    [Serializable]
    public class AssetBundleConfig
    {
        /// <summary>
        /// 所有AB的信息
        /// </summary>
        public List<AssetBundleInfo> BundleInfoList;
    }

    /// <summary>
    /// AB包具体信息
    /// </summary>
    [Serializable]
    public class AssetBundleInfo
    {
        /// <summary>
        /// 路径
        /// </summary>
        public string Path;

        /// <summary>
        /// 路径的Crc码
        /// </summary>
        public uint Crc;

        /// <summary>
        /// AB包名
        /// </summary>
        public string BundleName;

        /// <summary>
        /// 资源名
        /// </summary>
        public string AssetName;

        /// <summary>
        /// 依赖项
        /// </summary>
        public List<string> BundleDependencies;
    }


    /// <summary>
    /// 内嵌的AB包信息
    /// </summary>
    [Serializable]
    public class BuiltinBundleInfo
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName;
        
        /// <summary>
        /// 校验本地已解压文件 是否与包内文件一致 如果不一致，说明本地文件被篡改，需要重新解压
        /// （需要进行校验的前提是当前模块没有开启热更）
        /// </summary>
        public string MD5;

        /// <summary>
        /// 文件体积
        /// </summary>
        public float SizeKB;
    }
}

