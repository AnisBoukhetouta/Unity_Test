using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    [CreateAssetMenu(menuName = "TAssetBundle/Composition Strategy/Default")]
    public class DefaultCompositionStrategy : TAssetBundleCompositionStrategy<CompositionStrategyBuildData>
    {
        public enum ECompositionType
        {
            Together,
            Separatly
        }

        public ECompositionType composition = ECompositionType.Together;
        public bool includeFolder = true;
        public bool includeFile = true;



        public override void Run(TAssetBundleManifest manifest, Data data)
        {
            var assetPaths = manifest.GetNotIncludedAssetPaths();

            if (composition == ECompositionType.Together)
            {
                AddAssetBuildBuildInfos(manifest, data, assetPaths);
            }
            else
            {
                foreach (var assetPath in assetPaths)
                {
                    AddAssetBuildBuildInfos(manifest, data, new string[] { assetPath });
                }
            }
        }

        private void AddAssetBuildBuildInfos(TAssetBundleManifest manifest,
            Data data,
            string[] assetPaths)
        {
            var d = data as CompositionStrategyBuildData;
            var filteredAssetPaths = assetPaths.Where(path =>
            {
                bool isFolder = AssetDatabase.IsValidFolder(path);

                if (includeFolder && isFolder)
                {
                    return true;
                }

                if (includeFile && !isFolder)
                {
                    return true;
                }

                return false;

            });

            if (filteredAssetPaths.Count() == 0)
            {
                return;
            }

            var objects = filteredAssetPaths.Select(path => AssetDatabase.LoadMainAssetAtPath(path));

            manifest.AddAssetBundleBuild(objects.ToList(), d.assetBundleBuildName);
        }
    }
}
