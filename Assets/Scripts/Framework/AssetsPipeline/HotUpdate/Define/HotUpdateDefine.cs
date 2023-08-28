using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>
    /// AB热更模式
    /// </summary>
    public enum BundleHotUpdateType
    {
        //内嵌 
        Builtin,
        //需要热更
        Hot,
    }

    /// <summary>
    /// 加载资源的方式
    /// </summary>
    public enum LoadAssetMode
    {
        Editor,  
        Bundle,
    }
    
    public class HotUpdateDefine
    {
        /// <summary>
        /// AB配置文件的AB包名标签
        /// </summary>
        public const string ABConfigTag = "_config";
        
        //ab包后缀
        public const string BundleExtension = ".bd";
        
        /// <summary>
        /// 获取热更补丁的清单的下载路径
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public static string GetHotPatchManifestPath(string moduleName)
        {
            return $"HotPatch/{moduleName.ToLower()}_PatchManifest.json";
        }

        /// <summary>
        /// 本地存储:获取热更补丁的清单的服务器目标路径
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public static string GetPersistServerHotPatchManifestPath(string moduleName)
        {
            return Application.persistentDataPath + $"/Server_{moduleName.ToLower()}_PatchManifest.json";
        }
        
        /// <summary>
        /// 本地存储:获取热更补丁的清单的本地路径
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public static string GetPersistLocalHotPatchManifestPath(string moduleName)
        {
            return Application.persistentDataPath + $"/Local_{moduleName.ToLower()}_PatchManifest.json";
        }


        /// <summary>
        /// 获取本地热更资源的存储路径
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public static string GetPersistHotAssetsPath(string moduleName)
        {
            return Application.persistentDataPath + $"/HotPatch/{moduleName.ToLower()}/";
        }
        
        /// <summary>
        /// 获取本地内嵌资源的存储路径
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public static string GetStreamingAssetsBuiltinBundlePath(string moduleName)
        {
            return Application.streamingAssetsPath + $"/AssetBundle/{moduleName.ToLower()}/";
        }
        
        /// <summary>
        /// 获取本地内嵌资源的解压存储路径
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public static string GetPersistentDecompressAssetsPath(string moduleName)
        {
            return Application.persistentDataPath + $"/DecompressAsset/{moduleName.ToLower()}/";
        }
        
        /// <summary>
        /// 获取本地内嵌资源的配置文件路径
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public static string GetBuiltinBundleInfoPath(string moduleName)
        {
            return $"{moduleName.ToLower()}_info";
        }

        /// <summary>
        /// 获取热更模块的配置AB包
        /// </summary>
        /// <returns></returns>
        public static string GetBundleConfigFileNameWithoutExtension(string moduleName)
        {
            return $"{moduleName.ToLower()}{ABConfigTag}";
        }
    }
}

