using System;
using System.Collections.Generic;
using System.Linq;

namespace TAssetBundle.Editor.Cache
{

    [Serializable]
    internal class BuildInfo
    {

        [Serializable]
        public struct FileInfo
        {
            public readonly static FileInfo Empty = new FileInfo { path = string.Empty };

            public string path;
            public string hash;
            public string metaHash;

            public static FileInfo Create(string filePath)
            {
                return new FileInfo
                {
                    path = filePath.ToLower(),
                    hash = Util.GetMD5HashFromFile(filePath),
                    metaHash = Util.GetMD5HashFromFile(filePath + ".meta")
                };
            }

            public bool IsChanged(FileInfo other)
            {
                return hash != other.hash || metaHash != other.metaHash;                    
            }
        }

        [Serializable]
        public class AssetBundleCacheInfo
        {
            public string assetBundleName;
            public FileInfo[] files;

            public bool TryGetFileCacheInfo(string filePath, out FileInfo fileInfo)
            {
                fileInfo = FileInfo.Empty;
                foreach (var info in files)
                {
                    if(info.path == filePath)
                    {
                        fileInfo = info;
                        return true;
                    }
                }

                return false;
            }
        }

        public string catalogName;
        public int catalogVersion;
        public Settings.BuildSetting build;
        public AssetBundleCacheInfo[] assetBundleCacheInfos;



        public enum EModifyType
        {
            Equal,
            Modified,
            Added,
            Deleted
        }

        public struct DiffInfo
        {
            public EModifyType type;
            public AssetBundleCacheInfo prev;
            public AssetBundleCacheInfo current;

            public bool IsEqual => type == EModifyType.Equal;
            public bool IsNotUsed => type == EModifyType.Modified || type == EModifyType.Deleted;

            public static DiffInfo Create(AssetBundleCacheInfo prev, AssetBundleCacheInfo current, EModifyType type)
            {
                return new DiffInfo
                {
                    type = type,
                    prev = prev,
                    current = current
                };
            }
        }


        public List<DiffInfo> GetDiffList(AssetCatalog cachedCatalog, BuildInfo cachedBuildInfo)
        {
            List<DiffInfo> diffList = new List<DiffInfo>();
            List<DiffInfo> equals = new List<DiffInfo>();

            foreach (var prevAssetBundleCacheInfo in cachedBuildInfo.assetBundleCacheInfos)
            {
                var assetBundleCacheInfo = FindAssetBundleInfo(prevAssetBundleCacheInfo.assetBundleName);

                if (assetBundleCacheInfo == null)
                {
                    //에셋번들이 지워짐
                    diffList.Add(DiffInfo.Create(prevAssetBundleCacheInfo, null, EModifyType.Deleted));
                }
                else if (IsChanged(prevAssetBundleCacheInfo, assetBundleCacheInfo))
                {
                    //에셋번들이 변경됨
                    diffList.Add(DiffInfo.Create(prevAssetBundleCacheInfo, assetBundleCacheInfo, EModifyType.Modified));
                }
                else
                {
                    equals.Add(DiffInfo.Create(prevAssetBundleCacheInfo, assetBundleCacheInfo, EModifyType.Equal));
                }
            }

            var notUsedAssetBundleNames = diffList.Where(diff => diff.IsNotUsed)
                .Select(diff => diff.prev.assetBundleName).ToList();

            foreach (var diff in equals)
            {
                var assetBundleData = cachedCatalog.FindAssetBundle(diff.prev.assetBundleName);

                //같은 에셋 번들이지만 의존중인 에셋번들이 지워지거나 변경됐다면 변경된 에셋번들이다
                bool depedentNotUsed = assetBundleData.dependencies.Any(dependent => notUsedAssetBundleNames.Contains(dependent));

                diffList.Add(DiffInfo.Create(diff.prev, diff.current, depedentNotUsed ? EModifyType.Modified : EModifyType.Equal));
            }


            foreach (var assetBundleCacheInfo in assetBundleCacheInfos)
            {
                var prevAssetBundleCacheInfo = cachedBuildInfo.FindAssetBundleInfo(assetBundleCacheInfo.assetBundleName);

                if (prevAssetBundleCacheInfo != null)
                    continue;

                //에셋번들이 추가됨
                diffList.Add(DiffInfo.Create(null, assetBundleCacheInfo, EModifyType.Added));
            }

            return diffList;
        }

        private static bool IsChanged(AssetBundleCacheInfo prevAssetBundleCache, AssetBundleCacheInfo newAssetBundleCache)
        {
            var equalFiles = new HashSet<string>();

            foreach (var prevFileInfo in prevAssetBundleCache.files)
            {
                if (!newAssetBundleCache.TryGetFileCacheInfo(prevFileInfo.path, out FileInfo currentFileInfo))
                {
                    //에셋이 지워짐
                    return true;
                }

                //에셋이 변경됨
                if (prevFileInfo.IsChanged(currentFileInfo))
                {
                    return true;
                }

                equalFiles.Add(prevFileInfo.path);
            }

            foreach (var newFileInfo in newAssetBundleCache.files)
            {
                if (equalFiles.Contains(newFileInfo.path))
                {
                    continue;
                }

                //에셋이 추가됨
                return true;
            }

            return false;
        }

        public AssetBundleCacheInfo FindAssetBundleInfo(string assetBundleName)
        {
            foreach (var assetBundleCacheInfo in assetBundleCacheInfos)
            {
                if (assetBundleCacheInfo.assetBundleName == assetBundleName)
                {
                    return assetBundleCacheInfo;
                }
            }

            return null;
        }
    }
}