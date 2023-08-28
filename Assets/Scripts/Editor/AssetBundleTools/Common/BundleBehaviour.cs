using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace AssetBundleTools
{
    public class BundleBehaviour
    {
        /// <summary>
        /// 模块的配置列表
        /// </summary>
        protected List<ABModuleData> moduleDataLst;
        
        /// <summary>
        /// 行列表
        /// </summary>
        protected List<List<ABModuleData>> moduleDataRowLst;

        /// <summary>
        /// 每一行最多排列多少个模块
        /// </summary>
        private const int MaxRowItemCount = 5;

        /// <summary>
        /// 每一个模块按钮的尺寸
        /// </summary>
        protected const int ItemSizeX= 130;
        
        protected const int ItemSizeY= 150;

        /// <summary>
        /// 模块的显示Icon
        /// </summary>
        private static GUIContent ItemContent;

        private static GUIStyle LableSelectStyle = new GUIStyle();

        private static GUIStyle LableUnSelectStyle = new GUIStyle();
        
        
        public virtual void Initial()
        {
            moduleDataLst = ABModuleConfigural.Ins.AssetBundleConfig;
            moduleDataRowLst = new List<List<ABModuleData>>();
            for (int i = 0; i < moduleDataLst.Count; i++)
            {
                int index = Mathf.FloorToInt(i / MaxRowItemCount);
                if (moduleDataRowLst.Count <= index + 1)
                {
                    moduleDataRowLst.Add(new List<ABModuleData>());
                }
                
                //往行列表中添加模块配置
                moduleDataRowLst[index].Add(moduleDataLst[i]);
            }
            
            ItemContent = EditorGUIUtility.IconContent("SceneAsset Icon");
            ItemContent.tooltip = "单击选中/取消\n双击打开模块窗口";

            
            LableSelectStyle.alignment = TextAnchor.MiddleCenter;
            LableSelectStyle.normal.textColor = Color.yellow;

            LableUnSelectStyle.alignment = TextAnchor.MiddleCenter;
            LableUnSelectStyle.normal.textColor = Color.white;


        }

        [OnInspectorGUI]
        public virtual void DrawGUI()
        {
            if (moduleDataRowLst == null) return;

            for (int col = 0; col < moduleDataRowLst.Count; col++)
            {
                var rowDatas = moduleDataRowLst[col];
                //开始横向绘制
                GUILayout.BeginHorizontal();
                for (int row = 0; row < rowDatas.Count; row++)
                {
                    var data = rowDatas[row];
                    DrawModuleDataButton(data,col,row);
                }

                if (col == moduleDataRowLst.Count - 1)
                {
                    //绘制添加模块按钮
                    DrawAddBundleModuleButton();
                }
                
                GUILayout.EndHorizontal();
            }

            if (moduleDataRowLst.Count <= 0)
            {
                //绘制添加模块按钮
                DrawAddBundleModuleButton();
            }

            //绘制打包按钮
            DrawBuildButton();
        }
        
        /// <summary>
        /// 绘制模块按钮
        /// </summary>
        /// <param name="data"></param>
        /// <param name="colIndex"></param>
        /// <param name="rowIndex"></param>
        private void DrawModuleDataButton(ABModuleData data,int colIndex,int rowIndex)
        {
            var click = GUILayout.Button(ItemContent,
                GUILayout.Width(ItemSizeX), GUILayout.Height(ItemSizeY));
            
            if (click)
            {
                data.IsBuild = !data.IsBuild;
                var doubleClick = Time.realtimeSinceStartup - data.LastClickTime <= 0.18f;
                if (doubleClick)
                {
                    //双击呼出 资源模块的配置窗口
                    ABModuleDetailWindow.ShowWindow(data.ModuleName);
                }
                data.LastClickTime = Time.realtimeSinceStartup;
            }

            GUIStyle style = LableUnSelectStyle;
            if (data.IsBuild)
            {
                style = LableSelectStyle;
            }
            
            GUI.Label(
                new Rect(colIndex * 5 + rowIndex * 135,ItemSizeY * (colIndex + 1) - 16,ItemSizeX,20),
                data.ModuleName,
                style);
        }

        
        /// <summary>
        /// 绘制打包按钮
        /// </summary>
        public virtual void DrawBuildButton()
        {
            
        }
        
        
        /// <summary>
        /// 打包资源
        /// </summary>
        public virtual void BuildBundle()
        {
            
        }

        /// <summary>
        /// 打包热更补丁
        /// </summary>
        public virtual void BuildPatch()
        {
            
        }
        
        /// <summary>
        /// 绘制添加模块按钮
        /// </summary>
        public virtual void DrawAddBundleModuleButton()
        {
            
        }
        
    }
}

