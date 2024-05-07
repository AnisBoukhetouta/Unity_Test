using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    public class RegexMatchCompositionStrategyData : CompositionStrategyBuildData
    {
        public string matchPattern;
    }

    [CreateAssetMenu(menuName = "TAssetBundle/Composition Strategy/Regex Match")]
    public class RegexMatchCompositionStrategy : TAssetBundleCompositionStrategy<RegexMatchCompositionStrategyData>
    {
        public override void Run(TAssetBundleManifest manifest, Data data)
        {
            var d = data as RegexMatchCompositionStrategyData;

            var objects = new List<Object>();

            foreach (var assetPath in manifest.GetNotIncludedAssetPaths())
            {
                if (Regex.IsMatch(Path.GetFileName(assetPath), d.matchPattern))
                {
                    objects.Add(AssetDatabase.LoadMainAssetAtPath(assetPath));
                }
            }

            manifest.AddAssetBundleBuild(objects, d.assetBundleBuildName);
        }
    }

}