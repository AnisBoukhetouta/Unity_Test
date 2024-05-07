using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TAssetBundle
{
    internal class EditorAssetProvider : IAssetProvider
    {
        private class EditorAssetInfo : IAssetInfo
        {
            public string assetPath;
            public UnityEngine.Object asset;
            public int referenceCount = 1;

            public UnityEngine.Object Asset => asset;

            public int ReferenceCount => referenceCount;

            public string AssetPath => assetPath;

            public IActiveAssetBundleInfo ActiveAssetBundle => null;
        }

        private class EditorAssetHandle<T> : IAssetHandle<T> where T : UnityEngine.Object
        {
            public EditorAssetInfo info;

            public bool IsLoaded => info != null;

            IAssetInfo IAssetHandle.Info => info;

            public T Get()
            {
                return info.asset as T;
            }
        }

        private readonly Settings _settings;
        private readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();
        private readonly Dictionary<string, IAssetHandle> _assetHandleMap = new Dictionary<string, IAssetHandle>(StringComparer.OrdinalIgnoreCase);
        private readonly IActiveAssetBundleInfo[] _emptyAssetBundles = new IActiveAssetBundleInfo[0];
        public bool EnableDebuggingLog => _settings.enableDebuggingLog;


        public EditorAssetProvider(Settings settings)
        {
            _settings = settings;
        }

        public TAsyncOperation InitializeAsync()
        {
            var asyncOp = new TAsyncOperation();
            CoroutineHandler.Instance.StartCoroutine(Initialize(asyncOp.Complete));
            return asyncOp;
        }

        private IEnumerator Initialize(Action onComplete)
        {
            yield return _waitForEndOfFrame;
            onComplete();
        }

        public TAsyncOperation<IAssetCatalogInfo> LoadCatalogInfoAsync(string catalogName)
        {
            var asyncOp = new TAsyncOperation<IAssetCatalogInfo>();
            CoroutineHandler.Instance.StartCoroutine(LoadCatalogInfo(asyncOp.Complete));
            return asyncOp;
        }

        private IEnumerator LoadCatalogInfo(Action<IAssetCatalogInfo> onComplete)
        {
            yield return _waitForEndOfFrame;
            onComplete(null);
        }

        public void SetRemoteUrl(string remoteUrl)
        {
        }

        public TAsyncOperation<IAssetHandle<T>> LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            var asyncOp = new TAsyncOperation<IAssetHandle<T>>();
            CoroutineHandler.Instance.StartCoroutine(LoadAssetCoroutine<T>(assetPath, asyncOp.Complete));
            return asyncOp;
        }

        private IEnumerator LoadAssetCoroutine<T>(string assetPath, Action<IAssetHandle<T>> onComplete) where T : UnityEngine.Object
        {
            yield return _waitForEndOfFrame;

            if (!_assetHandleMap.TryGetValue(assetPath, out IAssetHandle assetHandle))
            {                
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

                if (asset != null)
                {
                    var handle = new EditorAssetHandle<T>
                    {
                        info = new EditorAssetInfo
                        {
                            assetPath = assetPath,
                            asset = asset,
                            referenceCount = 1,
                        }
                    };

                    Logger.Log("loaded asset - " + assetPath);
                    _assetHandleMap.Add(assetPath, handle);
                    onComplete(handle);
                }
                else
                {
                    Logger.Error("not exist asset - " + assetPath);
                    onComplete(null);
                }
            }
            else
            {
                var handle = assetHandle as EditorAssetHandle<T>;
                ++handle.info.referenceCount;

                if (EnableDebuggingLog)
                {
                    Logger.Log(string.Format("increase asset - {0}, refCount:{1}",
                        assetPath, assetHandle.Info.ReferenceCount));
                }

                onComplete(handle);
            }
        }


        public bool ExistAsset(string assetPath)
        {
            return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath));
        }

        public bool UnloadAsset(string assetPath, bool unloadAllLoadedObject)
        {
            if (!_assetHandleMap.TryGetValue(assetPath, out IAssetHandle assetHandle))
            {
                Logger.Warning("not loaded asset - " + assetPath);
                return false;
            }

            return UnloadAsset(assetHandle, unloadAllLoadedObject);
        }

        public bool UnloadAsset(IAssetHandle assetHandle, bool unloadAllLoadedObject)
        {
            if (assetHandle.Info == null)
            {
                Logger.Error("invalid asset handle");
                return false;
            }

            var assetInfo = (EditorAssetInfo)assetHandle.Info;

            if (assetHandle.Info.ReferenceCount == 0)
            {
                Logger.Warning("already unloaded asset - " + assetInfo.assetPath);
                return false;
            }

            var info = (EditorAssetInfo)assetHandle.Info;

            --info.referenceCount;

            if (info.referenceCount > 0)
            {
                if (EnableDebuggingLog)
                {
                    Logger.Log(string.Format("decrease asset - {0}, refCount:{1}",
                        info.assetPath, info.referenceCount));
                }

                return true;
            }

            if (EnableDebuggingLog)
            {
                Logger.Log("unload asset - " + assetHandle.Info.AssetPath);
            }

            return _assetHandleMap.Remove(assetHandle.Info.AssetPath);
        }

        public TAsyncOperation<bool> CheckCatalogUpdateAsync(string catalogName)
        {
            var asyncOp = new TAsyncOperation<bool>();
            CoroutineHandler.Instance.StartCoroutine(CheckCatalogUpdate(asyncOp.Complete));
            return asyncOp;
        }

        private IEnumerator CheckCatalogUpdate(Action<bool> onComplete)
        {
            yield return _waitForEndOfFrame;
            onComplete(false);
        }

        public TAsyncOperation<bool> UpdateCatalogAsync(string catalogName)
        {
            var asyncOp = new TAsyncOperation<bool>();
            CoroutineHandler.Instance.StartCoroutine(UpdateCatalog(asyncOp.Complete));
            return asyncOp;
        }

        private IEnumerator UpdateCatalog(Action<bool> onComplete)
        {
            yield return _waitForEndOfFrame;
            onComplete(false);
        }

        public void UnloadAll(bool unloadAllLoadedObject)
        {
            if (EnableDebuggingLog)
            {
                Logger.Log("unload all");
            }

            _assetHandleMap.Clear();
        }

        public TAsyncOperation LoadSceneAsync(LoadSceneInfo loadSceneInfo)
        {
            var asyncOp = new TAsyncOperation();
            CoroutineHandler.Instance.StartCoroutine(LoadScene(asyncOp.InnerOperations, loadSceneInfo, asyncOp.Complete));
            return asyncOp;
        }

        private IEnumerator LoadScene(InnerOperations innerOperations, LoadSceneInfo loadSceneInfo, Action onComplete)
        {
            var request = SceneManager.LoadSceneAsync(loadSceneInfo.sceneNameOrPath, loadSceneInfo.loadSceneMode);
            innerOperations.Add(request);

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
                        
            onComplete();
        }

        public TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesAsync(string catalogName)
        {
            var asyncOp = new TAsyncOperationProgress<AssetBundleDownloadInfo>();
            CoroutineHandler.Instance.StartCoroutine(DownloadAssets(asyncOp.Complete));
            return asyncOp;
        }

        public TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesByTagsAsync(string[] tags)
        {
            var asyncOp = new TAsyncOperationProgress<AssetBundleDownloadInfo>();
            CoroutineHandler.Instance.StartCoroutine(DownloadAssets(asyncOp.Complete));
            return asyncOp;
        }

        public TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesByAssetsAsync(string[] assetPaths)
        {
            var asyncOp = new TAsyncOperationProgress<AssetBundleDownloadInfo>();
            CoroutineHandler.Instance.StartCoroutine(DownloadAssets(asyncOp.Complete));
            return asyncOp;
        }

        public TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesByScenesAsync(string[] sceneNameOrPaths)
        {
            var asyncOp = new TAsyncOperationProgress<AssetBundleDownloadInfo>();
            CoroutineHandler.Instance.StartCoroutine(DownloadAssets(asyncOp.Complete));
            return asyncOp;
        }

        private IEnumerator DownloadAssets(Action<AssetBundleDownloadInfo> onComplete)
        {
            yield return _waitForEndOfFrame;
            onComplete(new AssetBundleDownloadInfo());
        }

        public IEnumerable<IActiveAssetBundleInfo> GetActiveAssetBundles()
        {
            return _emptyAssetBundles;
        }

        public bool ExistScene(string sceneNameOrPath)
        {
            return true;
        }

        public TAsyncOperation<long> GetDownloadSizeAsync(string catalogName)
        {
            var asyncOp = new TAsyncOperation<long>();
            CoroutineHandler.Instance.StartCoroutine(GetDownloadSize(asyncOp.Complete));
            return asyncOp;
        }
        public TAsyncOperation<long> GetDownloadSizeByTagsAsync(string[] tags)
        {
            var asyncOp = new TAsyncOperation<long>();
            CoroutineHandler.Instance.StartCoroutine(GetDownloadSize(asyncOp.Complete));
            return asyncOp;
        }
        public TAsyncOperation<long> GetDownloadSizeByAssetsAsync(string[] assetPaths)
        {
            var asyncOp = new TAsyncOperation<long>();
            CoroutineHandler.Instance.StartCoroutine(GetDownloadSize(asyncOp.Complete));
            return asyncOp;
        }

        public TAsyncOperation<long> GetDownloadSizeByScenesAsync(string[] sceneNames)
        {
            var asyncOp = new TAsyncOperation<long>();
            CoroutineHandler.Instance.StartCoroutine(GetDownloadSize(asyncOp.Complete));
            return asyncOp;
        }

        private IEnumerator GetDownloadSize(Action<long> onComplete)
        {
            yield return _waitForEndOfFrame;
            onComplete(0);
        }

        public void SetWebRequestBeforeSendCallback(WebRequestCallback handler)
        {
        }

        public void SetWebRequestResultCallback(WebRequestCallback handler)
        {
        }

        public IEnumerable<IAssetHandle> GetLoadedAssetHandles()
        {
            return _assetHandleMap.Values;
        }

        public UnityWebRequestError GetLastWebRequestError()
        {
            return new UnityWebRequestError();
        }

        public bool IsLoadedAsset(string assetPath)
        {
            return _assetHandleMap.ContainsKey(assetPath);
        }

        public void SetTagComparer(ITagComparer tagComparer)
        {
        }
    }
}

#endif