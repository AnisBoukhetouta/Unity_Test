using System.Text;
using UnityEngine;

namespace TAssetBundle
{
    /// <summary>
    /// Json Catalog Serializer
    /// </summary>
    public class JsonCatalogSerializer : CatalogSerializer
    {
        public override AssetCatalog Read(byte[] data)
        {
            return JsonUtility.FromJson<AssetCatalog>(Encoding.UTF8.GetString(data));
        }

        public override byte[] Write(AssetCatalog catalog)
        {
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(catalog, true));
        }
    }
}
