using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    public static class TAssetBundleManifestUtil
    {
        /// <summary>
        /// Get manifest by asset bundle name
        /// </summary>
        /// <param name="assetBundleName">assetBundleName</param>
        /// <returns>TAssetBundleManifest</returns>
        public static TAssetBundleManifest GetManifest(string assetBundleName)
        {
            var manifestFilePath = Path.Combine("Assets", Path.GetDirectoryName(assetBundleName) + ".asset");

            if (!File.Exists(manifestFilePath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<TAssetBundleManifest>(manifestFilePath);
        }

        /// <summary>
        /// Get the build name of the Assets
        /// </summary>
        /// <param name="manifest">manifest</param>
        /// <param name="objects">assets</param>
        /// <param name="buildName">asset bundle build name</param>
        /// <returns></returns>
        public static string GetAssetBundleBuildName(this TAssetBundleManifest manifest,
            List<Object> objects,
            EAssetBundleBuildName buildName)
        {
            if (buildName == EAssetBundleBuildName.FirstObject)
            {
                if (objects.Count > 0)
                    return objects.First().name;
            }
            else if (buildName == EAssetBundleBuildName.Number)
            {
                return (manifest.assetBundleBuildInfos.Count + 1).ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// Add AssetBundle Build to Manifest
        /// </summary>
        /// <param name="manifest">manifest</param>
        /// <param name="objects">assets</param>
        /// <param name="buildName"></param>
        public static void AddAssetBundleBuild(this TAssetBundleManifest manifest,
            List<Object> objects,
            EAssetBundleBuildName buildName)
        {
            if (objects.Count == 0)
                return;

            manifest.AddAssetBundleBuildInfo(new AssetBundleBuildInfo
            {
                buildName = manifest.GetAssetBundleBuildName(objects, buildName),
                objects = objects
            });
        }
    }
}