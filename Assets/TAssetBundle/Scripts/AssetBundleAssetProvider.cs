using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TAssetBundle
{
    internal class AssetBundleAssetProvider : IAssetProvider
    {
        private readonly AssetBundleManager _manager;

        public AssetBundleAssetProvider(Settings settings)
        {
            _manager = new AssetBundleManager(settings);
        }

        public TAsyncOperation InitializeAsync()
        {
            return _manager.InitializeAsync();
        }

        public TAsyncOperation<IAssetHandle<T>> LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            return _manager.LoadAssetAsync<T>(assetPath);
        }

        public TAsyncOperation<IAssetCatalogInfo> LoadCatalogInfoAsync(string catalogName)
        {
            return _manager.LoadCatalogInfoAsync(catalogName);
        }

        public void SetRemoteUrl(string remoteUrl)
        {
            _manager.SetRemoteUrl(remoteUrl);
        }

        public bool ExistAsset(string assetPath)
        {
            return _manager.ExistAsset(assetPath);
        }

        public bool UnloadAsset(string assetPath, bool unloadAllLoadedObject)
        {
            return _manager.UnloadAsset(assetPath, unloadAllLoadedObject);
        }

        public bool UnloadAsset(IAssetHandle assetHandle, bool unloadAllLoadedObject)
        {
            return _manager.UnloadAsset(assetHandle, unloadAllLoadedObject);
        }

        public TAsyncOperation<bool> CheckCatalogUpdateAsync(string catalogName)
        {
            return _manager.CheckCatalogUpdateAsync(catalogName);
        }

        public TAsyncOperation<bool> UpdateCatalogAsync(string catalogName)
        {
            return _manager.UpdateCatalogAsync(catalogName);
        }

        public void UnloadAll(bool unloadAllLoadedObject)
        {
            _manager.UnloadAssetBundleAll(unloadAllLoadedObject);
        }

        public TAsyncOperation LoadSceneAsync(LoadSceneInfo loadSceneInfo)
        {
            var asyncOp = new TAsyncOperation();
            
            CoroutineHandler.Instance.StartCoroutine(
                LoadSceneCoroutine(asyncOp.InnerOperations, loadSceneInfo, asyncOp.Complete));

            return asyncOp;
        }

        private IEnumerator LoadSceneCoroutine(InnerOperations innerOperations, LoadSceneInfo loadSceneInfo, Action onComplete)
        {
            innerOperations.PushCount();

            yield return _manager.CheckInitialize();

            if (_manager.TryGetAssetBundleByScene(loadSceneInfo.sceneNameOrPath, out AssetBundleRuntimeInfo assetBundleInfo))
            {
                var assetBundleLoadAsync = _manager.LoadAssetBundleAsync(assetBundleInfo);
                innerOperations.Add(assetBundleLoadAsync.InnerOperations);

                yield return assetBundleLoadAsync;

                if (assetBundleLoadAsync.Result == null)
                {
                    innerOperations.PopCount();
                    Logger.Error("invalid scene asset bundle - " + assetBundleInfo.AssetBundleName);
                    onComplete();
                    yield break;
                }
            }
            
            var request = SceneManager.LoadSceneAsync(loadSceneInfo.sceneNameOrPath, loadSceneInfo.loadSceneMode);
            innerOperations.Add(request);
            innerOperations.PopCount();

            if(loadSceneInfo.allowSceneActivation != null)
            {
                while(!request.isDone)
                {
                    request.allowSceneActivation = loadSceneInfo.allowSceneActivation(request.progress);
                    yield return null;
                }
            }
            else
            {
                yield return request;
            }            

            if (assetBundleInfo != null)
            {
                var assetBundleUnloader = new GameObject("SceneAssetBundleUnloader").AddComponent<SceneAssetBundleUnloader>();
                assetBundleUnloader.Setting(_manager, assetBundleInfo.AssetBundleName);

                if (loadSceneInfo.loadSceneMode == LoadSceneMode.Additive)
                {
                    var loadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
                    SceneManager.MoveGameObjectToScene(assetBundleUnloader.gameObject, loadedScene);   
                }
            }

            onComplete();
        }

        public TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesAsync(string catalogName)
        {
            return _manager.DownloadAssetBundlesAsync(catalogName);
        }

        public TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesByTagsAsync(string[] tags)
        {
            return _manager.DownloadAssetBundlesByTagsAsync(tags);
        }
        public TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesByAssetsAsync(string[] assetPaths)
        {
            return _manager.DownloadAssetBundlesByAssetsAsync(assetPaths);
        }
        public TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesByScenesAsync(string[] sceneNameOrPaths)
        {
            return _manager.DownloadAssetBundlesByScenesAsync(sceneNameOrPaths);
        }

        public IEnumerable<IActiveAssetBundleInfo> GetActiveAssetBundles()
        {
            return _manager.ActiveAssetBundles.Values;
        }

        public bool ExistScene(string sceneNameOrPath)
        {
            return _manager.TryGetAssetBundleByScene(sceneNameOrPath, out AssetBundleRuntimeInfo info);
        }

        public TAsyncOperation<long> GetDownloadSizeAsync(string catalogName)
        {
            return _manager.GetDownloadSizeAsync(catalogName);
        }

        public TAsyncOperation<long> GetDownloadSizeByTagsAsync(string[] tags)
        {
            return _manager.GetDownloadSizeByTagsAsync(tags);
        }

        public TAsyncOperation<long> GetDownloadSizeByAssetsAsync(string[] assetPaths)
        {
            return _manager.GetDownloadSizeByAssetsAsync(assetPaths);
        }

        public TAsyncOperation<long> GetDownloadSizeByScenesAsync(string[] sceneNames)
        {
            return _manager.GetDownloadSizeByScenesAsync(sceneNames);
        }

        public void SetWebRequestBeforeSendCallback(WebRequestCallback handler)
        {
            _manager.SetWebRequestBeforeSendCallback(handler);
        }

        public void SetWebRequestResultCallback(WebRequestCallback handler)
        {
            _manager.SetWebRequestResultCallback(handler);
        }

        public IEnumerable<IAssetHandle> GetLoadedAssetHandles()
        {
            return _manager.AssetHandleMap.Values.Where(assetHandle => assetHandle.IsLoaded);
        }

        public UnityWebRequestError GetLastWebRequestError()
        {
            return _manager.LastWebRequestError;
        }

        public bool IsLoadedAsset(string assetPath)
        {
            return _manager.AssetHandleMap.TryGetValue(assetPath, out IAssetHandle handle) && handle.IsLoaded;
        }

        public void SetTagComparer(ITagComparer tagComparer)
        {
            _manager.SetTagComparer(tagComparer);
        }
    }

}