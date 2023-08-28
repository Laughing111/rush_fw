using System.Collections;
using System.Collections.Generic;
using AssetBundleDataConfig;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace AssetBundleTools
{
    public class AssetBundleBuildWindow : OdinMenuEditorWindow
    {
        public ABModuleWindow ModuleWindow = new ABModuleWindow();
        
        public BuildHotPatchWindow HotPatchWindow = new BuildHotPatchWindow();

        public const int BetterWindowWith = 850;

        public const int BetterWindowHeight = 600;
        
        protected override OdinMenuTree BuildMenuTree()
        {
            ModuleWindow.Initial();
            HotPatchWindow.Initial();
            OdinMenuTree tree = new OdinMenuTree(false)
            {
                {
                    "Build", null, EditorIcons.SettingsCog
                },
                {
                    "Build/AssetBundle",ModuleWindow,EditorIcons.UnityFolderIcon
                },
                {
                    "Build/HotPatchWindow",HotPatchWindow,EditorIcons.Paperclip
                },
                {
                    "Build/BundleSettings",ABSettings.Ins,EditorIcons.SettingsCog
                }
                

            };

            return tree;
        }
    }

}
