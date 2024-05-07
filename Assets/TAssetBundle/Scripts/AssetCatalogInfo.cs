using System;
using System.Collections.Generic;
using System.Linq;

namespace TAssetBundle
{
    /// <summary>
    /// Asset Catalog Runtime Information
    /// </summary>
    internal class AssetCatalogInfo : IAssetCatalogInfo
    {
        private readonly IAssetBundleManager _owner;
        private readonly string _name;
        private readonly string _remoteHash;
        private readonly string _localHash;
        private readonly bool _isRemote;

        private readonly Dictionary<string, AssetBundleRuntimeInfo> _assetBundleInfos = new Dictionary<string, AssetBundleRuntimeInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, AssetBundleRuntimeInfo> _assetPaths = new Dictionary<string, AssetBundleRuntimeInfo>(StringComparer.Ordinal);
        private readonly List<(string sceneName, AssetBundleRuntimeInfo info)> _sceneNames = new List<(string sceneName, AssetBundleRuntimeInfo info)>();

        public string Name => _name;
        public string RemoteHash
        {
            get
            {
                if(!string.IsNullOrEmpty(_remoteHash))
                {
                    return _remoteHash;
                }

                return _localHash;
            }
        }

        public bool IsRemote => _isRemote;
        
        public IEnumerable<AssetBundleRuntimeInfo> AssetBundleInfos => _assetBundleInfos.Values;       
        

        public AssetCatalogInfo(IAssetBundleManager owner, string name, AssetCatalog localCatalog, AssetCatalog remoteCatalog)
        {
            _owner = owner;
            _name = name;
            _localHash = localCatalog != null ? localCatalog.MD5Hash : string.Empty;
            _remoteHash = remoteCatalog != null ? remoteCatalog.MD5Hash : string.Empty;

            if (localCatalog == null)
            {
                _isRemote = true;
            }
            else if(remoteCatalog == null)
            {
                _isRemote = false;
            }
            else
            {
                _isRemote = remoteCatalog.buildNumber >= localCatalog.buildNumber;
            }

            var catalog = _isRemote ? remoteCatalog : localCatalog;

            foreach (var info in catalog.assetBundleInfos)
            {
                var assetBundleInfo = new AssetBundleRuntimeInfo(this, info);

                _assetBundleInfos.Add(assetBundleInfo.AssetBundleName, assetBundleInfo);

                foreach (var assetPath in info.assetPaths)
                {
                    _assetPaths.Add(assetPath, assetBundleInfo);
                }

                foreach (var scenePath in info.scenePaths)
                {
                    _assetPaths.Add(scenePath, assetBundleInfo);
                    _sceneNames.Add((System.IO.Path.GetFileNameWithoutExtension(scenePath), assetBundleInfo));
                }
            }

            foreach (var info in catalog.assetBundleInfos)
            {
                _assetBundleInfos[info.assetBundleName].SetDependencies(
                    info.dependencies.Select(dependent => _assetBundleInfos[dependent]).ToArray());
            }
        }

        public bool TryGetAssetBundle(string assetBundleName, out AssetBundleRuntimeInfo assetBundleInfo)
        {
            return _assetBundleInfos.TryGetValue(assetBundleName, out assetBundleInfo);
        }

        public bool TryGetAssetBundleByAssetPath(string assetPath, out AssetBundleRuntimeInfo assetBundleInfo)
        {
            return _assetPaths.TryGetValue(assetPath, out assetBundleInfo);
        }

        public bool TryGetAssetBundleBySceneName(string sceneName, out AssetBundleRuntimeInfo assetBundleInfo)
        {
            foreach(var sceneNameInfo in _sceneNames)
            {
                if(sceneNameInfo.sceneName == sceneName)
                {
                    assetBundleInfo = sceneNameInfo.info;
                    return true;
                }
            }

            assetBundleInfo = null;
            return false;
        }

        public AssetBundleRuntimeInfo[] GetAssetBundlesByTags(string[] tags)
        {
            return _assetBundleInfos.Values.Where(assetBundleInfo =>
                    _owner.TagComparer.IsIncludeTags(assetBundleInfo.Tags, tags)).ToArray();
        }
    }

}
