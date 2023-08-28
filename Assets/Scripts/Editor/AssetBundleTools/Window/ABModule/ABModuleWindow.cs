using System;
using UnityEditor;
using UnityEngine;

namespace AssetBundleTools
{
    public class ABModuleWindow : BundleBehaviour
    {
        /// <summary>
        /// 添加模块按钮
        /// </summary>
        public override void DrawAddBundleModuleButton()
        {
            base.DrawAddBundleModuleButton();

            var content = EditorGUIUtility.IconContent("CollabCreate Icon");
            if (GUILayout.Button(content, GUILayout.Width(ItemSizeX), GUILayout.Height(ItemSizeY)))
            {
                //添加模块
                ABModuleDetailWindow.ShowWindow(String.Empty);
            }
        }

        
        /// <summary>
        /// 打包按钮
        /// </summary>
        public override void DrawBuildButton()
        {
            base.DrawBuildButton();
            
            GUILayout.BeginArea(new Rect(5,540,660,500));
            
            GUILayout.BeginHorizontal();

            var style = BundleEditorUtility.GetGuiStyle("PreButtonBlue");
            style.fixedHeight = 55;

            if (GUILayout.Button("打包资源", style,GUILayout.Height(200)))
            {
                BuildBundle();
            }
            
            if (GUILayout.Button("内嵌AB包", style, GUILayout.Height(200)))
            {
                CopyBundleToStreamingAssetsPath();
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.EndArea();
        }

        
        /// <summary>
        /// 打包
        /// </summary>
        public override void BuildBundle()
        {
            base.BuildBundle();
            foreach (var data in moduleDataLst)
            {
                if (data.IsBuild)
                {
                    //打包
                    BuildBundlePipeLine.BuildAssetBundle(data);
                }
            }
        }

        /// <summary>
        /// 内嵌资源
        /// </summary>
        public void CopyBundleToStreamingAssetsPath()
        {
            foreach (var data in moduleDataLst)
            {
                if (data.IsBuild)
                {
                    BuildBundlePipeLine.CopyBundlesToStreamingAssets(data,true);
                }
            }
        }
    }
}

