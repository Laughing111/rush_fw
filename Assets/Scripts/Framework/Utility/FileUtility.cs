using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>
    /// 文件IO工具类
    /// </summary>
    public class FileUtility
    {
        public static void WriteFile(string path,string content)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var writer = File.CreateText(path);
            writer.Write(content);
            writer.Dispose();
            writer.Close();
        }
        
        public static void WriteFile(string path,byte[] content)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var writer = File.Create(path);
            writer.Write(content,0,content.Length);
            writer.Dispose();
            writer.Close();
        }
    }
}

