using UnityEditor;
using UnityEngine;

namespace AssetBundleTools
{
    public class BuildHotPatchWindow : BundleBehaviour
    {
        /// <summary>
        /// 热更补丁版本号
        /// </summary>
        [HideInInspector]
        public string PatchVersion = "1";
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

            if (GUILayout.Button("打包热更", style,GUILayout.Height(200)))
            {
                BuildPatch();
            }
            
            if (GUILayout.Button("上传资源", style, GUILayout.Height(200)))
            {
                UploadPatch();
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.EndArea();
        }

        
        /// <summary>
        /// 打包
        /// </summary>
        public override void BuildPatch()
        {
            base.BuildPatch();
            foreach (var data in moduleDataLst)
            {
                if (data.IsBuild)
                {
                    if (int.TryParse(PatchVersion, out var version))
                    {
                        //打包
                        BuildBundlePipeLine.BuildAssetBundle(data,BuildType.HotPatch,version);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("打补丁包", "补丁版本号有误！请检查！", "ok");
                    }
                    
                }
            }
        }

        /// <summary>
        /// 上传资源
        /// </summary>
        public void UploadPatch()
        {
            foreach (var data in moduleDataLst)
            {
                if (data.IsBuild)
                {
                    //
                }
            }
        }


        public override void DrawGUI()
        {
            base.DrawGUI();
            GUILayout.BeginArea(new Rect(5,500,660,500));
            
            GUILayout.BeginHorizontal();

            PatchVersion = EditorGUILayout.TextField("热更包版本号", PatchVersion, GUILayout.Width(650), GUILayout.Height(24));
            
            GUILayout.EndHorizontal();
            
            GUILayout.EndArea();
        }
    }
}

