using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    public class ExcludeObjectsCompositionStrategyData : TAssetBundleCompositionStrategy.Data
    {
        public List<UnityEngine.Object> excludeObjects;
    }

    [CreateAssetMenu(menuName = "TAssetBundle/Composition Strategy/Exclude Objects")]
    public class ExcludeObjectsStrategy : TAssetBundleCompositionStrategy<ExcludeObjectsCompositionStrategyData>
    {
        public override void Run(TAssetBundleManifest manifest, Data data)
        {
            var d = data as ExcludeObjectsCompositionStrategyData;

            if (d.excludeObjects == null || d.excludeObjects.Count == 0)
                return;

            foreach (var info in manifest.assetBundleBuildInfos)
            {
                foreach (var deleteObj in d.excludeObjects)
                {
                    if (info.objects.Remove(deleteObj))
                    {
                        Logger.Log("exclude asset - " + AssetDatabase.GetAssetPath(deleteObj));
                        manifest.MarkAsDirty();
                    }
                }
            }
        }
    }

}
