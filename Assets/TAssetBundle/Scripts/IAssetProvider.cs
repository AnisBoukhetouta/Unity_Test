using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace TAssetBundle
{

    internal interface IAssetProvider
    {
        TAsyncOperation InitializeAsync();
        TAsyncOperation<IAssetCatalogInfo> LoadCatalogInfoAsync(string catalogName);
        void SetRemoteUrl(string remoteUrl);
        TAsyncOperation<bool> CheckCatalogUpdateAsync(string catalogName);
        TAsyncOperation<bool> UpdateCatalogAsync(string catalogName);
        TAsyncOperation<long> GetDownloadSizeAsync(string catalogName);
        TAsyncOperation<long> GetDownloadSizeByTagsAsync(string[] tags);
        TAsyncOperation<long> GetDownloadSizeByAssetsAsync(string[] assetPaths);
        TAsyncOperation<long> GetDownloadSizeByScenesAsync(string[] sceneNameOrPaths);
        bool ExistAsset(string assetPath);
        TAsyncOperation<IAssetHandle<T>> LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object;
        bool UnloadAsset(string assetPath, bool unloadAllLoadedObject);
        bool UnloadAsset(IAssetHandle assetHandle, bool unloadAllLoadedObject);
        void UnloadAll(bool unloadAllLoadedObject);
        bool ExistScene(string sceneNameOrPath);
        TAsyncOperation LoadSceneAsync(LoadSceneInfo loadSceneInfo);
        TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesAsync(string catalogName);
        TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesByTagsAsync(string[] tags);
        TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesByAssetsAsync(string[] assetPaths);
        TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesByScenesAsync(string[] sceneNameOrPaths);
        IEnumerable<IActiveAssetBundleInfo> GetActiveAssetBundles();
        IEnumerable<IAssetHandle> GetLoadedAssetHandles();
        bool IsLoadedAsset(string assetPath);
        UnityWebRequestError GetLastWebRequestError();
        void SetTagComparer(ITagComparer tagComparer);


        void SetWebRequestBeforeSendCallback(WebRequestCallback callback);
        void SetWebRequestResultCallback(WebRequestCallback callback);
    }
}