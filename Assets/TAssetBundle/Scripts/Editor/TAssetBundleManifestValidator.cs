using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace TAssetBundle.Editor
{
    internal static class TAssetBundleManifestValidator
    {
        private static TAssetBundleManifest _manifest;
        private static readonly HashSet<UnityEngine.Object> _validAssets = new HashSet<UnityEngine.Object>();
        private static readonly List<TAssetBundleManifest> _otherManifests = new List<TAssetBundleManifest>();
        private static readonly HashSet<UnityEngine.Object> _duplicateCheck = new HashSet<UnityEngine.Object>();

        public static bool CheckValidate(TAssetBundleManifest manifest)
        {
            _manifest = manifest;
            _otherManifests.Clear();
            _otherManifests.AddRange(manifest.GetManifestTree().Where(m => m != manifest));
            _validAssets.Clear();

            foreach (var asset in manifest.GetValidAssets())
            {                
                _validAssets.Add(asset);
            }

            var changed = CheckCompositionStrategyInfos(manifest);
            changed |= CheckRemoveInvalidPathIgnoreAsset(manifest);
            changed |= CheckAssetBundleBuildInfos(manifest);

            return changed;
        }

        private static bool CheckCompositionStrategyInfos(TAssetBundleManifest manifest)
        {
            bool changed = false;
            for (int i = 0; i < manifest.compositionStrategyInfos.Count; ++i)
            {
                var info = manifest.compositionStrategyInfos[i];

                if (info.strategy != null)
                {
                    if (info.data == null ||
                        manifest.compositionStrategyInfos.FindIndex(cs => cs.data == info.data) < i ||
                        info.data.GetType() != info.strategy.GetDataType())
                    {
                        info.data = info.strategy.CreateData();
                        changed = true;
                    }
                }
                else if (info.data != null)
                {
                    info.data = null;
                    changed = true;
                }
            }

            return changed;
        }

        private static bool CheckRemoveInvalidPathIgnoreAsset(TAssetBundleManifest manifest)
        {
            return CheckRemoveInvalidPathObjects(manifest.ignoreAssets, false);
        }

        private static bool CheckAssetBundleBuildInfos(TAssetBundleManifest manifest)
        {
            bool removed = false;

            _duplicateCheck.Clear();

            var assetBundleBuildInfos = manifest.assetBundleBuildInfos.ToArray();

            foreach (var info in assetBundleBuildInfos)
            {
                if (string.IsNullOrEmpty(info.buildName) && info.objects.Count > 0 && info.objects.First() != null)
                {
                    info.buildName = info.objects.First().name;
                    manifest.MarkAsDirty();
                }

                removed |= CheckRemoveIgnoreAssets(info);
                removed |= CheckRemoveInvalidPathObjects(info);
                removed |= CheckRemoveDuplicateAssets(info);

                if(removed && info.objects.Count == 0)
                {
                    info.buildName = string.Empty;
                }
            }

            return removed;
        }

        private static bool CheckRemoveIgnoreAssets(AssetBundleBuildInfo info)
        {
            var count = info.objects.Count;

            var ignoreAssets = info.objects.Where(asset => _manifest.ignoreAssets.Contains(asset)).ToArray();
            
            foreach(var asset in ignoreAssets)
            {
                Logger.Warning(string.Format("remove ignore asset - manifest:{0}, asset:{1}",
                    _manifest.ManifestPath, AssetDatabase.GetAssetPath(asset)));

                info.objects.Remove(asset);
            }

            return count > info.objects.Count;
        }

        private static bool CheckRemoveInvalidPathObjects(List<UnityEngine.Object> objects, bool removeNull)
        {
            var count = objects.Count;

            UnityEngine.Object[] invalidPathObjects;

            if (removeNull)
            {
                objects.RemoveAll(obj => obj == null);
                invalidPathObjects = objects.Where(obj => !_validAssets.Contains(obj)).ToArray();
            }
            else
            {
                invalidPathObjects = objects.Where(obj => obj != null && !_validAssets.Contains(obj)).ToArray();
            }

            foreach (var invalidPathObj in invalidPathObjects)
            {   
                objects.Remove(invalidPathObj);

                Logger.Warning(string.Format("not managed asset path - manifest:{0}, asset:{1}",
                    _manifest.ManifestPath, AssetDatabase.GetAssetPath(invalidPathObj)));
            }

            return count > objects.Count;
        }


        private static bool CheckRemoveInvalidPathObjects(AssetBundleBuildInfo info)
        {
            return CheckRemoveInvalidPathObjects(info.objects, true);
        }

        private static bool CheckRemoveDuplicateAssets(AssetBundleBuildInfo info)
        {
            bool isDuplicate = false;

            foreach(var asset in info.objects.ToArray())
            {
                if(!_duplicateCheck.Add(asset))
                {
                    Logger.Warning("duplicate asset - " + AssetDatabase.GetAssetPath(asset));
                    info.objects.Remove(asset);
                    isDuplicate = true;
                }
            }

            var otherManifestDuplicates = info.objects.Where(obj =>
            {   
                foreach (var otherManifest in _otherManifests)
                {
                    if (otherManifest.IsIncludedAsset(obj))
                    {
                        Logger.Warning(string.Format("other manifest duplicate asset - {0}, {1}", 
                            otherManifest.ManifestPath, AssetDatabase.GetAssetPath(obj)));
                        return true;
                    }
                }

                return false;

            }).ToArray();

            foreach (var duplicate in otherManifestDuplicates)
            {
                info.objects.Remove(duplicate);
            }

            isDuplicate |= otherManifestDuplicates.Length > 0;

            return isDuplicate;
        }
    }

}
