using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleTools
{
    public class BundleEditorUtility
    {
        public static GUIStyle GetGuiStyle(string styleName)
        {
            GUIStyle style = null;
            foreach (var item in GUI.skin.customStyles)
            {
                if (string.Equals(item.name.ToLower(), styleName.ToLower()))
                {
                    style = item;
                    break;
                }
            }

            return style;
        }
    }
}

