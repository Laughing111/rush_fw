using System;
using Game.Runtime;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace AssetBundleDataConfig
{
    [CreateAssetMenu(menuName = "AssetBundle/Create AssetBundle Settings",fileName = "AssetBundleSettings")]
    public class ABSettings : ScriptableObject
    {
        #region 单例加载

        private const string ABSettingsPath = "BuiltinConfig/AssetBundleSettings";
        
        private static ABSettings ins;
        public static ABSettings Ins
        {
            get
            {
                if (ins == null)
                {
#if UNITY_EDITOR
                    ins = AssetDatabase.LoadAssetAtPath<ABSettings>($"Assets/Resources/{ABSettingsPath}.asset");
#else
                    ins = Resources.Load<ABSettings>(ABSettingsPath);
#endif

                }

                return ins;
            }
        }

        #endregion

        [TitleGroup("资源加载热更设置"),LabelText("AssetBundle下载地址")]
        public string AssetBundleDownloadUrl;

        [TitleGroup("打包设置")]
        [LabelText("是否加密AB包")]
        public BundleEncryptToggle EncryptToggle = new BundleEncryptToggle();


        [TitleGroup("打包设置")] 
        [LabelText("资源打包平台")]
        public BuildTarget CurBuildTarget;
        
        [TitleGroup("打包设置")] 
        [LabelText("压缩格式")]
        public BuildAssetBundleOptions BuildOptions;

        [TitleGroup("资源热更设置")] 
        [LabelText("资源热更模式")]
        public BundleHotUpdateType BundleHotType;

        [TitleGroup("资源热更设置")] 
        [LabelText("资源下载的最大线程数")]
        public int BundleDownloadMaxThread;

        [TitleGroup("资源加载设置")] 
        [LabelText("加载方式")]
        public LoadAssetMode AssetLoadMode;
    }

    [Serializable,Toggle("IsEncrypt")]
    public class BundleEncryptToggle
    {
        /// <summary>
        /// 是否加密
        /// </summary>
        public bool IsEncrypt;

        [LabelText("密钥")]
        public string EncryptKey;
    }
}

