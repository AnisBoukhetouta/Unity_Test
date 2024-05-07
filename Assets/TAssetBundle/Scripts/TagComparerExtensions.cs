using System.Linq;

namespace TAssetBundle
{
    internal static class TagComparerExtensions
    {
        public static bool IsIncludeTags(this ITagComparer tagComparer, string[] sourceTags, string[] targetTags)
        {
            return sourceTags.Any(sourceTag => targetTags.Any(targetTag => tagComparer.IsInclude(sourceTag, targetTag)));
        }
    }
}
