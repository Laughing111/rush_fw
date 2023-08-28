using System;
using System.Collections.Generic;
using System.IO;
using AssetBundleDataConfig;
using Game.Runtime;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace AssetBundleTools
{

    public enum BuildType
    {
        AssetBundle,   //资源包
        HotPatch,      //补丁包
    }
    public static class BuildBundlePipeLine
    {

        /// <summary>
        /// 补丁包版本
        /// </summary>
        private static int hotPatchVersion;

        /// <summary>
        /// 打包类型
        /// </summary>
        private static BuildType curBuildType;

        /// <summary>
        /// 需要打包的模块数据
        /// </summary>
        private static ABModuleData moduleData;

        /// <summary>
        /// 所有AB包的路径列表
        /// </summary>
        private static List<string> allBundlePathLst = new();

        /// <summary>
        /// 存储所有文件夹中的AB包 的路径字典
        /// </summary>
        private static Dictionary<string, List<string>> allFolderBundlePath = new();

        /// <summary>
        /// 存储 所有预制件的AB包路径字典
        /// </summary>
        private static Dictionary<string, List<string>> allPrefabsBundlePath = new();
        
        //AB包输出路径
        private static string bundleOutputPath
        {
            get
            {
                return Application.dataPath + $"/../AssetBundle/{moduleData.ModuleName}/{ABSettings.Ins.CurBuildTarget.ToString()}/";
            }
        }

        public static string GetBundleOutPutPath(string moduleName)
        {
            return Application.dataPath + $"/../AssetBundle/{moduleName}/{ABSettings.Ins.CurBuildTarget.ToString()}/";
        }
        
        //热更补丁输出路径
        private static string patchOutputPath
        {
            get
            {
                return Application.dataPath + $"/../HotPatch/{moduleData.ModuleName}/{hotPatchVersion.ToString()}/{ABSettings.Ins.CurBuildTarget.ToString()}/";
            }
        }
        
        //热更补丁输出路径
        private static string patchManifestPath
        {
            get
            {
                return Application.dataPath + $"/../{HotUpdateDefine.GetHotPatchManifestPath(moduleData.ModuleName)}";
            }
        }

        private static string ResourcesPath
        {
            get
            {
                return Application.dataPath + $"/Resources/";
            }
        }

        /// <summary>
        /// 打包
        /// </summary>
        /// <param name="data"></param>
        /// <param name="buildType"></param>
        /// <param name="patchVersion"></param>
        public static void BuildAssetBundle(ABModuleData data,BuildType buildType = BuildType.AssetBundle,int patchVersion = 0)
        {
            //初始化
            Init(data, buildType, patchVersion);

            //打包所有文件夹
            BuildFolderToBundle();
            
            //打包父文件夹下的所有子文件夹
            BuildSubFolderFromRootDirToBundle();

            //打包所有预制件
            BuildAllPrefabsToBundle();
            
            //开始打包
            InternalBuildAssetBundles();
            
            Dispose();
        }

        private static void Init(ABModuleData data,BuildType buildType = BuildType.AssetBundle,int patchVersion = 0)
        {
            allBundlePathLst.Clear();
            allFolderBundlePath.Clear();
            allPrefabsBundlePath.Clear();
            
            curBuildType = buildType;
            hotPatchVersion = patchVersion;
            moduleData = data;

            if (Directory.Exists(bundleOutputPath))
            {
                Directory.Delete(bundleOutputPath,true);
            }

            Directory.CreateDirectory(bundleOutputPath);
        }
        
        /// <summary>
        /// 整个文件夹 打成一个AB包
        /// </summary>
        private static void BuildFolderToBundle()
        {
            if (moduleData.SingleBundlePath == null || moduleData.SingleBundlePath.Length <= 0) return;

            foreach (var info in moduleData.SingleBundlePath)
            {
                //文件夹路径
                var path = info.BundlePath.Replace(@"\", "/");
                if (!Directory.Exists(path))
                {
                    Debug.LogError($"资源文件夹不存在，请检查！{path}");
                    continue;
                }
                if (!ValidBundleFile(path))
                {
                    Debug.LogError($"AB文件不合法，请检查！{path}");
                    continue;
                }
                //拼接AB包名
                var bundleName =  GetBundleName(info.ABName);
                
                AddBundleInfoToAllBundlePathLstLstAndallFolderBundleDict(bundleName, path);
            }
            
        }

        
        /// <summary>
        /// 文件夹内的所有子文件夹，依次打成AB包
        /// </summary>
        private static void BuildSubFolderFromRootDirToBundle()
        {
            if (moduleData.RootFolderPath == null || moduleData.RootFolderPath.Length <= 0) return;

            foreach (var dirPath in moduleData.RootFolderPath)
            {
                var parentDirPath = dirPath.Replace(@"\", "/");
                var dirInfo = new DirectoryInfo(parentDirPath);
                if (!dirInfo.Exists)
                {
                    Debug.LogError($"资源文件夹不存在，请检查！{parentDirPath}");
                    continue;
                }
                //获取所有子文件夹
                var childInfos = dirInfo.GetDirectories();
                foreach (var childDirInfo in childInfos)
                {
                    //子文件夹名 即 AB名
                    var srcAbName = childDirInfo.Name;
                    var abName = GetBundleName(srcAbName);
                    var path = $"{parentDirPath}/{srcAbName}";
                    if (!ValidBundleFile(path))
                    {
                        Debug.LogError($"AB文件不合法或AB包重复，请检查！{path}");
                        continue;
                    }

                    AddBundleInfoToAllBundlePathLstLstAndallFolderBundleDict(abName, path);
                    
                    //遍历子文件夹中的资源
                    var files = childDirInfo.GetFiles("*");
                    foreach (var fileInfo in files)
                    {
                        if (fileInfo.Extension.EndsWith(".meta"))
                        {
                            continue;
                        }

                        var filePath = $"{parentDirPath}/{srcAbName}/{fileInfo.Name}";
                        if (!ValidBundleFile(filePath))
                        {
                            Debug.LogError($"AB文件不合法或AB包重复，请检查！{filePath}");
                            continue;
                        }
                        AddBundleInfoToAllBundlePathLstLstAndallFolderBundleDict(abName, filePath);
                    }

                }
            }
        }


        /// <summary>
        /// 所有预制件 依次打成一个AB包
        /// </summary>
        private static void BuildAllPrefabsToBundle()
        {
            if (moduleData.PrefabPath == null || moduleData.PrefabPath.Length <= 0) return;
            
            //获取所有预制件的GUID
            var guidArr = AssetDatabase.FindAssets("t:Prefab", moduleData.PrefabPath);
            foreach (var guid in guidArr)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                //拼接AB包名
                var abName = GetBundleName(Path.GetFileNameWithoutExtension(path));
                if (!allBundlePathLst.Contains(abName))
                {
                    //分析预制件的依赖资源
                    //这里依赖资源会包含这个预制件本身
                     var dependencies = AssetDatabase.GetDependencies(path);
                     var lst = new List<string>();
                     foreach (var depPath in dependencies)
                     {
                         if (!ValidBundleFile(depPath))
                         {
                             continue;
                         }
                         
                         allBundlePathLst.Add(depPath);
                         lst.Add(depPath);
                     }

                     if (!allPrefabsBundlePath.ContainsKey(abName))
                     {
                         allPrefabsBundlePath.Add(abName,lst);
                     }
                     else
                     {
                         Debug.LogError($"重复的预制件AB包名，当前模块下有预制件文件重复：{abName}");
                     }
                }
            }
        }

        /// <summary>
        /// 将文件夹AB包 添加到所有bundle列表 和 文件夹AB包的字典中
        /// </summary>
        private static void AddBundleInfoToAllBundlePathLstLstAndallFolderBundleDict(string abName,string abPath)
        {
            allBundlePathLst.Add(abPath);

            if (!allFolderBundlePath.ContainsKey(abName))
            {
                allFolderBundlePath.Add(abName,new List<string>());   
            }
            allFolderBundlePath[abName].Add(abPath);
        }


        private static void InternalBuildAssetBundles()
        {
            try
            {
                //修改所有要打包的文件的AB包名字
                ModifyAllFileBundleName();
                //生成一份AB包配置
                GenerateAssetBundleConfig();
                AssetDatabase.Refresh();

                var buildTarget = (UnityEditor.BuildTarget)ABSettings.Ins.CurBuildTarget;
                var buildOpt = (UnityEditor.BuildAssetBundleOptions)ABSettings.Ins.BuildOptions;
                var manifest = BuildPipeline.BuildAssetBundles(bundleOutputPath, buildOpt, buildTarget);
                if (manifest == null)
                {
                    Debug.LogError($"打AB模块包失败-{moduleData.ModuleName}");
                }
                else
                {
                    //删除所有manifest
                    DeleteBundleManifest();
                    //加密AB包
                    EncryptAllBundles();
                    Debug.Log($"打AB模块包成功-{moduleData.ModuleName}");
                    if (curBuildType == BuildType.HotPatch)
                    {
                        //生成热更补丁
                        GenerateHotPatchAsset();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            
            //清理AB设置
            ModifyAllFileBundleName(true);

        }

        /// <summary>
        /// 删除所有AB包自动生成的Manifest
        /// </summary>
        public static void DeleteBundleManifest()
        {
            var allFilePath = Directory.GetFiles(bundleOutputPath);
            foreach (var path in allFilePath)
            {
                if (path.EndsWith(".manifest"))
                {
                    File.Delete(path);
                }
            }
        }

        /// <summary>
        /// 生成AB配置
        /// </summary>
        private static void GenerateAssetBundleConfig()
        {
            var config = new AssetBundleConfig();
            config.BundleInfoList = new List<AssetBundleInfo>();

            //所有AB文件的字典 key-路径 value-AB名
            var allABFilePathMap = new Dictionary<string, string>();
            //获取所有AB
            var allBundlerNames = AssetDatabase.GetAllAssetBundleNames();

            foreach (var abName in allBundlerNames)
            {
                //获取指定AB包名的所有AB资源路径
                var bundleAssets = AssetDatabase.GetAssetPathsFromAssetBundle(abName);

                foreach (var assetPath in bundleAssets)
                {
                    if (!assetPath.EndsWith(".cs"))
                    {
                        allABFilePathMap.Add(assetPath,abName);
                    }
                }
            }
            
            //计算AssetBundle数据 生成AssetBundle配置文件
            foreach (var item in allABFilePathMap)
            {
                var path = item.Key;
                AssetBundleInfo info = new AssetBundleInfo();
                info.Path = path;
                info.BundleName = item.Value;
                info.AssetName = Path.GetFileName(path);
                info.Crc = Crc32.GetCrc32(path);
                info.BundleDependencies = new List<string>();
                var dependencies = AssetDatabase.GetDependencies(path);
                foreach (var depItem in dependencies)
                {
                    //依赖项不能是自己 或 cs脚本
                    if (depItem.Equals(path) || depItem.EndsWith(".cs"))
                    {
                        continue;
                    }

                    if (allABFilePathMap.TryGetValue(depItem,out var assetBundleName))
                    {
                        //避免重复添加
                        if (!info.BundleDependencies.Contains(assetBundleName))
                        {
                            info.BundleDependencies.Add(assetBundleName);
                        }
                    }
                }
                
                //添加入config
                config.BundleInfoList.Add(info);

            }
            
            //生成json文件
            CreateBundleConfigFile(config);
        }


        private static void CreateBundleConfigFile(AssetBundleConfig config)
        {
            //生成配置文件 缩进格式
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            string configPath = GetBundleConfigFileSavePath(false);
            FileUtility.WriteFile(configPath,System.Text.Encoding.UTF8.GetBytes(json));

            //修改配置文件的ab包名
            var importer = AssetImporter.GetAtPath(configPath.Replace(Application.dataPath, "Assets"));
            if (importer != null)
            {
                importer.assetBundleName = $"{moduleData.ModuleName.ToLower()}_config{HotUpdateDefine.BundleExtension}";
            }

        }
        /// <summary>
        /// 每一个AB包的配置文件存储路径
        /// </summary>
        /// <returns></returns>
        private static string GetBundleConfigFileSavePath(bool assetPath)
        {
            var preTag = assetPath ? "Assets" : Application.dataPath;
            return preTag + $"/{moduleData.ModuleName.ToLower()}{HotUpdateDefine.ABConfigTag}.json";
        }

        /// <summary>
        /// 修改或清空AssetBundle
        /// </summary>
        /// <param name="clear"></param>
        private static void ModifyAllFileBundleName(bool clear = false)
        {
            var index = 0;
            //修改所有文件夹下的ABName
            foreach (var item in allFolderBundlePath)
            {
                index++;
                EditorUtility.DisplayProgressBar("配置AB包",$"配置AB文件 {item.Key}",index*1.0f/allFolderBundlePath.Count);
                foreach (var path in item.Value)
                {
                    var importer = AssetImporter.GetAtPath(path);
                    if(importer == null) continue;
                    importer.assetBundleName = (clear ? string.Empty : item.Key + HotUpdateDefine.BundleExtension);
                }
                EditorUtility.ClearProgressBar();
            }
            
            //修改所有预制件的BundleName
            index = 0;
            foreach (var item in allPrefabsBundlePath)
            {
                index++;
                var bundleList = item.Value;
                foreach (var path in bundleList)
                {
                    EditorUtility.DisplayProgressBar("配置AB包",$"配置AB文件 {item.Key}",index*1.0f/allPrefabsBundlePath.Count);
                    var importer = AssetImporter.GetAtPath(path);
                    if(importer == null) continue;
                    importer.assetBundleName = (clear ? string.Empty : item.Key + HotUpdateDefine.BundleExtension);
                }
            }
            EditorUtility.ClearProgressBar();

            //移除未使用的ABName
            if (clear)
            {
                var configAssetPath = GetBundleConfigFileSavePath(true);
                //修改配置文件的ab包名
                var importer = AssetImporter.GetAtPath(configAssetPath);
                if (importer != null)
                {
                    importer.assetBundleName = string.Empty;
                }
                AssetDatabase.RemoveUnusedAssetBundleNames();
            }
        }

        /// <summary>
        /// 校验打成AB包的文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool ValidBundleFile(string path)
        {
            //如果是脚本 不合法
            if (path.EndsWith(".cs"))
            {
                return false;
            }
            foreach (var bundlePath in allBundlePathLst)
            {
                //重复 不合法
                if (bundlePath.Equals(path) || bundlePath.Contains(path))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 防止多模块中，AB包重名，因此最终的AB包名用模块名进行拼接
        /// </summary>
        /// <param name="srcABName"></param>
        /// <returns></returns>
        private static string GetBundleName(string srcABName)
        {
            return moduleData.ModuleName + "_" + srcABName;
        }


        /// <summary>
        /// 加密所有bundle
        /// </summary>
        private static void EncryptAllBundles()
        {
            if (!ABSettings.Ins.EncryptToggle.IsEncrypt)
            {
                return;
            }
            
            DirectoryInfo dirInfo = new DirectoryInfo(bundleOutputPath);
            var filesInfo = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            for(int i = 0;i<filesInfo.Length;i++)
            {
                var info = filesInfo[i];
                EditorUtility.DisplayProgressBar("加密AB",$"{info.Name}",i * 1.0f /filesInfo.Length);
                
                //todo 加密秘钥
                AES.AESFileEncrypt(info.FullName,ABSettings.Ins.EncryptToggle.EncryptKey);
                
            }
            EditorUtility.ClearProgressBar();
            Debug.Log( "AB包加密成功！");
        }

        /// <summary>
        /// 拷贝AB包StreamIngAssets文件夹
        /// </summary>
        /// <param name="showTips"></param>
        public static void CopyBundlesToStreamingAssets(ABModuleData module, bool showTips)
        {
            var abOutPutPath = GetBundleOutPutPath(module.ModuleName);
            DirectoryInfo dirInfo = new DirectoryInfo(abOutPutPath);
            var filesInfo = dirInfo.GetFiles("*", SearchOption.AllDirectories);

            //目录创建和管理
            if (filesInfo.Length <= 0)
            {
                Debug.LogError("AB包为空，无需拷贝到StreamingAssets目录！");
                return;
            }

            var streamingAssetsModulePath = HotUpdateDefine.GetStreamingAssetsBuiltinBundlePath(module.ModuleName);
            if (Directory.Exists(streamingAssetsModulePath))
            {
                Directory.Delete(streamingAssetsModulePath,true);
            }

            Directory.CreateDirectory(streamingAssetsModulePath);

            var configInfoList = new List<BuiltinBundleInfo>();
            for(int i = 0;i<filesInfo.Length;i++)
            {
                var info = filesInfo[i];
                EditorUtility.DisplayProgressBar("拷贝AB包到StreamingAssets目录",$"{info.Name}",i * 1.0f /filesInfo.Length);
                File.Copy(info.FullName,streamingAssetsModulePath + $"/{info.Name}");
                
                //生成内嵌资源的文件信息
                var builtinInfo = new BuiltinBundleInfo();
                builtinInfo.FileName = info.Name;
                builtinInfo.MD5 = MD5.GetMd5FromFile(info.FullName);
                builtinInfo.SizeKB = info.Length * 1.0f / 1024;
                configInfoList.Add(builtinInfo);
            }

            //写入本地
            if (!Directory.Exists(ResourcesPath))
            {
                Directory.CreateDirectory(ResourcesPath);
            }
            
            var builtinConfigPath = ResourcesPath + $"{HotUpdateDefine.GetBuiltinBundleInfoPath(module.ModuleName)}.json";
            CreateBuiltinConfigFile(builtinConfigPath,configInfoList);
            EditorUtility.ClearProgressBar();
            if (showTips)
            {
                EditorUtility.DisplayDialog("拷贝AB包到StreamingAssets目录", $"拷贝成功！{streamingAssetsModulePath}", "ok");
            }
            Debug.Log($"拷贝AB包到StreamingAssets目录,{streamingAssetsModulePath}");
        }

        /// <summary>
        /// 生成内置的AB配置文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="builtinConfig"></param>
        private static void CreateBuiltinConfigFile(string path,List<BuiltinBundleInfo> builtinConfig)
        {
            var json = JsonConvert.SerializeObject(builtinConfig,Formatting.Indented);
            
            FileUtility.WriteFile(path, System.Text.Encoding.UTF8.GetBytes(json));
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 生成热更补丁
        /// </summary>
        private static void GenerateHotPatchAsset()
        {
            if (Directory.Exists(patchOutputPath))
            {
                Directory.Delete(patchOutputPath,true);
            }

            Directory.CreateDirectory(patchOutputPath);

            var patchesFile =  Directory.GetFiles(bundleOutputPath, "*" + HotUpdateDefine.BundleExtension);
            for (int i = 0; i < patchesFile.Length; i++)
            {
                var path = patchesFile[i];
                EditorUtility.DisplayProgressBar("生成热更补丁文件",$"{path}",i * 1.0f /patchesFile.Length);
                var dstPath = patchOutputPath + Path.GetFileName(path);
                
                File.Copy(path,dstPath);
            }
            EditorUtility.ClearProgressBar();
            
            //生成补丁清单
            GenerateHotPatchAssetManifest();
            Debug.Log("热更补丁生成成功！");
        }

        /// <summary>
        /// 生成热更补丁的资源清单
        /// </summary>
        public static void GenerateHotPatchAssetManifest()
        {
            //清单
            HotPatchManifest patchCfg = new HotPatchManifest();
            patchCfg.DownLoadUrl = ABSettings.Ins.AssetBundleDownloadUrl + $"/HotPatch/{moduleData.ModuleName}/{hotPatchVersion}/{ABSettings.Ins.CurBuildTarget}";
            patchCfg.PatchAssetList = new List<HotPatchAsset>();
            //补丁资源
            var patchAsset = new HotPatchAsset();
            patchAsset.PatchVersion = hotPatchVersion;
            patchAsset.PatchInfoList = new List<HotPatchInfo>();

            //补丁资源的文件信息
            DirectoryInfo dirInfo = new DirectoryInfo(patchOutputPath);
            var bundleInfos = dirInfo.GetFiles("*" + HotUpdateDefine.BundleExtension);
            foreach (var bundleInfo in bundleInfos)
            {
                var patchInfo = new HotPatchInfo();
                patchInfo.ABName = bundleInfo.Name;
                patchInfo.MD5 = MD5.GetMd5FromFile(bundleInfo.FullName);
                patchInfo.SizeK = bundleInfo.Length * 1.0f / 1024;
                patchAsset.PatchInfoList.Add(patchInfo);
            }
            patchCfg.PatchAssetList.Add(patchAsset);
            
            //生成清单文件
            if (File.Exists(patchManifestPath))
            {
                File.Delete(patchManifestPath);
            }
            var json = JsonConvert.SerializeObject(patchCfg,Formatting.Indented);
            FileUtility.WriteFile(patchManifestPath,System.Text.Encoding.UTF8.GetBytes(json));
        }
        

        private static void Dispose()
        {
            allBundlePathLst.Clear();
            allFolderBundlePath.Clear();
            allPrefabsBundlePath.Clear();
        }

    }
}

