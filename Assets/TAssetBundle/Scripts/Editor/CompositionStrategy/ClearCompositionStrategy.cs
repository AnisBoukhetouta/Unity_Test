using UnityEngine;

namespace TAssetBundle.Editor
{
    [CreateAssetMenu(menuName = "TAssetBundle/Composition Strategy/Clear")]
    public class ClearCompositionStrategy : TAssetBundleCompositionStrategy
    {
        public override void Run(TAssetBundleManifest manifest, Data data)
        {
            manifest.assetBundleBuildInfos.Clear();
            manifest.MarkAsDirty();
        }
    }

}