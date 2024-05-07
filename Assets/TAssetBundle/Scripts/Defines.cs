using UnityEngine;

namespace TAssetBundle
{
    /// <summary>
    /// Playmode on editor
    /// </summary>
    public enum EEditorPlayMode
    {
        /// <summary>
        /// Load assets Directly
        /// </summary>
        [Tooltip("Load assets Directly")]
        EditorAsset,

        /// <summary>
        /// Load assets from AssetBundle
        /// </summary>
        [Tooltip("Load assets from AssetBundle")]
        AssetBundle,
    }


    /// <summary>
    /// How to include AssetBundle when building an app
    /// </summary>
    public enum EBuildIncludeAssetBundle
    {
        /// <summary>
        /// Not include AssetBundles in the app
        /// </summary>
        [Tooltip("Not include AssetBundles in the app")]
        None,

        /// <summary>
        /// Only built-in AssetBundles included in the app
        /// </summary>
        [Tooltip("Only built-in AssetBundles included in the app")]
        BuiltinOnly,

        /// <summary>
        /// Include all AssetBundles in the app
        /// </summary>
        [Tooltip("Include all AssetBundles in the app")]
        All
    }

    public static class Defines
    {
        public const string Version = "3.9.2";
        public const int CatalogVersion = 4;
        public readonly static Hash128 DefaultHash = default;
        public const string DefaultCatalogName = "catalog";
        public const string DataPathPrefix = "TAssetBundle";
        public const string StreamingPathPrefix = "TA";
        public const string SettingFileName = "TAssetBundleSettings";        
        public const string HashFileExtensions = ".hash";
        public readonly static string SettingFilePath = string.Format("Assets/Resources/{0}.asset", SettingFileName);
    }

}