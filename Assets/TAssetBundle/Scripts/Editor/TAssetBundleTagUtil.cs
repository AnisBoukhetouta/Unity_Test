

using System.Collections.Generic;
using UnityEditor;

namespace TAssetBundle.Editor
{
    internal static class TAssetBundleTagUtil
    {
        public const string TagFilePath = "Assets/Editor/TAssetBundleTag.asset";

        public static TAssetBundleTagRepository GetTagRepository()
        {
            return EditorUtil.GetOrCreateScriptableFile<TAssetBundleTagRepository>(TagFilePath);
        }


        public static void AddTags(this TAssetBundleTagRepository tagRepository, IEnumerable<string> tags)
        {
            var dirty = false;

            foreach (var tag in tags)
            {
                if (!tagRepository.tags.Contains(tag))
                {
                    tagRepository.tags.Add(tag);
                    dirty = true;
                }
            }

            if (dirty)
            {
                EditorUtility.SetDirty(tagRepository);
            }
        }
    }

}
