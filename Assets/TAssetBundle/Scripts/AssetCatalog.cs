using System;

namespace TAssetBundle
{

    [Serializable]
    public class AssetCatalog
    {
        public string buildVersion;
        public int catalogVersion;
        public int buildNumber;        
        public AssetBundleInfo[] assetBundleInfos;

        public string MD5Hash { get; private set; }

        public void Init(string md5Hash)
        {
            MD5Hash = md5Hash;
        }

        public AssetBundleInfo FindAssetBundle(string assetBundleName)
        {
            foreach (var assetBundleInfo in assetBundleInfos)
            {
                if (assetBundleInfo.assetBundleName == assetBundleName)
                    return assetBundleInfo;
            }

            return null;
        }
    }

}
