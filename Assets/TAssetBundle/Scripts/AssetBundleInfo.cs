using System;
using UnityEngine;

namespace TAssetBundle
{
    [Serializable]
    public class AssetBundleInfo
    {
        public string assetBundleName;
        public string hashString;
        public string[] dependencies;
        public string[] assetPaths;
        public string[] scenePaths;
        public long size;
        public bool builtin;
        public bool encrypt;
        public string[] tags;

        public string GetFileName(bool withHash)
        {
            return Util.GetAssetBundleName(assetBundleName, hashString, withHash);
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this, true);
        }
    }
}

