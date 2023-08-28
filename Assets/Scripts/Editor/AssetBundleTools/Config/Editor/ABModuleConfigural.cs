using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetBundleTools
{
    [CreateAssetMenu(menuName = "AssetBundle/Create AssetBundle Config",fileName = "AssetBundleConfig")]
    public class ABModuleConfigural :ScriptableObject
    {

        #region 单例加载

        private const string ABConfiguralPath = "Assets/Scripts/Editor/AssetBundleTools/AssetBundleConfig.asset";
        
        private static ABModuleConfigural ins;
        public static ABModuleConfigural Ins
        {
            get
            {
                if (ins == null)
                {
                    ins = AssetDatabase.LoadAssetAtPath<ABModuleConfigural>(ABConfiguralPath);
                }

                return ins;
            }
        }

        #endregion

        /// <summary>
        /// AB模块的配置清单
        /// </summary>
        public List<ABModuleData> AssetBundleConfig;

        /// <summary>
        ///  根据模块名 获取模块数据
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ABModuleData GetAbModuleDataByName(string name)
        {
            foreach (var item in AssetBundleConfig)
            {
                if (item.ModuleName.Equals(name))
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// 移除模块
        /// </summary>
        /// <param name="name"></param>
        public void RemoveModuleDataByName(string name)
        {
            for (int i = AssetBundleConfig.Count - 1; i >= 0; i--)
            {
                if (AssetBundleConfig[i].ModuleName.Equals(name))
                {
                    AssetBundleConfig.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// 存储新模块
        /// </summary>
        /// <param name="data"></param>
        public void SaveModuleData(ABModuleData data)
        {
            AssetBundleConfig.Add(data);
            Save();
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}

