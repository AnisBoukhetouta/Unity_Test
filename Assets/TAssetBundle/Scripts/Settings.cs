using System;
using UnityEngine;

namespace TAssetBundle
{
    /// <summary>
    /// TAssetBundle Settings
    /// </summary>
    public class Settings : ScriptableObject
    {
        [Serializable]
        public class BuildSetting
        {
            /// <summary>
            /// Catalog file extensions
            /// </summary>
            [Tooltip("Catalog file extensions")]
            public string catalogFileExtensions = ".json";

            /// <summary>
            /// AssetBundle file extensions
            /// </summary>
            [Tooltip("AssetBundle file extensions")]
            public string assetBundleFileExtensions = ".bundle";

            /// <summary>
            /// Compress Catalog
            /// </summary>
            [Tooltip("Compress Catalog")]
            public bool compressCatalog = false;

            /// <summary>
            /// Encrypt Catalog
            /// </summary>
            [Tooltip("Encrypt Catalog")]
            public bool encryptCatalog = false;

            /// <summary>
            /// Crypto Key
            /// </summary>
            [Tooltip("Crypto Key")]
            public string cryptoKey;

            /// <summary>
            /// Add a hash after the AssetBundle file name
            /// </summary>
            [Tooltip("Add a hash after the AssetBundle file name")]
            public bool appendHashFromFileName = false;

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public static bool operator ==(BuildSetting lhs, BuildSetting rhs)
            {
                if (lhs.catalogFileExtensions != rhs.catalogFileExtensions)
                    return false;

                if (lhs.compressCatalog != rhs.compressCatalog)
                    return false;

                if (lhs.assetBundleFileExtensions != rhs.assetBundleFileExtensions)
                    return false;

                if (lhs.encryptCatalog != rhs.encryptCatalog)
                    return false;

                if (lhs.cryptoKey != rhs.cryptoKey)
                    return false;

                if (lhs.appendHashFromFileName != rhs.appendHashFromFileName)
                    return false;

                return true;
            }

            public static bool operator !=(BuildSetting lhs, BuildSetting rhs)
            {
                return !(lhs == rhs);
            }
        }

        /// <summary>
        /// Catalog name
        /// </summary>
        [Tooltip("Catalog name")]
        public string catalogName = Defines.DefaultCatalogName;

        /// <summary>
        /// AssetBundle Output Path
        /// </summary>
        [Tooltip("AssetBundle Output Path")]
        public string assetBundleOutputPath = "AssetBundle";

        /// <summary>
        /// The build number will use the catalog with the higher build number between the local and remote catalogs.
        /// </summary>
        [Tooltip("The build number will use the catalog with the higher build number between the local and remote catalogs.")]
        public int buildNumber;

        /// <summary>
        /// Build setting information (Build cache is initialized according to options)
        /// </summary>
        [Tooltip("Build Setting (Build cache is initialized according to options)")]
        public BuildSetting build = new BuildSetting();

#if UNITY_EDITOR
        /// <summary>
        /// Editor Play Mode (Editor Only)
        /// </summary>
        [Tooltip("Editor Play Mode (Editor Only)")]
        public EEditorPlayMode editorPlayMode;

        /// <summary>
        /// Forced in editor to use only remote AssetBundles. (Editor only)
        /// </summary>
        [Tooltip("Forced in editor to use only remote AssetBundles. (Editor only)")]
        public bool forceRemoteAssetBundleInEditor = false;
#endif

        /// <summary>
        /// Maximum count of concurrent web requests
        /// </summary>
        [Tooltip("Maximum count of concurrent web requests")]
        public int maxConcurrentRequestCount = 5;

        /// <summary>
        /// Maximum count of web request retries
        /// </summary>
        [Tooltip("Maximum count of web request retries")]
        public int maxRetryRequestCount = 3;

        /// <summary>
        /// Retry request wait duration
        /// </summary>
        [Tooltip("Retry request wait duration")]
        public float retryRequestWaitDuration = 1f;

        /// <summary>
        /// Use Build Cache
        /// </summary>
        [Tooltip("Use Build Cache")]
        public bool useBuildCache = true;

        /// <summary>
        /// Enable Debugging Log
        /// </summary>
        [Tooltip("Enable Debugging Log")]
        public bool enableDebuggingLog = false;

        /// <summary>
        /// Include catalog in app
        /// </summary>
        [Tooltip("Include catalog in app")]
        public bool buildIncludeCatalog = true;

        /// <summary>
        /// Include AssetBundles in app
        /// </summary>
        [Tooltip("Include AssetBundles in app")]
        public EBuildIncludeAssetBundle buildIncludeAssetBundle = EBuildIncludeAssetBundle.BuiltinOnly;

        /// <summary>
        /// Recompress the AssetBundles built-in in your app to lz4 (very efficient, but slightly increases file size).
        /// </summary>
        [Tooltip("Recompress the AssetBundles built-in in your app to lz4 (very efficient, but slightly increases file size).")]
        public bool useBuiltinAssetBundleLZ4Recompress = true;

        /// <summary>
        /// If enabled, the UnityRemoteAssetBundleProvider is used. When disabled, always uses a SpecificManagedAssetBundleProvider. SpecificManagedAssetBundleProvider are default used for crypto asset bundles.
        /// </summary>
        [Tooltip("If enabled, the UnityRemoteAssetBundleProvider is used. When disabled, always uses a SpecificManagedAssetBundleProvider. SpecificManagedAssetBundleProvider are default used for crypto asset bundles.")]
        public bool useUnityRemoteAssetBundleProvider = true;

        /// <summary>
        /// Custom crypto serializer
        /// </summary>
        [Tooltip("Custom Crypto Serializer")]
        public CryptoSerializer customCryptoSerializer;

        /// <summary>
        /// Custom catalog serializer
        /// </summary>
        [Tooltip("Custom Catalog Serializer")]
        public CatalogSerializer customCatalogSerializer;


        /// <summary>
        /// default remote url
        /// </summary>
        [Tooltip("Default Remote Url")]
        public string defaultRemoteUrl;

        /// <summary>
        /// build target string
        /// </summary>
        [Tooltip("Build Target String")]
        [HideInInspector]
        public string buildTarget;


        /// <summary>
        /// Generate New Crypto Key
        /// </summary>
        [ContextMenu("Generate New Crypto Key")]
        public void GenerateNewCryptoKey()
        {
            build.cryptoKey = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Get Crypto Serializer 
        /// </summary>
        /// <returns>Crypto Serializer</returns>
        public CryptoSerializer GetCryptoSerializer()
        {
            CryptoSerializer cryptoSerializer = customCryptoSerializer != null ? customCryptoSerializer : CreateInstance<RijndaelCryptoSerializer>();

            cryptoSerializer.Init(build.cryptoKey);

            return cryptoSerializer;
        }


        /// <summary>
        /// Get Catalog Serializer
        /// </summary>
        /// <returns>Catalog Serializer</returns>
        public CatalogSerializer GetCatalogSerializer()
        {
            return customCatalogSerializer != null ? customCatalogSerializer : CreateInstance<JsonCatalogSerializer>();
        }

        /// <summary>
        /// Get Catalog File Handler
        /// </summary>
        /// <returns></returns>
        public CatalogFileHandler GetCatalogFileHandler()
        {
            return new CatalogFileHandler(GetCatalogSerializer(),
                GetCryptoSerializer(),
                build.catalogFileExtensions,
                build.compressCatalog,
                build.encryptCatalog);
        }

        /// <summary>
        /// Get the BuildTarget as a string from the currently running platform
        /// </summary>
        /// <returns>buildTarget string</returns>
        public string GetBuildTarget()
        {
#if UNITY_EDITOR
            return Util.GetActiveBuildTarget().ToString();
#else
            return buildTarget;
#endif
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(build.cryptoKey))
            {
                GenerateNewCryptoKey();
            }
        }
    }
}