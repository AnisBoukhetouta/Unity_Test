using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    public class FixedCompositionStrategyData : CompositionStrategyBuildData
    {
        public List<UnityEngine.Object> fixedObjects;
    }

    [CreateAssetMenu(menuName = "TAssetBundle/Composition Strategy/Fixed Objects")]
    public class FixedCompositionStrategy : TAssetBundleCompositionStrategy<FixedCompositionStrategyData>
    {
        public override void Run(TAssetBundleManifest manifest, Data data)
        {
            var d = data as FixedCompositionStrategyData;

            if (d.fixedObjects == null || d.fixedObjects.Count == 0)
                return;

            var assetPaths = manifest.GetNotIncludedAssetPaths();
            var objects = new List<Object>();

            foreach (var assetPath in assetPaths)
            {
                var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);

                if (d.fixedObjects.Contains(obj))
                {
                    objects.Add(obj);
                }
            }

            manifest.AddAssetBundleBuild(objects, d.assetBundleBuildName);
        }
    }

}
