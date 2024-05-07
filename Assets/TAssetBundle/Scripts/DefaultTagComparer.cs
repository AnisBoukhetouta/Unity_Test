namespace TAssetBundle
{
    /// <summary>
    /// Default Tag Comparer
    /// </summary>
    public class DefaultTagComparer : ITagComparer
    {
        public bool IsInclude(string source, string target)
        {
            return source.StartsWith(target);
        }
    }

}
