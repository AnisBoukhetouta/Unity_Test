using UnityEngine;

namespace TAssetBundle
{

    /// <summary>
    /// Asset Bundle Runtime Information
    /// </summary>
    public class AssetBundleRuntimeInfo
    {
        public IAssetCatalogInfo Catalog { get; private set; }
        public string AssetBundleName { get; private set; }
        public string HashString { get; private set; }
        public Hash128 Hash { get; private set; }
        public AssetBundleRuntimeInfo[] Dependencies { get; private set; }
        public long Size { get; private set; }
        public bool Builtin { get; private set; }
        public bool Encrypt { get; private set; }
        public string[] Tags { get; private set; }

        public AssetBundleRuntimeInfo(IAssetCatalogInfo catalog, AssetBundleInfo assetBundleInfo)
        {
            Catalog = catalog;
            AssetBundleName = assetBundleInfo.assetBundleName;
            HashString = assetBundleInfo.hashString;
            Hash = string.IsNullOrEmpty(HashString) ? Defines.DefaultHash : Hash128.Parse(HashString);
            Size = assetBundleInfo.size;
            Builtin = assetBundleInfo.builtin;
            Encrypt = assetBundleInfo.encrypt;
            Tags = assetBundleInfo.tags;
        }

        internal void SetDependencies(AssetBundleRuntimeInfo[] dependencies)
        {
            Dependencies = dependencies;
        }

        public string GetName(bool withHash)
        {
            return Util.GetAssetBundleName(AssetBundleName, HashString, withHash);
        }

        public string GetNameWithTags()
        {
            return Tags.Length > 0 ? string.Format("{0}[{1}]", AssetBundleName, string.Join(", ", Tags)) : AssetBundleName;
        }
    }
}