using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundleTools
{
    public class ABModuleConvertTools
    {

        public static string ABModuleNameEnumFilePath = Application.dataPath + "/Scripts/Editor/AssetBundleTools/Config/ABModuleEnum.cs";
        
        public static void ConvertEnumFileForAllABModule()
        {
            var modules = ABModuleConfigural.Ins.AssetBundleConfig;
            if (modules == null || modules.Count <= 0)
            {
                return;
            }
            
            var nameSpace = "AssetBundleTools";
            var name = "ABModuleEnum";

            if (File.Exists(ABModuleNameEnumFilePath))
            {
                File.Delete(ABModuleNameEnumFilePath);
                AssetDatabase.Refresh();
            }

            var writer = File.CreateText(ABModuleNameEnumFilePath);
            writer.WriteLine("/* -------------------------------");
            writer.WriteLine("/* ------- Auto Generate ---------");
            writer.WriteLine("/* Description: Represents each assetBundle module which is used to download on loaded");
            writer.WriteLine("------------------------------- */");
            
            writer.WriteLine($"namespace {nameSpace}");
            writer.WriteLine("{");
            
            writer.WriteLine($"\tpublic enum {name}");
            writer.WriteLine("\t{");

            for (int i = 0; i < modules.Count; i++)
            {
                writer.WriteLine($"\t\t{modules[i].ModuleName},");
            }
            
            writer.WriteLine("\t}");
            
            writer.WriteLine("}");
            writer.Close();
            
            AssetDatabase.Refresh();
        }
    }
}

