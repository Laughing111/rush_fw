using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleDataConfig
{
    public enum BuildTarget
    {
        /// <summary>
        ///   <para>Build a Windows standalone.</para>
        /// </summary>
        StandaloneWindows = 5,

        /// <summary>
        ///   <para>Build an iOS player.</para>
        /// </summary>
        iOS = 9,

        /// <summary>
        ///   <para>Build an Android .apk standalone app.</para>
        /// </summary>
        Android = 13, // 0x0000000D

        /// <summary>
        ///   <para>Build a PS4 Standalone.</para>
        /// </summary>
        PS4 = 31, // 0x0000001F

        /// <summary>
        ///   <para>Build a Nintendo Switch player.</para>
        /// </summary>
        Switch = 38, // 0x00000026

        /// <summary>
        ///   <para>Build to PlayStation 5 platform.</para>
        /// </summary>
        PS5 = 44, // 0x0000002C
    }
    
    public enum BuildAssetBundleOptions
  {
    /// <summary>
    ///   <para>Build assetBundle without any special option.</para>
    /// </summary>
    None = 0,
    /// <summary>
    ///   <para>Don't compress the data when creating the AssetBundle.</para>
    /// </summary>
    UncompressedAssetBundle = 1,
    /// <summary>
    ///   <para>Do not include type information within the AssetBundle.</para>
    /// </summary>
    DisableWriteTypeTree = 8,
    /// <summary>
    ///   <para>Builds an asset bundle using a hash for the id of the object stored in the asset bundle.</para>
    /// </summary>
    DeterministicAssetBundle = 16, // 0x00000010
    /// <summary>
    ///   <para>Force rebuild the assetBundles.</para>
    /// </summary>
    ForceRebuildAssetBundle = 32, // 0x00000020
    /// <summary>
    ///   <para>Ignore the type tree changes when doing the incremental build check.</para>
    /// </summary>
    IgnoreTypeTreeChanges = 64, // 0x00000040
    /// <summary>
    ///   <para>Append the hash to the assetBundle name.</para>
    /// </summary>
    AppendHashToAssetBundleName = 128, // 0x00000080
    /// <summary>
    ///   <para>Use chunk-based LZ4 compression when creating the AssetBundle.</para>
    /// </summary>
    ChunkBasedCompression = 256, // 0x00000100
    /// <summary>
    ///   <para>Do not allow the build to succeed if any errors are reporting during it.</para>
    /// </summary>
    StrictMode = 512, // 0x00000200
    /// <summary>
    ///   <para>Do a dry run build.</para>
    /// </summary>
    DryRunBuild = 1024, // 0x00000400
    /// <summary>
    ///   <para>Disables Asset Bundle LoadAsset by file name.</para>
    /// </summary>
    DisableLoadAssetByFileName = 4096, // 0x00001000
    /// <summary>
    ///   <para>Disables Asset Bundle LoadAsset by file name with extension.</para>
    /// </summary>
    DisableLoadAssetByFileNameWithExtension = 8192, // 0x00002000
    /// <summary>
    ///   <para>Removes the Unity Version number in the Archive File &amp; Serialized File headers during the build.</para>
    /// </summary>
    AssetBundleStripUnityVersion = 32768, // 0x00008000
  }
}