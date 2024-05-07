using System.Collections.Generic;


namespace TAssetBundle
{

    public interface IAssetCatalogInfo
    {
        string Name { get; }
        IEnumerable<AssetBundleRuntimeInfo> AssetBundleInfos { get; }
    }

}
