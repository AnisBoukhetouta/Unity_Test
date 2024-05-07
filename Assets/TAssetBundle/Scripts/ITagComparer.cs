
namespace TAssetBundle
{
    /// <summary>
    /// Tag Comaparer
    /// </summary>
    public interface ITagComparer
    {
        /// <summary>
        /// Whether the source tag is included in the target tag
        /// </summary>
        /// <param name="source">AssetBundles Tag</param>
        /// <param name="target">Requested Tag</param>
        /// <returns>true if the tag is included, false otherwise</returns>
        bool IsInclude(string source, string target);
    }
}
