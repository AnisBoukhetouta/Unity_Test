using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TAssetBundle
{
    /// <summary>
    /// 에셋 번들 관리자
    /// </summary>
    internal class AssetBundleManager : IAssetBundleManager
    {
        private enum EInitializeState
        {
            NotInitialized,
            Initializing,
            Complete,
        }


        /// <summary>
        /// 에셋에 대한 에셋번들 정보
        /// </summary>
        private class AssetBundleAssetInfo : IAssetInfo
        {
            public string assetPath;
            public UnityEngine.Object asset;
            public int referenceCount;
            public ActiveAssetBundleInfo assetBundleInfo;

            public UnityEngine.Object Asset => asset;

            public int ReferenceCount => referenceCount;

            public string AssetPath => assetPath;
            public IActiveAssetBundleInfo ActiveAssetBundle => assetBundleInfo;
        }

        /// <summary>
        /// 에셋 핸들
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class AssetHandle<T> : IAssetHandle<T> where T : UnityEngine.Object
        {
            public InnerOperations innerOperations;
            private bool _loaded;
            private IAssetInfo _assetInfo;
            public IAssetInfo Info => _assetInfo;

            public bool IsLoaded => _loaded;

            public bool IsValid => _assetInfo != null && _assetInfo.Asset != null;

            public T Get()
            {
                return _assetInfo.Asset as T;
            }

            public void SetAssetInfo(IAssetInfo info)
            {
                _assetInfo = info;
                _loaded = true;
                innerOperations = null;
            }
        }


        /// <summary>
        /// 현재 활성화중인 에셋번들 정보
        /// </summary>
        private class ActiveAssetBundleInfo : IActiveAssetBundleInfo
        {
            public AssetBundleRuntimeInfo assetBundleInfo;
            public AssetBundle assetBundle;
            public int referenceCount = 1;
            public IActiveAssetBundleInfo[] dependencies;

            public AssetBundleRuntimeInfo AssetBundleInfo => assetBundleInfo;

            public int ReferenceCount => referenceCount;

            public IActiveAssetBundleInfo[] Dependencies => dependencies;
        }

        /// <summary>
        /// 로딩중인 에셋번들 정보
        /// </summary>
        private class LoadingAssetBundleInfo
        {
            public AssetCatalogInfo catalogInfo;
            public AssetBundleRuntimeInfo assetBundleInfo;
            public AssetBundle assetBundle;
            public Action<ActiveAssetBundleInfo> onComplete;
            public InnerOperations innerOperations;
            public int referenceCount = 1;

            public void Add(Action<ActiveAssetBundleInfo> onComplete)
            {
                ++referenceCount;
                this.onComplete += onComplete;
            }
        }

        private string _localPath = string.Empty;
        private string _remoteUrl = string.Empty;
        private readonly Dictionary<string, AssetCatalogInfo> _catalogs = new Dictionary<string, AssetCatalogInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, IActiveAssetBundleInfo> _activeAssetBundles = new Dictionary<string, IActiveAssetBundleInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, LoadingAssetBundleInfo> _loadingAssetBundles = new Dictionary<string, LoadingAssetBundleInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, AssetBundleRuntimeInfo> _assetBundleInfoCache = new Dictionary<string, AssetBundleRuntimeInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, IAssetHandle> _assetHandleMap = new Dictionary<string, IAssetHandle>(StringComparer.OrdinalIgnoreCase);

        private readonly WebRequest _webRequest;
        private readonly Settings _settings;
        private readonly CatalogFileHandler _catalogFileHandler;
        private readonly CryptoSerializer _cryptoSerializer;
        private readonly IAssetBundleProvider _localAssetBundleProvider;
        private readonly RemoteAssetBundleProvider _unityDefaultAssetBundleProvider;
        private readonly RemoteAssetBundleProvider _specificManagedAssetBundleProvider;
        private ITagComparer _tagComparer;

        private EInitializeState _initState = EInitializeState.NotInitialized;
        private WebRequestCallback _beforeSendCallback;
        private WebRequestCallback _resultCallback;
        private UnityWebRequestError _lastWebRequestError;

        public Settings Settings => _settings;
        public Dictionary<string, AssetCatalogInfo> Catalogs => _catalogs;
        public Dictionary<string, IActiveAssetBundleInfo> ActiveAssetBundles => _activeAssetBundles;
        public Dictionary<string, IAssetHandle> AssetHandleMap => _assetHandleMap;
        public bool EnableDebuggingLog => _settings.enableDebuggingLog;
        public UnityWebRequestError LastWebRequestError => _lastWebRequestError;
        public WebRequest WebRequest => _webRequest;
        public CryptoSerializer CryptoSerializer => _cryptoSerializer;
        public ITagComparer TagComparer => _tagComparer;
        public string LocalPath => _localPath;
        public string RemoteUrl => _remoteUrl;

        public AssetBundleManager(Settings settings)
        {
            _settings = settings;
            _webRequest = new WebRequest(new WebRequest.Option
            {
                maxConcurrentRequestCount = settings.maxConcurrentRequestCount,
                maxRetryRequestCount = settings.maxRetryRequestCount,
                retryRequestWaitDuration = settings.retryRequestWaitDuration,
                enableDebuggingLog = settings.enableDebuggingLog,
            });

            _catalogFileHandler = settings.GetCatalogFileHandler();
            _cryptoSerializer = _catalogFileHandler.CryptoSerializer;
            _webRequest.OnBeforeSend += OnBeforSendWebRequest;
            _webRequest.OnComplete += OnCompleteWebRequest;
            _webRequest.OnError += OnErrorWebRequest;
            _localAssetBundleProvider = new LocalAssetBundleProvider(this);
            _specificManagedAssetBundleProvider = new SpecificManagedAssetBundleProvider(this);
            _unityDefaultAssetBundleProvider = new UnityDefaultAssetBundleProvider(this);
            _tagComparer = new DefaultTagComparer();

            SetRemoteUrl(settings.defaultRemoteUrl);
        }

        public void SetRemoteUrl(string remoteUrl)
        {
            _remoteUrl = string.IsNullOrEmpty(remoteUrl) ? string.Empty : 
                remoteUrl.Replace("[BuildTarget]", _settings.GetBuildTarget())
                    .Replace("[AppVersion]", Application.version);
        }

        public void SetWebRequestBeforeSendCallback(WebRequestCallback callback)
        {
            _beforeSendCallback = callback;
        }

        public void SetWebRequestResultCallback(WebRequestCallback callback)
        {
            _resultCallback = callback;
        }

        public void SetTagComparer(ITagComparer tagComparer)
        {
            _tagComparer = tagComparer;
        }

        private void ClearAssetBundleInfoCache()
        {
            _assetBundleInfoCache.Clear();
        }

        #region Initialize

        public TAsyncOperation InitializeAsync()
        {
            var asyncOp = new TAsyncOperation();
            CoroutineHandler.Instance.EndOfFrame(InitializeCorouine(asyncOp.Complete));
            return asyncOp;
        }

        private IEnumerator InitializeCorouine(Action onComplete)
        {
            if (_initState == EInitializeState.Complete)
            {
                onComplete?.Invoke();
                yield break;
            }

            if (_initState == EInitializeState.Initializing)
            {
                yield return new WaitWhile(() => _initState == EInitializeState.Initializing);

                onComplete?.Invoke();
                yield break;
            }

            _initState = EInitializeState.Initializing;

            if (EnableDebuggingLog)
            {
                Logger.Log("initialize");
            }

#if UNITY_EDITOR
            _localPath = string.Format("{0}/../{1}/{2}", Application.dataPath, _settings.assetBundleOutputPath, Util.GetActiveBuildTarget());
#else
            _localPath = string.Format("{0}/{1}", Application.streamingAssetsPath, Defines.StreamingPathPrefix);
#endif
            if (_settings.buildIncludeCatalog)
            {
                var loadCatalogAsync = LoadCatalogInfoAsync(_settings.catalogName);

                yield return loadCatalogAsync;

                if (loadCatalogAsync.Result == null)
                {
                    throw new InvalidOperationException("not exist catalog. please assetbundle build first - " + _settings.catalogName);
                }
            }

            _initState = EInitializeState.Complete;
            onComplete?.Invoke();
        }

        public IEnumerator CheckInitialize()
        {
            if (_initState == EInitializeState.Complete)
            {
                yield break;
            }

            yield return InitializeCorouine(null);
        }

        #endregion

        #region NeedDownloadAssetBundle

        private IEnumerator NeedDownloadAssetBundleCoroutine(AssetBundleRuntimeInfo assetBundleInfo, ReturnValue<bool> needDownload)
        {
            var isCached = new ReturnValue<bool>();

            if (IsLocalAssetBundle(assetBundleInfo))
            {
                yield return _localAssetBundleProvider.IsCached(assetBundleInfo, isCached);

                if (isCached.Value)
                {
                    needDownload.Value = false;
                    yield break;
                }
            }

            var remoteAssetBundleProvider = GetRemoteAssetBundleProvider(assetBundleInfo);

            yield return remoteAssetBundleProvider.IsCached(assetBundleInfo, isCached);

            needDownload.Value = !isCached.Value;
        }

        #endregion

        #region GetDownloadSize        
        public TAsyncOperation<long> GetDownloadSizeAsync(string catalogName)
        {
            var asyncOp = new TAsyncOperation<long>();
            CoroutineHandler.Instance.StartCoroutine(
                GetDownloadSizeCoroutine(() => GetAssetBundlesByCatalog(catalogName), false, asyncOp.Complete));

            return asyncOp;
        }

        public TAsyncOperation<long> GetDownloadSizeByTagsAsync(string[] tags)
        {
            var asyncOp = new TAsyncOperation<long>();
            CoroutineHandler.Instance.StartCoroutine(
                GetDownloadSizeCoroutine(() => GetAssetBundlesByTags(tags), true, asyncOp.Complete));
            return asyncOp;
        }

        public TAsyncOperation<long> GetDownloadSizeByAssetsAsync(string[] assetPaths)
        {
            var asyncOp = new TAsyncOperation<long>();
            CoroutineHandler.Instance.StartCoroutine(
                GetDownloadSizeCoroutine(() => GetAssetBundlesByAssets(assetPaths), true, asyncOp.Complete));
            return asyncOp;
        }

        public TAsyncOperation<long> GetDownloadSizeByScenesAsync(string[] sceneNameOrPaths)
        {
            var asyncOp = new TAsyncOperation<long>();
            CoroutineHandler.Instance.StartCoroutine(
                GetDownloadSizeCoroutine(() => GetAssetBundlesByScenes(sceneNameOrPaths), true, asyncOp.Complete));
            return asyncOp;
        }

        private IEnumerator GetDownloadSizeCoroutine(Func<AssetBundleRuntimeInfo[]> getAssetBundles, bool includeDependencies, Action<long> onComplete)
        {
            yield return CheckInitialize();

            var assetBundles = getAssetBundles();

            if (assetBundles.Length == 0)
            {
                onComplete?.Invoke(0);
                yield break;
            }

            var resultAssetBundles = new ReturnValue<AssetBundleRuntimeInfo[]>();

            yield return GetNeedDownloadAssetBundles(assetBundles, includeDependencies, resultAssetBundles);

            var size = resultAssetBundles.Value.Sum(assetBundle => assetBundle.Size);

            if (EnableDebuggingLog)
            {
                Logger.Log("total download size - " + size);
            }

            onComplete?.Invoke(size);
        }

        #endregion

        #region GetRemoteHash
        private IEnumerator GetRemoteHash(InnerOperations innerOperations,
            string catalogName,
            ReturnValue<string> returnValue)
        {
            var url = string.Format("{0}/{1}", _remoteUrl, catalogName + Defines.HashFileExtensions);
            var requestAsync = _webRequest.GetAsync(innerOperations, url);

            yield return requestAsync;

            if (requestAsync.Request.IsSuccess())
            {
                returnValue.Value = requestAsync.Request.downloadHandler.text;
            }
        }
        #endregion

        #region DownloadAssetBundle
        public TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesAsync(string catalogName)
        {
            var asyncOp = new TAsyncOperationProgress<AssetBundleDownloadInfo>();

            CoroutineHandler.Instance.StartCoroutine(
                DownloadAssetBundlesCoroutine(asyncOp.InnerOperations, 
                () => GetAssetBundlesByCatalog(catalogName),
                    false, 
                    asyncOp.Update, 
                    asyncOp.Complete)
            );

            return asyncOp;
        }

        public TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesByTagsAsync(string[] tags)
        {
            var asyncOp = new TAsyncOperationProgress<AssetBundleDownloadInfo>();

            CoroutineHandler.Instance.StartCoroutine(
                DownloadAssetBundlesCoroutine(asyncOp.InnerOperations,
                () => GetAssetBundlesByTags(tags),
                    true,
                    asyncOp.Update,
                    asyncOp.Complete)
            );

            return asyncOp;
        }

        public TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesByAssetsAsync(string[] assetPaths)
        {
            var asyncOp = new TAsyncOperationProgress<AssetBundleDownloadInfo>();

            CoroutineHandler.Instance.StartCoroutine(
                DownloadAssetBundlesCoroutine(asyncOp.InnerOperations,
                () => GetAssetBundlesByAssets(assetPaths),
                    true,
                    asyncOp.Update,
                    asyncOp.Complete)
            );

            return asyncOp;
        }

        public TAsyncOperationProgress<AssetBundleDownloadInfo> DownloadAssetBundlesByScenesAsync(string[] sceneNameOrPaths)
        {
            var asyncOp = new TAsyncOperationProgress<AssetBundleDownloadInfo>();

            CoroutineHandler.Instance.StartCoroutine(
                DownloadAssetBundlesCoroutine(asyncOp.InnerOperations,
                () => GetAssetBundlesByScenes(sceneNameOrPaths),
                    true,
                    asyncOp.Update,
                    asyncOp.Complete)
            );

            return asyncOp;
        }

        private IEnumerator DownloadAssetBundlesCoroutine(InnerOperations innerOperations,
            Func<AssetBundleRuntimeInfo[]> getAssetBundles,
            bool includeDependencies,
            Action<AssetBundleDownloadInfo> onProgress,
            Action<AssetBundleDownloadInfo> onComplete)
        {
            yield return CheckInitialize();

            var downloadInfo = new AssetBundleDownloadInfo();

            var assetBundleInfos = getAssetBundles();

            if (assetBundleInfos.Length == 0)
            {
                onComplete?.Invoke(downloadInfo);
                yield break;
            }

            if (string.IsNullOrEmpty(_remoteUrl))
            {
                Logger.Warning("remote url empty");
                onComplete?.Invoke(downloadInfo);
                yield break;
            }

            var resultAssetBundles = new ReturnValue<AssetBundleRuntimeInfo[]>();

            yield return GetNeedDownloadAssetBundles(assetBundleInfos, includeDependencies, resultAssetBundles);

            if (resultAssetBundles.Value.Length == 0)
            {
                Logger.Log("no assetbundles to download");
                onComplete?.Invoke(downloadInfo);
                yield break;
            }

            downloadInfo.downloads.AddRange(resultAssetBundles.Value.Select(assetBundle => new AssetBundleDownloadInfo.DownloadInfo
            {
                assetBundleInfo = assetBundle
            }));

            onProgress?.Invoke(downloadInfo);

            var downloadSize = downloadInfo.TotalDownloadSize;

            if (EnableDebuggingLog)
            {
                Logger.Log("assetbundle total download size - " + downloadSize);
            }

            var downloadings = new HashSet<AssetBundleDownloadInfo.DownloadInfo>(downloadInfo.downloads);

            foreach (var downloadAssetBundle in downloadings)
            {
                var remoteAssetBundleProvider = GetRemoteAssetBundleProvider(downloadAssetBundle.assetBundleInfo);

                remoteAssetBundleProvider.RequestDownload(innerOperations,
                    downloadAssetBundle,
                    download => onProgress?.Invoke(downloadInfo),
                    download => downloadings.Remove(download));
            }

            yield return new WaitWhile(() => downloadings.Count > 0);

            onComplete?.Invoke(downloadInfo);
        }

        #endregion

        #region CheckCatalogUpdate
        public TAsyncOperation<bool> CheckCatalogUpdateAsync(string catalogName)
        {
            var asyncOp = new TAsyncOperation<bool>();
            CoroutineHandler.Instance.StartCoroutine(CheckCatalogUpdateCoroutine(asyncOp.InnerOperations, catalogName, asyncOp.Complete));
            return asyncOp;
        }

        private IEnumerator CheckCatalogUpdateCoroutine(InnerOperations innerOperations,
            string catalogName, 
            Action<bool> onNeedUpdate)
        {
            yield return CheckInitialize();

            if (string.IsNullOrEmpty(_remoteUrl))
            {
                Logger.Warning("remote url empty");
                onNeedUpdate?.Invoke(false);
                yield break;
            }

            var remoteHash = new ReturnValue<string>();
            yield return GetRemoteHash(innerOperations, catalogName, remoteHash);

            if (string.IsNullOrEmpty(remoteHash.Value))
            {
                onNeedUpdate?.Invoke(false);
                yield break;
            }

            bool needCatalogUpdate;

            if (_catalogs.TryGetValue(catalogName, out AssetCatalogInfo catalog))
            {
                needCatalogUpdate = catalog.RemoteHash != remoteHash.Value;
            }
            else
            {
                needCatalogUpdate = true;
            }

            if (EnableDebuggingLog)
            {
                Logger.Log("need catalog update - " + needCatalogUpdate);
            }

            onNeedUpdate?.Invoke(needCatalogUpdate);
        }
        #endregion

        #region UpdateCatalog
        public TAsyncOperation<bool> UpdateCatalogAsync(string catalogName)
        {
            var asyncOp = new TAsyncOperation<bool>();
            CoroutineHandler.Instance.StartCoroutine(UpdateCatalogCoroutine(asyncOp.InnerOperations, catalogName, asyncOp.Complete));
            return asyncOp;
        }

        private IEnumerator UpdateCatalogCoroutine(InnerOperations innerOperations, 
            string catalogName, 
            Action<bool> onComplete)
        {
            yield return CheckInitialize();

            var checkCatalogUpdatAsync = CheckCatalogUpdateAsync(catalogName);

            yield return checkCatalogUpdatAsync;

            if (!checkCatalogUpdatAsync.Result)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            if (EnableDebuggingLog)
            {
                Logger.Log("update catalog start - " + catalogName);
            }

            var remoteHash = new ReturnValue<string>();
            yield return GetRemoteHash(innerOperations, catalogName, remoteHash);

            var catalogHashString = remoteHash.Value;
            var remoteCatalogUrl = string.Format("{0}/{1}", _remoteUrl, catalogName + Settings.build.catalogFileExtensions);
            var catalogDownloadAsync = _webRequest.GetAsync(innerOperations, remoteCatalogUrl);

            yield return catalogDownloadAsync;

            if (catalogDownloadAsync.Request.IsSuccess())
            {
                var catalogBytes = catalogDownloadAsync.Request.downloadHandler.data;
                var dataPath = Util.GetDataPath();

                if (!Directory.Exists(dataPath))
                    Directory.CreateDirectory(dataPath);

                var catalogSavePath = Path.Combine(dataPath, catalogName);                
                File.WriteAllBytes(catalogSavePath + Settings.build.catalogFileExtensions, catalogBytes);
                File.WriteAllText(catalogSavePath + Defines.HashFileExtensions, catalogHashString);

                if (EnableDebuggingLog)
                {
                    Logger.Log("update catalog finish - " + catalogName);
                }

                UnloadCatalog(catalogName);

                yield return LoadCatalogInfoAsync(catalogName);

                onComplete?.Invoke(true);
            }
            else
            {
                Logger.Error("catalog download fail - " + catalogName);
                onComplete?.Invoke(false);
            }
        }
        #endregion

        #region LoadCatalog
        private IEnumerator LoadCatalogCoroutine(InnerOperations innerOperations,
            string catalogName,
            bool remote,
            ReturnValue<AssetCatalog> returnValue)
        {
            AssetCatalog catalog = null;
            var catalogPath = remote ? Util.GetDataPath(catalogName) : string.Format("{0}/{1}", _localPath, catalogName);
            var returnBytes = new ReturnValue<byte[]>();

            yield return ReadAllBytesLocalFile(innerOperations, catalogPath + Settings.build.catalogFileExtensions, returnBytes);

            if (returnBytes.Value != null)
            {
                catalog = _catalogFileHandler.Load(returnBytes.Value);

                if(EnableDebuggingLog)
                {
                    Logger.Log(string.Format("loaded {0} catalog - name:{1}, buildNumber:{2}, hash:{3}",
                        remote ? "remote" : "local",
                        catalogName,
                        catalog.buildNumber,
                        catalog.MD5Hash));
                }
            }

            returnValue.Value = catalog;
        }

        #endregion

        #region LoadCatalogInfo
        public TAsyncOperation<IAssetCatalogInfo> LoadCatalogInfoAsync(string catalogName)
        {
            var asyncOp = new TAsyncOperation<IAssetCatalogInfo>();
            CoroutineHandler.Instance.EndOfFrame(LoadCatalogInfoCoroutine(asyncOp.InnerOperations, catalogName, asyncOp.Complete));
            return asyncOp;
        }

        private IEnumerator LoadCatalogInfoCoroutine(InnerOperations innerOperations, string catalogName, Action<IAssetCatalogInfo> onComplete)
        {
            if (_catalogs.TryGetValue(catalogName, out AssetCatalogInfo catalogInfo))
            {
                Logger.Warning("already loaded catalog - " + catalogName);
                onComplete?.Invoke(catalogInfo);
                yield break;
            }

            if (EnableDebuggingLog)
            {
                Logger.Log("load catalog - " + catalogName);
            }

            var loadCatalog = new ReturnValue<AssetCatalog>();

            yield return LoadCatalogCoroutine(innerOperations, catalogName, true, loadCatalog);

            AssetCatalog remoteCatalog = loadCatalog.Value;
            AssetCatalog localCatalog = null;

            if (_settings.buildIncludeCatalog)
            {
                yield return LoadCatalogCoroutine(innerOperations, catalogName, false, loadCatalog);
                localCatalog = loadCatalog.Value;
            }

            if (localCatalog != null || remoteCatalog != null)
            {
                catalogInfo = new AssetCatalogInfo(this, catalogName, localCatalog, remoteCatalog);
                _catalogs.Add(catalogName, catalogInfo);

                ClearAssetBundleInfoCache();

                if (EnableDebuggingLog)
                {
                    Logger.Log(string.Format("use {0} catalog - {1}", catalogInfo.IsRemote ? "remote" : "local", catalogName));
                }
            }
            else
            {
                Logger.Warning("fail to load catalog - " + catalogName);
            }

            onComplete?.Invoke(catalogInfo);
        }

        
        #endregion

        #region UnloadCatalog
        public void UnloadCatalog(string catalogName)
        {
            if (_catalogs.Remove(catalogName))
            {
                if (EnableDebuggingLog)
                {
                    Logger.Log("unload catalog - " + catalogName);
                }

                ClearAssetBundleInfoCache();
}
            else
            {
                Logger.Warning("not loaded catalog - " + catalogName);
            }
        }
        #endregion

        #region UnloadAssetBundle
        public void UnloadAssetBundleAll(bool unloadAllLoadedObject)
        {
            if (EnableDebuggingLog)
            {
                Logger.Log("unload asset bundle all - unloadAllLoadedObject:" + unloadAllLoadedObject);
            }

            foreach (var activeAssetBundle in _activeAssetBundles.Values)
            {
                if (EnableDebuggingLog)
                {
                    Logger.Log("unload asset bundle - " + activeAssetBundle.AssetBundleInfo.AssetBundleName);
                }

                ((ActiveAssetBundleInfo)activeAssetBundle).assetBundle.Unload(unloadAllLoadedObject);
            }

            _activeAssetBundles.Clear();
            _assetHandleMap.Clear();
        }

        public void UnloadAssetBundle(string assetBundleName, bool unloadAllLoadedObject)
        {
            if (!_activeAssetBundles.TryGetValue(assetBundleName, out IActiveAssetBundleInfo info))
            {
                Logger.Warning("not loaded asset bundle - " + assetBundleName);
                return;
            }

            UnloadAssetBundle(info, unloadAllLoadedObject);
        }

        private void UnloadAssetBundle(IActiveAssetBundleInfo info, bool unloadAllLoadedObject)
        {
            if (info.ReferenceCount <= 0)
            {
                Logger.Error("already unloaded asset bundle - " + info.AssetBundleInfo.AssetBundleName);
                return;
            }

            var activeAssetBundleInfo = (ActiveAssetBundleInfo)info;

            --activeAssetBundleInfo.referenceCount;

            if (EnableDebuggingLog)
            {
                Logger.Log(string.Format("decrease assetBundle - {0}, refCount:{1}",
                    info.AssetBundleInfo.AssetBundleName, info.ReferenceCount));
            }

            if (info.ReferenceCount == 0)
            {
                if (EnableDebuggingLog)
                {
                    Logger.Log(string.Format("unload asset bundle - {0}, unloadAllLoadedObject:{1}",
                        info.AssetBundleInfo.AssetBundleName, unloadAllLoadedObject));
                }

                if (activeAssetBundleInfo.assetBundle != null)
                {
                    activeAssetBundleInfo.assetBundle.Unload(unloadAllLoadedObject);
                }

                _activeAssetBundles.Remove(info.AssetBundleInfo.AssetBundleName);

                foreach (var dependentInfo in info.AssetBundleInfo.Dependencies)
                {
                    UnloadAssetBundle(dependentInfo.AssetBundleName, unloadAllLoadedObject);
                }
            }
        }
        #endregion

        #region LoadAssetBundle
        public TAsyncOperation<IActiveAssetBundleInfo> LoadAssetBundleAsync(AssetBundleRuntimeInfo assetBundleInfo)
        {
            var asyncOp = new TAsyncOperation<IActiveAssetBundleInfo>();            
            LoadAssetBundle(asyncOp.InnerOperations, assetBundleInfo, asyncOp.Complete);
            return asyncOp;
        }

        public void LoadAssetBundle(InnerOperations innerOperations, AssetBundleRuntimeInfo assetBundleInfo, Action<IActiveAssetBundleInfo> onComplete)
        {
            if (_activeAssetBundles.TryGetValue(assetBundleInfo.AssetBundleName, out IActiveAssetBundleInfo info))
            {
                var activeAssetBundleInfo = (ActiveAssetBundleInfo)info;

                ++activeAssetBundleInfo.referenceCount;

                if (EnableDebuggingLog)
                {
                    Logger.Log(string.Format("increase assetBundle - {0}, refCount:{1}", info.AssetBundleInfo.AssetBundleName, info.ReferenceCount));
                }

                onComplete?.Invoke(info);
                return;
            }

            if (_loadingAssetBundles.TryGetValue(assetBundleInfo.AssetBundleName, out LoadingAssetBundleInfo loadingInfo))
            {
                loadingInfo.Add(onComplete);
                innerOperations.Add(loadingInfo.innerOperations);

                if (EnableDebuggingLog)
                {
                    Logger.Log(string.Format("loading increase - {0}, refCount:{1}", assetBundleInfo.AssetBundleName, loadingInfo.referenceCount));
                }

                return;
            }            

            loadingInfo = new LoadingAssetBundleInfo
            {
                catalogInfo = (AssetCatalogInfo)assetBundleInfo.Catalog,
                assetBundleInfo = assetBundleInfo,
                onComplete = onComplete, 
                innerOperations = innerOperations
            };

            innerOperations.PushCount();
            _loadingAssetBundles.Add(assetBundleInfo.AssetBundleName, loadingInfo);            
            LoadAssetBundleInternal(loadingInfo);
        }

        private async void LoadAssetBundleInternal(LoadingAssetBundleInfo loadingInfo)
        {
            var tasks = loadingInfo.assetBundleInfo.Dependencies.Select(dependentInfo =>
            {
                var asyncOp = LoadAssetBundleAsync(dependentInfo);
                loadingInfo.innerOperations.Add(asyncOp.InnerOperations);
                return asyncOp.ToTask();
            });

            await Task.WhenAll(tasks);

            CoroutineHandler.Instance.StartCoroutine(LoadAssetBundleCoroutine(loadingInfo));
        }

        private bool IsLocalAssetBundle(AssetBundleRuntimeInfo assetBundleInfo)
        {
#if UNITY_EDITOR            
            return !_settings.forceRemoteAssetBundleInEditor;
#else
            if (_settings.buildIncludeAssetBundle == EBuildIncludeAssetBundle.All)
            {
                return true;
            }
            else if(_settings.buildIncludeAssetBundle == EBuildIncludeAssetBundle.BuiltinOnly && assetBundleInfo.Builtin)
            {
                return true;
            }

            return false;
#endif
        }

        public IEnumerator ExistFile(string filePath, ReturnValue<bool> returnValue)
        {
            if (IsWebRequestFilePath(filePath))
            {
                //Head Method를 사용하면 안드로이드에서는 로컬 에셋번들에 접근이 안된다
                var request = _webRequest.GetAsync(null, filePath);

                yield return request;

                returnValue.Value = request.Request.IsSuccess();
            }
            else
            {
                returnValue.Value = File.Exists(filePath);
            }
        }

        public IEnumerator LoadAssetBundleFromFile(
            InnerOperations innerOperations,
            string filePath, 
            bool encrypt, 
            Hash128 hash, 
            ReturnValue<AssetBundle> returnValue)
        {
            if (encrypt)
            {
                var returnBytes = new ReturnValue<byte[]>();

                yield return ReadAllBytesLocalFile(innerOperations, filePath, returnBytes);

                if (returnBytes.Value == null)
                {
                    yield break;
                }

                returnBytes.Value = _cryptoSerializer.Decrypt(returnBytes.Value);

                var request = AssetBundle.LoadFromMemoryAsync(returnBytes.Value);

                innerOperations.Add(request);

                yield return request;                

                returnValue.Value = request.assetBundle;
            }
            else
            {
                if(IsWebRequestFilePath(filePath))
                {
                    var request = _webRequest.GetAssetBundleAsync(innerOperations, filePath, hash);

                    yield return request;

                    if (request.Request.IsSuccess())
                    {
                        returnValue.Value = DownloadHandlerAssetBundle.GetContent(request.Request);
                    }
                }
                else
                {
                    var request = AssetBundle.LoadFromFileAsync(filePath);
                    innerOperations.Add(request);
                    yield return request;

                    returnValue.Value = request.assetBundle;
                }
            }
        }

        private IEnumerator LoadAssetBundleCoroutine(LoadingAssetBundleInfo loadingInfo)
        {
            yield return CheckInitialize();

            var assetBundleInfo = loadingInfo.assetBundleInfo;

            var result = new ReturnValue<AssetBundle>();

            if (IsLocalAssetBundle(assetBundleInfo))
            {
                var isCached = new ReturnValue<bool>();

                yield return _localAssetBundleProvider.IsCached(assetBundleInfo, isCached);

                if(isCached.Value)
                {
                    if (EnableDebuggingLog)
                    {
                        Logger.Log("load local assetbundle - " + assetBundleInfo.AssetBundleName);
                    }

                    loadingInfo.innerOperations.PopCount();
                    yield return _localAssetBundleProvider.LoadAssetBundle(loadingInfo.innerOperations, assetBundleInfo, result);

                    if (result.Value != null)
                    {
                        loadingInfo.assetBundle = result.Value;
                        OnCompleteLoadAssetBundle(loadingInfo);
                        yield break;
                    }
                    else
                    {
                        loadingInfo.innerOperations.PushCount();
                        Logger.Warning(string.Format("fail to load local assetbundle - ({0}), try remote download", assetBundleInfo.AssetBundleName));
                    }
                }
            }

            loadingInfo.innerOperations.PopCount();

            if (string.IsNullOrEmpty(_remoteUrl))
            {
                OnCompleteLoadAssetBundle(loadingInfo);
                yield break;
            }
            
            var remoteAssetBundleProvider = GetRemoteAssetBundleProvider(assetBundleInfo);

            yield return remoteAssetBundleProvider.LoadAssetBundle(loadingInfo.innerOperations, assetBundleInfo, result);

            if (result.Value != null)
            {
                loadingInfo.assetBundle = result.Value;
            }

            OnCompleteLoadAssetBundle(loadingInfo);
        }

        private RemoteAssetBundleProvider GetRemoteAssetBundleProvider(AssetBundleRuntimeInfo info)
        {
            //암호화 에셋번들이거나 유니티 기본 에셋번들 제공자를 사용하지 않으면 따로 관리되는 에셋번들 제공자를 사용한다
            if (info.Encrypt || !_settings.useUnityRemoteAssetBundleProvider)
            {
                return _specificManagedAssetBundleProvider;
            }
            else
            {
                return _unityDefaultAssetBundleProvider;
            }
        }

        private void OnCompleteLoadAssetBundle(LoadingAssetBundleInfo loadingInfo)
        {
            _loadingAssetBundles.Remove(loadingInfo.assetBundleInfo.AssetBundleName);

            if (loadingInfo.assetBundle != null)
            {
                if (EnableDebuggingLog)
                {
                    Logger.Log("loaded asset bundle - " + loadingInfo.assetBundleInfo.AssetBundleName);
                }

                var activeAssetBundleInfo = new ActiveAssetBundleInfo
                {
                    assetBundle = loadingInfo.assetBundle,
                    assetBundleInfo = loadingInfo.assetBundleInfo,
                    referenceCount = loadingInfo.referenceCount,
                    dependencies = GetActiveAssetBundlesDependencies(loadingInfo.assetBundleInfo)
                };

                _activeAssetBundles.Add(loadingInfo.assetBundleInfo.AssetBundleName, activeAssetBundleInfo);
                loadingInfo.onComplete?.Invoke(activeAssetBundleInfo);
            }
            else
            {
                Logger.Error("fail to load asset bundle - " + loadingInfo.assetBundleInfo.AssetBundleName);
                loadingInfo.onComplete?.Invoke(null);
            }
        }
        #endregion

        #region LoadAsset
        private IEnumerator LoadAssetCoroutine<T>(InnerOperations innerOperations, string assetPath, Action<IAssetHandle<T>> onComplete) where T : UnityEngine.Object
        {
            yield return CheckInitialize();

            if (_assetHandleMap.TryGetValue(assetPath, out IAssetHandle assetHandle))
            {
                var handle = assetHandle as AssetHandle<T>;

                if (!assetHandle.IsLoaded)
                {
                    innerOperations.Add(handle.innerOperations);
                    yield return new WaitUntil(() => assetHandle.IsLoaded);
                }

                if (handle.IsValid)
                {
                    var assetInfo = assetHandle.Info as AssetBundleAssetInfo;

                    ++assetInfo.referenceCount;

                    if (EnableDebuggingLog)
                    {
                        Logger.Log(string.Format("increase asset - {0}, refCount:{1}",
                            assetInfo.assetPath, assetInfo.referenceCount));
                    }

                    onComplete?.Invoke(handle);
                    yield break;
                }
            }

            if (!TryGetAssetBundleByAssetPath(assetPath, out AssetBundleRuntimeInfo info))
            {
                Logger.Error("not exist asset - " + assetPath);
                onComplete?.Invoke(null);
                yield break;
            }

            if (EnableDebuggingLog)
            {
                Logger.Log(string.Format("load asset - assetPath:{0}, assetBundle:{1}", assetPath, info.AssetBundleName));
            }

            innerOperations.PushCount();

            var loadAssetHandle = new AssetHandle<T>();
            loadAssetHandle.innerOperations = innerOperations;
            _assetHandleMap.Add(assetPath, loadAssetHandle);

            var assetBundleLoadAsync = LoadAssetBundleAsync(info);
            innerOperations.Add(assetBundleLoadAsync.InnerOperations);

            yield return assetBundleLoadAsync;

            var activeAssetBundleInfo = (ActiveAssetBundleInfo)assetBundleLoadAsync.Result;

            if (activeAssetBundleInfo == null)
            {
                innerOperations.PopCount();
                _assetHandleMap.Remove(assetPath);
                loadAssetHandle.SetAssetInfo(null);
                onComplete?.Invoke(null);
                yield break;
            }

            var assetLoadAsync = activeAssetBundleInfo.assetBundle.LoadAssetAsync<T>(assetPath);
            innerOperations.Add(assetLoadAsync);
            innerOperations.PopCount();
            yield return assetLoadAsync;

            if (assetLoadAsync.asset != null)
            {
                if (EnableDebuggingLog)
                {
                    Logger.Log(string.Format("loaded asset - assetPath:{0}, assetBundle:{1}",
                        assetPath, info.AssetBundleName));
                }

                loadAssetHandle.SetAssetInfo(new AssetBundleAssetInfo
                {
                    assetPath = assetPath,
                    asset = assetLoadAsync.asset,
                    assetBundleInfo = activeAssetBundleInfo,
                    referenceCount = 1,
                });
            }
            else
            {
                Logger.Error(string.Format("fail to load asset - assetPath:{0}, assetBundle:{1}",
                    assetPath, info.AssetBundleName));

                _assetHandleMap.Remove(assetPath);
                loadAssetHandle.SetAssetInfo(null);
                UnloadAssetBundle(activeAssetBundleInfo, true);
            }

            onComplete?.Invoke(loadAssetHandle.IsValid ? loadAssetHandle : null);
        }

        public TAsyncOperation<IAssetHandle<T>> LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            var asyncOp = new TAsyncOperation<IAssetHandle<T>>();
            CoroutineHandler.Instance.StartCoroutine(LoadAssetCoroutine<T>(asyncOp.InnerOperations, assetPath, asyncOp.Complete));
            return asyncOp;
        }
        #endregion

        #region UnloadAsset
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
            if (!assetHandle.IsLoaded)
            {
                Logger.Error("invalid asset handle");
                return false;
            }

            var assetInfo = assetHandle.Info as AssetBundleAssetInfo;

            if (assetInfo.ReferenceCount == 0)
            {
                Logger.Warning("already unloaded asset - " + assetInfo.assetPath);
                return false;
            }

            --assetInfo.referenceCount;

            if (assetInfo.ReferenceCount > 0)
            {
                if (EnableDebuggingLog)
                {
                    Logger.Log(string.Format("decrease asset - {0}, refCount:{1}",
                        assetInfo.assetPath, assetInfo.referenceCount));
                }

                return true;
            }

            if (EnableDebuggingLog)
            {
                Logger.Log("unload asset - " + assetInfo.assetPath);
            }

            _assetHandleMap.Remove(assetInfo.assetPath);

            UnloadAssetBundle(assetInfo.assetBundleInfo, unloadAllLoadedObject);

            return true;
        }
        #endregion

        public bool ExistAsset(string assetPath)
        {
            return TryGetAssetBundleByAssetPath(assetPath, out AssetBundleRuntimeInfo info);
        }

        private bool TryGetAssetBundleByAssetPath(string assetPath,
            out AssetBundleRuntimeInfo assetBundleInfo)
        {
            if (_assetBundleInfoCache.TryGetValue(assetPath, out assetBundleInfo))
            {
                return assetBundleInfo != null;
            }

            var assetPathLower = assetPath.ToLower();
            assetBundleInfo = null;

            foreach (var catalog in _catalogs.Values)
            {
                if (catalog.TryGetAssetBundleByAssetPath(assetPathLower, out assetBundleInfo))
                {
                    break;
                }
            }

            _assetBundleInfoCache.Add(assetPath, assetBundleInfo);

            return assetBundleInfo != null;
        }

        public bool TryGetAssetBundleByScene(string sceneNameOrPath,
            out AssetBundleRuntimeInfo assetBundleInfo)
        {
            if (_assetBundleInfoCache.TryGetValue(sceneNameOrPath, out assetBundleInfo))
            {
                return assetBundleInfo != null;
            }

            assetBundleInfo = null;

            var hasExtensions = sceneNameOrPath.EndsWith(".unity");

            if(hasExtensions)
            {
                foreach(var catalogInfo in _catalogs.Values)
                {
                    if (catalogInfo.TryGetAssetBundleByAssetPath(sceneNameOrPath, out assetBundleInfo))
                    {
                        break;
                    }
                }
            }
            else
            {
                foreach (var catalogInfo in _catalogs.Values)
                {
                    if (catalogInfo.TryGetAssetBundleBySceneName(sceneNameOrPath, out assetBundleInfo))
                    {
                        break;
                    }
                }
            }

            _assetBundleInfoCache.Add(sceneNameOrPath, assetBundleInfo);
            return assetBundleInfo != null;
        }

        private void OnBeforSendWebRequest(UnityWebRequest request, WebRequestCommand command)
        {
            _beforeSendCallback?.Invoke(request, command);
        }

        private void OnCompleteWebRequest(UnityWebRequest request, WebRequestCommand command)
        {
            _resultCallback?.Invoke(request, command);
        }

        private void OnErrorWebRequest(UnityWebRequest request, WebRequestCommand command)
        {
            _lastWebRequestError = request.GetError();
            _resultCallback?.Invoke(request, command);
        }

        private IEnumerator ReadAllBytesLocalFile(InnerOperations innerOperations, string filePath, ReturnValue<byte[]> returnBytes)
        {
            byte[] bytes = null;

            if (IsWebRequestFilePath(filePath))
            {                
                var request = _webRequest.GetAsync(innerOperations, filePath, useRetry: false);

                yield return request;

                if (request.Request.IsSuccess())
                {
                    bytes = request.Request.downloadHandler.data;
                }
            }
            else
            {
                if (File.Exists(filePath))
                {
                    bytes = File.ReadAllBytes(filePath);
                }
            }

            returnBytes.Value = bytes;
        }

        private void CollectDependencies(AssetBundleRuntimeInfo assetBundleInfo, List<AssetBundleRuntimeInfo> dependencies)
        {
            dependencies.Add(assetBundleInfo);

            foreach (var dependent in assetBundleInfo.Dependencies)
            {
                CollectDependencies(dependent, dependencies);
            }
        }

        private AssetBundleRuntimeInfo[] GetAssetBundlesByCatalog(string catalogName)
        {
            if (!_catalogs.TryGetValue(catalogName, out AssetCatalogInfo catalogInfo))
            {
                Logger.Error("invalid catalog - " + catalogName);
                return new AssetBundleRuntimeInfo[0];
            }

            return catalogInfo.AssetBundleInfos.ToArray();
        }

        private AssetBundleRuntimeInfo[] GetAssetBundlesByTags(string[] tags)
        {
            var infos = new List<AssetBundleRuntimeInfo>();

            foreach (var catalog in _catalogs.Values)
            {
                infos.AddRange(catalog.GetAssetBundlesByTags(tags));
            }

            return infos.ToArray();
        }

        private AssetBundleRuntimeInfo[] GetAssetBundlesByAssets(string[] assetPaths)
        {
            var infos = new List<AssetBundleRuntimeInfo>(assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                if (TryGetAssetBundleByAssetPath(assetPath, out AssetBundleRuntimeInfo info))
                {
                    infos.Add(info);
                }
            }

            return infos.ToArray();
        }

        private AssetBundleRuntimeInfo[] GetAssetBundlesByScenes(string[] sceneNameOrPaths)
        {
            var infos = new List<AssetBundleRuntimeInfo>(sceneNameOrPaths.Length);

            foreach (var sceneNameOrPath in sceneNameOrPaths)
            {
                if (TryGetAssetBundleByScene(sceneNameOrPath, out AssetBundleRuntimeInfo info))
                {
                    infos.Add(info);
                }
            }

            return infos.ToArray();
        }

        private IEnumerator GetNeedDownloadAssetBundles(AssetBundleRuntimeInfo[] assetBundleInfos, bool includeDependencies, ReturnValue<AssetBundleRuntimeInfo[]> returnAssetBundles)
        {
            AssetBundleRuntimeInfo[] targetAssetBundles;

            if (includeDependencies)
            {
                var assetBundles = new List<AssetBundleRuntimeInfo>(assetBundleInfos.Length);

                foreach (var assetBundleInfo in assetBundleInfos)
                {
                    CollectDependencies(assetBundleInfo, assetBundles);
                }

                targetAssetBundles = assetBundles.Distinct().ToArray();
            }
            else
            {
                targetAssetBundles = assetBundleInfos;
            }

            var needDownloadAssetBundles = new List<AssetBundleRuntimeInfo>(targetAssetBundles.Length);
            var needDownload = new ReturnValue<bool>();

            foreach (var assetBundleInfo in targetAssetBundles)
            {
                yield return NeedDownloadAssetBundleCoroutine(assetBundleInfo, needDownload);

                if (needDownload.Value)
                {
                    needDownloadAssetBundles.Add(assetBundleInfo);
                }
            }

            var result = needDownloadAssetBundles.ToArray();

            if (EnableDebuggingLog)
            {
                foreach (var assetBundleInfo in result)
                {
                    Logger.Log(string.Format("need to download assetbundle - {0}, size:{1}",
                        assetBundleInfo.GetNameWithTags(),
                        assetBundleInfo.Size));
                }
            }

            returnAssetBundles.Value = result;
        }

        private IActiveAssetBundleInfo[] GetActiveAssetBundlesDependencies(AssetBundleRuntimeInfo assetBundleInfo)
        {
            return assetBundleInfo.Dependencies
                .Select(dependent => _activeAssetBundles[dependent.AssetBundleName])
                .ToArray();
        }

        private bool IsWebRequestFilePath(string filePath)
        {
            bool needWebRequest = false;

#if !UNITY_EDITOR
    #if UNITY_ANDROID
            needWebRequest = filePath.StartsWith("jar");
    #elif UNITY_WEBGL
            needWebRequest = !filePath.StartsWith(Application.persistentDataPath);
    #endif
#endif
            return needWebRequest;
        }
    }

}