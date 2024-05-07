using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    [CreateAssetMenu(menuName = "TAssetBundle/Composition Strategy/Same Name Together")]
    public class SameNameCompositionStrategy : TAssetBundleCompositionStrategy<CompositionStrategyBuildData>
    {
        public override void Run(TAssetBundleManifest manifest, Data data)
        {
            var d = data as CompositionStrategyBuildData;
            var assetPaths = manifest.GetNotIncludedAssetPaths();
            var assetNames = new HashSet<string>(assetPaths.Select(path => Path.GetFileNameWithoutExtension(path)));

            foreach (var assetName in assetNames)
            {
                var filteredAssetPahts = assetPaths.Where(path => Path.GetFileNameWithoutExtension(path) == assetName).ToArray();
                var objects = filteredAssetPahts.Select(path => AssetDatabase.LoadMainAssetAtPath(path)).ToList();

                manifest.AddAssetBundleBuild(objects, d.assetBundleBuildName);
            }
        }
    }
}
