using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TAssetBundle.Editor
{

    [CreateAssetMenu(menuName = "TAssetBundle/Composition Strategy/Regex Match Delete")]
    public class RegexMatchDeleteCompositionStrategy : RegexMatchCompositionStrategy
    {
        public bool isMatchRemove = true;

        public override void Run(TAssetBundleManifest manifest, Data data)
        {
            var d = data as RegexMatchCompositionStrategyData;

            var buildInfos = manifest.assetBundleBuildInfos.ToArray();

            foreach (var buildInfo in buildInfos)
            {
                var isMatch = buildInfo.ObjectPaths.Any(assetPath =>
                {
                    return Regex.IsMatch(Path.GetFileName(assetPath), d.matchPattern);
                });

                if (isMatch == isMatchRemove)
                {
                    if (isMatchRemove)
                    {
                        Logger.Log("regex match remove asset bundle - " + buildInfo.buildName);
                    }
                    else
                    {
                        Logger.Log("regex not match remove asset bundle - " + buildInfo.buildName);
                    }

                    manifest.assetBundleBuildInfos.Remove(buildInfo);
                    manifest.MarkAsDirty();
                }
            }
        }
    }

}