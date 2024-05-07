using System.Linq;
using UnityEngine.SceneManagement;

namespace TAssetBundle
{
    public static partial class AssetManager
    {
        /// <summary>
        /// Whether the asset exists in the catalog
        /// </summary>
        /// <param name="assetRef">asset reference</param>
        /// <returns>true if the asset exists, false otherwise</returns>
        public static bool ExistAsset(AssetRef assetRef)
        {
            return ExistAsset(assetRef.Path);
        }

        /// <summary>
        /// Load asset by asset reference
        /// </summary>
        /// <typeparam name="T">asset type</typeparam>
        /// <param name="assetRef">asset reference</param>
        /// <returns>A handle to that asset</returns>
        public static TAsyncOperation<IAssetHandle<T>> LoadAssetAsync<T>(AssetRef assetRef) where T : UnityEngine.Object
        {
            return LoadAssetAsync<T>(assetRef.Path);
        }

        /// <summary>
        /// Decreases the reference count of the asset reference.
        /// When an AssetBundle containing an Asset is no longer referenced, it is automatically released.
        /// </summary>
        /// <param name="assetRef">asset reference</param>
        /// <returns>true if the asset is released false otherwise</returns>
        public static bool UnloadAsset(AssetRef assetRef)
        {
            return UnloadAsset(assetRef.Path);
        }

        /// <summary>
        /// Load the scene with the scene asset reference
        /// </summary>
        /// <param name="sceneAssetRef">scene asset reference</param>
        /// <param name="loadSceneMode">load scene mode</param>
        /// <returns></returns>
        public static TAsyncOperation LoadSceneAsync(SceneAssetRef sceneAssetRef, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            return LoadSceneAsync(new LoadSceneInfo
            {
                sceneNameOrPath = sceneAssetRef.Path,
                loadSceneMode = loadSceneMode
            });
        }

        /// <summary>
        /// Get the total size of the files that need to be downloaded out of the asset bundles required by the asset.
        /// </summary>
        /// <param name="assetRefs">asset references</param>
        /// <returns>file size</returns>
        public static TAsyncOperation<long> GetDownloadSizeByAssetsAsync(AssetRef[] assetRefs)
        {
            return _assetProvider.GetDownloadSizeByAssetsAsync(assetRefs.Select(assetRef => assetRef.Path).ToArray());
        }

        /// <summary>
        /// Get the total size of the files that need to be downloaded out of the asset bundles required for the scene.
        /// </summary>
        /// <param name="sceneAssetRefs">scene asset references</param>
        /// <returns>file size</returns>
        public static TAsyncOperation<long> GetDownloadSizeByScenesAsync(SceneAssetRef[] sceneAssetRefs)
        {
            return _assetProvider.GetDownloadSizeByScenesAsync(sceneAssetRefs.Select(assetRef => assetRef.FileName).ToArray());
        }

        /// <summary>
        /// Download files that need to be downloaded among the asset bundles used by the assets.
        /// </summary>
        /// <param name="assetRefs">asset references</param>
        /// <returns>asset bundle download info</returns>
        public static TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadByAssetsAsync(AssetRef[] assetRefs)
        {
            return _assetProvider.DownloadAssetBundlesByAssetsAsync(assetRefs.Select(assetRef => assetRef.Path).ToArray());
        }

        /// <summary>
        /// Download the files that need to be downloaded among the asset bundles used by the scene
        /// </summary>
        /// <param name="sceneAssetRefs">scene asset references</param>
        /// <returns>asset bundle download info</returns>
        public static TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadByScenesAsync(SceneAssetRef[] sceneAssetRefs)
        {
            return _assetProvider.DownloadAssetBundlesByScenesAsync(sceneAssetRefs.Select(assetRef => assetRef.Path).ToArray());
        }

        /// <summary>
        /// Is the asset for that assetRef loaded?
        /// </summary>
        /// <param name="assetRef">asset reference</param>
        /// <returns>true if the asset has been loaded false otherwise</returns>
        public static bool IsLoadedAsset(AssetRef assetRef)
        {
            return IsLoadedAsset(assetRef.Path);
        }
    }

}

