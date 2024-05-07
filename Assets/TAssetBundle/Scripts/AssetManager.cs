using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TAssetBundle
{
    /// <summary>
    /// TAssetBundle Integrated Asset Manager
    /// </summary>
    public static partial class AssetManager
    {
        /// <summary>
        /// Get the settings
        /// </summary>
        public static Settings Settings => _settings;

        /// <summary>
        /// Initialize 
        /// </summary>
        /// <returns></returns>
        public static TAsyncOperation InitializeAsync()
        {
            return _assetProvider.InitializeAsync();
        }

        /// <summary>
        /// load the catalog with that name
        /// </summary>
        /// <param name="catalogName">catalog name</param>
        /// <returns>asset catalog info</returns>
        public static TAsyncOperation<IAssetCatalogInfo> LoadCatalogInfoAsync(string catalogName)
        {
            return _assetProvider.LoadCatalogInfoAsync(catalogName);
        }

        /// <summary>
        /// set remote urls
        /// </summary>
        /// <param name="remoteUrl">remote urls</param>
        [Obsolete("AssetManager.SetRemoteUrls is deprecated. Use AssetManager.SetRemoteUrl instead.")]
        public static void SetRemoteUrls(string[] remoteUrls)
        {
            SetRemoteUrl(remoteUrls != null && remoteUrls.Length > 0 ? remoteUrls[0] : string.Empty);
        }

        /// <summary>
        /// set remote url
        /// [BuildTarget] in url will be changed to UnityEditor.BuildTarget
        /// [AppVersion] in url will be changed to Application.version
        /// </summary>
        /// <param name="remoteUrl">remote url</param>
        public static void SetRemoteUrl(string remoteUrl)
        {
            _assetProvider.SetRemoteUrl(remoteUrl);
        }

        /// <summary>
        /// Callback before sending web request
        /// </summary>
        /// <param name="callback">web request callback</param>
        public static void SetWebRequestBeforeSendCallback(WebRequestCallback callback)
        {
            _assetProvider.SetWebRequestBeforeSendCallback(callback);
        }

        /// <summary>
        /// Result callback for web request
        /// </summary>
        /// <param name="callback">WebRequestCallback</param>
        public static void SetWebRequestResultCallback(WebRequestCallback callback)
        {
            _assetProvider.SetWebRequestResultCallback(callback);
        }


        /// <summary>
        /// Last web request error information
        /// </summary>
        /// <returns>UnityWebRequestError</returns>
        public static UnityWebRequestError GetLastWebRequestError()
        {
            return _assetProvider.GetLastWebRequestError();
        }


        /// <summary>
        /// Check if the default catalog needs to be updated
        /// </summary>
        /// <returns>true if update is needed false otherwise</returns>
        public static TAsyncOperation<bool> CheckCatalogUpdateAsync()
        {
            return CheckCatalogUpdateAsync(_settings.catalogName);
        }

        /// <summary>
        /// Check if a catalog with that name needs to be updated
        /// </summary>
        /// <param name="catalogName">catalog name</param>
        /// <returns>true if update is needed false otherwise</returns>
        public static TAsyncOperation<bool> CheckCatalogUpdateAsync(string catalogName)
        {
            return _assetProvider.CheckCatalogUpdateAsync(catalogName);
        }


        /// <summary>
        /// Update the default catalog
        /// </summary>
        /// <returns>true if the catalog is updated false otherwise</returns>
        public static TAsyncOperation<bool> UpdateCatalogAsync()
        {
            return UpdateCatalogAsync(_settings.catalogName);
        }


        /// <summary>
        /// Update the catalog with that name
        /// </summary>
        /// <param name="catalogName">catalog name</param>
        /// <returns>true if the catalog is updated false otherwise</returns>
        public static TAsyncOperation<bool> UpdateCatalogAsync(string catalogName)
        {
            return _assetProvider.UpdateCatalogAsync(catalogName);
        }

        /// <summary>
        /// Whether the asset exists in the catalog
        /// </summary>
        /// <param name="assetPath">asset path</param>
        /// <returns>true if the asset exists, false otherwise</returns>
        public static bool ExistAsset(string assetPath)
        {
            return _assetProvider.ExistAsset(assetPath);
        }

        /// <summary>
        /// Load asset by asset path
        /// </summary>
        /// <typeparam name="T">asset type</typeparam>
        /// <param name="assetPath">asset path</param>
        /// <returns>A handle to that asset</returns>
        public static TAsyncOperation<IAssetHandle<T>> LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new ArgumentException("assetPath empty");
            }

            return _assetProvider.LoadAssetAsync<T>(assetPath);
        }


        /// <summary>
        /// Decreases the reference count of the asset in the path.
        /// When an AssetBundle containing an Asset is no longer referenced, it is automatically released.
        /// </summary>
        /// <param name="assetPath">asset path</param>
        /// <returns>true if the asset is released false otherwise</returns>
        public static bool UnloadAsset(string assetPath)
        {
            return _assetProvider.UnloadAsset(assetPath, true);
        }


        /// <summary>
        /// Decreases the reference count of the asset handle.
        /// When an AssetBundle containing an Asset is no longer referenced, it is automatically released.
        /// </summary>
        /// <param name="assetHandle">asset handle</param>
        /// <returns>true if the asset is released false otherwise</returns>
        public static bool UnloadAsset(IAssetHandle assetHandle)
        {
            return _assetProvider.UnloadAsset(assetHandle, true);
        }

        /// <summary>
        /// Unload all assets. All AssetBundles are unloaded
        /// </summary>
        public static void UnloadAll()
        {
            _assetProvider.UnloadAll(true);
        }

        /// <summary>
        /// Check if a scene with that name exists
        /// </summary>
        /// <param name="sceneNameOrPath">scene name</param>
        /// <returns>true if exists, otherwise false</returns>
        public static bool ExistScene(string sceneNameOrPath)
        {
            return _assetProvider.ExistScene(sceneNameOrPath);
        }


        /// <summary>
        /// Load the scene with that LoadSceneInfo
        /// </summary>
        /// <param name="loadSceneInfo">LoadSceneInfo</param>
        /// <returns></returns>
        public static TAsyncOperation LoadSceneAsync(LoadSceneInfo loadSceneInfo)
        {
            if (string.IsNullOrEmpty(loadSceneInfo.sceneNameOrPath))
            {
                throw new ArgumentException("sceneNameOrPath empty");
            }

            return _assetProvider.LoadSceneAsync(loadSceneInfo);
        }

        /// <summary>
        /// Load the scene with that name
        /// </summary>
        /// <param name="sceneNameOrPath">scene name</param>
        /// <param name="loadSceneMode">load scene mode</param>
        /// <returns></returns>
        public static TAsyncOperation LoadSceneAsync(string sceneNameOrPath, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            return LoadSceneAsync(new LoadSceneInfo
            {
                sceneNameOrPath = sceneNameOrPath,
                loadSceneMode = loadSceneMode
            });
        }

        /// <summary>
        /// Gets the total size of files to be downloaded from the default catalog
        /// </summary>
        /// <returns>file size</returns>
        public static TAsyncOperation<long> GetDownloadSizeAsync()
        {
            return GetDownloadSizeAsync(_settings.catalogName);
        }

        /// <summary>
        /// Gets the total size of the files to be downloaded from the catalog of that name
        /// </summary>
        /// <param name="catalogName">catalog name</param>
        /// <returns>file size</returns>
        public static TAsyncOperation<long> GetDownloadSizeAsync(string catalogName)
        {
            return _assetProvider.GetDownloadSizeAsync(catalogName);
        }

        /// <summary>
        /// Gets the total size of the files to download among asset bundles that contain that tag.
        /// </summary>
        /// <param name="tags">tags</param>
        /// <returns>file size</returns>
        public static TAsyncOperation<long> GetDownloadSizeByTagsAsync(string[] tags)
        {
            return _assetProvider.GetDownloadSizeByTagsAsync(tags);
        }


        /// <summary>
        /// Get the total size of the files that need to be downloaded out of the asset bundles required by the asset.
        /// </summary>
        /// <param name="assetPaths">asset paths</param>
        /// <returns>file size</returns>
        public static TAsyncOperation<long> GetDownloadSizeByAssetsAsync(string[] assetPaths)
        {
            return _assetProvider.GetDownloadSizeByAssetsAsync(assetPaths);
        }

        /// <summary>
        /// Get the total size of the files that need to be downloaded out of the asset bundles required for the scene.
        /// </summary>
        /// <param name="sceneNameOrPaths">scene names</param>
        /// <returns>file size</returns>
        public static TAsyncOperation<long> GetDownloadSizeByScenesAsync(string[] sceneNameOrPaths)
        {
            return _assetProvider.GetDownloadSizeByScenesAsync(sceneNameOrPaths);
        }

        /// <summary>
        /// Download the files you need to download from the default catalog
        /// </summary>
        /// <returns>asset bundle download info</returns>
        public static TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAsync()
        {
            return DownloadAsync(_settings.catalogName);
        }

        /// <summary>
        /// Download the file you need to download from the catalog with that name.
        /// </summary>
        /// <param name="catalogName">catalog name</param>
        /// <returns>asset bundle download info</returns>
        public static TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAsync(string catalogName)
        {
            return _assetProvider.DownloadAssetBundlesAsync(catalogName);
        }

        /// <summary>
        /// Download files that need to be downloaded among asset bundles containing the tag
        /// </summary>
        /// <param name="tags">tags</param>
        /// <returns>asset bundle download info</returns>
        public static TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadByTagsAsync(string[] tags)
        {
            return _assetProvider.DownloadAssetBundlesByTagsAsync(tags);
        }

        /// <summary>
        /// Download files that need to be downloaded among the asset bundles used by the assets.
        /// </summary>
        /// <param name="assetPaths">asset paths</param>
        /// <returns>asset bundle download info</returns>
        public static TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadByAssetsAsync(string[] assetPaths)
        {
            return _assetProvider.DownloadAssetBundlesByAssetsAsync(assetPaths);
        }

        /// <summary>
        /// Download the files that need to be downloaded among the asset bundles used by the scene
        /// </summary>
        /// <param name="sceneNameOrPaths">scene names</param>
        /// <returns>asset bundle download info</returns>
        public static TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadByScenesAsync(string[] sceneNameOrPaths)
        {
            return _assetProvider.DownloadAssetBundlesByScenesAsync(sceneNameOrPaths);
        }

        /// <summary>
        /// Get the asset bundles currently in active
        /// </summary>
        /// <returns>active asset bundle collection</returns>
        public static IEnumerable<IActiveAssetBundleInfo> GetActiveAssetBundles()
        {
            return _assetProvider.GetActiveAssetBundles();
        }

        /// <summary>
        /// Get loaded asset handles
        /// </summary>
        /// <returns>asset handle collection</returns>
        public static IEnumerable<IAssetHandle> GetLoadedAssetHandles()
        {
            return _assetProvider.GetLoadedAssetHandles();
        }

        /// <summary>
        /// Is the asset for that path loaded?
        /// </summary>
        /// <param name="assetPath">asset path</param>
        /// <returns>true if the asset has been loaded false otherwise</returns>
        public static bool IsLoadedAsset(string assetPath)
        {
            return _assetProvider.IsLoadedAsset(assetPath);
        }

        /// <summary>
        /// set tag comparer
        /// </summary>
        /// <param name="tagComparer">tag comparer</param>
        public static void SetTagComparer(ITagComparer tagComparer)
        {
            _assetProvider.SetTagComparer(tagComparer);
        }
    }
}
