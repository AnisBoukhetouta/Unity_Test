using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace TAssetBundle
{


    [Serializable]
    internal struct AssetBundleCacheVersion
    {
        public string hash;
        public string path;

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    internal class AssetBundleCacheData
    {
        public string name;
        public AssetBundleCacheVersion[] versions;
    }


    internal class AssetBundleCacheInfo
    {
        private readonly List<AssetBundleCacheVersion> _versions;

        public List<AssetBundleCacheVersion> Versions => _versions;

        public AssetBundleCacheInfo(IEnumerable<AssetBundleCacheVersion> versions)
        {
            _versions = new List<AssetBundleCacheVersion>(versions);
        }

        public bool UpdateVersion(AssetBundleCacheVersion newVersion)
        {
            if (_versions.Count > 0)
            {
                var lastVersion = _versions[_versions.Count - 1];

                if (lastVersion.hash == newVersion.hash &&
                    lastVersion.path == newVersion.path)
                {
                    return false;
                }
            }

            _versions.Add(newVersion);

            return true;
        }
    }

    /// <summary>
    /// 따로 관리되는 캐시 된 에셋 번들 정보를 관리하는 매니저    
    /// </summary>
    internal class AssetBundleCacheManager
    {
        private readonly Dictionary<string, AssetBundleCacheInfo> _cacheInfos = new Dictionary<string, AssetBundleCacheInfo>();

        public event Action<AssetBundleCacheVersion> OnRemovedVersion;


        private string GetRootPath()
        {
            return Path.Combine(CachingWrapper.GetPath(), "cache_info");
        }

        private string GetFilePath(string fileName)
        {
#if UNITY_EDITOR
            fileName = fileName.Replace("/", "_");
#else
            fileName = Util.GetMD5HashFromString(fileName);            
#endif
            return Path.Combine(GetRootPath(), fileName);
        }

        public AssetBundleCacheInfo GetCacheInfo(string name)
        {
            if (!_cacheInfos.TryGetValue(name, out AssetBundleCacheInfo info))
            {
                if (!TryLoad(GetFilePath(name), out AssetBundleCacheData cacheData))
                {
                    cacheData = new AssetBundleCacheData
                    {
                        name = name,
                        versions = new AssetBundleCacheVersion[0]
                    };
                }

                info = new AssetBundleCacheInfo(cacheData.versions);
                _cacheInfos.Add(name, info);
            }

            return info;
        }

        public void UpdateVersion(string name, AssetBundleCacheVersion version)
        {
            var cacheInfo = GetCacheInfo(name);
            var filePath = GetFilePath(name);

            if (cacheInfo.UpdateVersion(version) || !File.Exists(filePath))
            {
                Save(filePath, new AssetBundleCacheData
                {
                    name = name,
                    versions = cacheInfo.Versions.ToArray()
                });
            }
        }

        public void RemoveNotUsedAssetBundleVersions()
        {
            Logger.Log("remove not used assetbundle versions");

            var rootPath = GetRootPath();

            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);

            var filePaths = Directory.GetFiles(rootPath);

            _cacheInfos.Clear();

            foreach (var filePath in filePaths)
            {
                if (!TryLoad(filePath, out AssetBundleCacheData data))
                {
                    continue;
                }

                RemoveNotUsedVersion(data);
            }
        }

        private void RemoveNotUsedVersion(AssetBundleCacheData data)
        {
            if (data.versions == null || data.versions.Length < 2)
                return;

            var lastIndex = data.versions.Length - 1;
            var lastVersion = data.versions[lastIndex];

            for (int i = 0; i < lastIndex; ++i)
            {
                var version = data.versions[i];
                Logger.Log("remove not used version - " + version);
                OnRemovedVersion?.Invoke(version);
            }

            data.versions = new AssetBundleCacheVersion[] { lastVersion };

            Save(GetFilePath(data.name), data);
        }

        private bool TryLoad(string filePath, out AssetBundleCacheData data)
        {
            data = null;

            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                data = JsonUtility.FromJson<AssetBundleCacheData>(json);
            }
            catch
            {
                File.Delete(filePath);
                Logger.Warning("invalid cache info - " + filePath);
                return false;
            }

            return true;
        }

        public void Save(string filePath, AssetBundleCacheData cacheData)
        {
            Util.CreateDirectoryFromFilePath(filePath);
            var json = JsonUtility.ToJson(cacheData, true);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }
    }
}
