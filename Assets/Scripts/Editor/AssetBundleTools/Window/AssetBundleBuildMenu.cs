using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;

namespace AssetBundleTools
{
    public class AssetBundleBuildMenu
    {
        public const string MenuRootName = "Tools/AssetBundle";
        
        [MenuItem(MenuRootName+"/BuildWindow")]
        public static void ShowAssetBundleBuildWindow()
        {
            var window = EditorWindow.GetWindow<AssetBundleBuildWindow>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(AssetBundleBuildWindow.BetterWindowWith, AssetBundleBuildWindow.BetterWindowHeight);
            window.ForceMenuTreeRebuild();
        }
        
        [MenuItem(MenuRootName + "/GenerateModuleEnum")]
        public static void GenerateABModuleEnum()
        {
            ABModuleConvertTools.ConvertEnumFileForAllABModule();
        }
    }  
}

