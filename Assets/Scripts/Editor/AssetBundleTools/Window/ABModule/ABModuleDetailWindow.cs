using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace AssetBundleTools
{
    public class ABModuleDetailWindow : OdinEditorWindow
    {
        [Required("请输入资源模块名称")]
        [LabelText("资源模块名 ")]
        [PropertySpace(spaceAfter:5,spaceBefore:5)]
        public string moduleName;

        [TabGroup("预制体包")]
        [HideLabel]
        [ReadOnly]
        [DisplayAsString]
        public string PrefabTable = "该文件夹下的所有预制体都会单独打成一个AB";
        
        
        [TabGroup("文件夹子包")]
        [HideLabel]
        [ReadOnly]
        [DisplayAsString]
        public string RootFolderSubBundleTable = "该文件夹下的所有子文件夹都会单独打成一个AB";
        
        [TabGroup("单个包")]
        [HideLabel]
        [ReadOnly]
        [DisplayAsString]
        public string SingleBundleTable = "指定文件夹单独打成一个AB";

        
        [FolderPath]
        [TabGroup("预制体包")] 
        [LabelText("预制体资源路径")]
        public string[] PrefabPath;

        
        [FolderPath] 
        [TabGroup("文件夹子包")] 
        [LabelText("文件夹子包路径")]
        public string[] RootFolderPath;

        [TabGroup("单个包")] 
        [LabelText("单个包路径")] 
        public BundleFileInfo[] SingleBundlePath;

        public static void ShowWindow(string moduleName)
        {
            ABModuleDetailWindow window = GetWindowWithRect<ABModuleDetailWindow>(new Rect(0, 0, 600, 600));
            window.Show();
            //更新窗口信息
            var module = ABModuleConfigural.Ins.GetAbModuleDataByName(moduleName);
            if (module != null)
            {
                window.moduleName = moduleName;
                window.PrefabPath = module.PrefabPath;
                window.RootFolderPath = module.RootFolderPath;
                window.SingleBundlePath = module.SingleBundlePath;
            }
        }

        /// <summary>
        /// 存储模块配置
        /// </summary>
        [OnInspectorGUI]
        public void DrawSaveModuleConfigButton()
        {
            GUILayout.BeginArea(new Rect(0,510,600,200));
            if (GUILayout.Button("Delete", GUILayout.Height(47)))
            {
                DeleteModuleConfig();
            }
            GUILayout.EndArea();
            
            GUILayout.BeginArea(new Rect(0,555,600,200));
            if (GUILayout.Button("Save", GUILayout.Height(47)))
            {
                SaveModuleConfig();
            }
            GUILayout.EndArea();
        }

        /// <summary>
        /// 删除模块配置
        /// </summary>
        public void DeleteModuleConfig()
        {
            ABModuleConfigural.Ins.RemoveModuleDataByName(moduleName);
            UnityEditor.EditorUtility.DisplayDialog("模块配置", "删除成功！", "ok");
            Close();
            AssetBundleBuildMenu.ShowAssetBundleBuildWindow();
        }

        /// <summary>
        /// 保存模块配置
        /// </summary>
        public void SaveModuleConfig()
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                UnityEditor.EditorUtility.DisplayDialog("模块配置", "保存失败，模块名称不能为空！", "ok");
                return;
            }
            var module = ABModuleConfigural.Ins.GetAbModuleDataByName(moduleName);
            if (module == null)
            {
                module = new ABModuleData();
                module.ModuleName = moduleName;
                module.PrefabPath = PrefabPath;
                module.RootFolderPath = RootFolderPath;
                module.SingleBundlePath = SingleBundlePath;
                ABModuleConfigural.Ins.SaveModuleData(module);
            }
            else
            {
                module.PrefabPath = PrefabPath;
                module.RootFolderPath = RootFolderPath;
                module.SingleBundlePath = SingleBundlePath;
                ABModuleConfigural.Ins.Save();
            }
            
            UnityEditor.EditorUtility.DisplayDialog("模块配置", "保存成功！", "ok");
            Close();
            AssetBundleBuildMenu.ShowAssetBundleBuildWindow();
        }
    }
}

