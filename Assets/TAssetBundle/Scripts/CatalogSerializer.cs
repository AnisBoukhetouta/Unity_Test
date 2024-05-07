using UnityEngine;

namespace TAssetBundle
{
    /// <summary>
    /// Catalog Serializer
    /// </summary>
    public abstract class CatalogSerializer : ScriptableObject
    {
        /// <summary>
        /// Write Catalog
        /// </summary>
        /// <param name="catalog">catalog</param>
        /// <returns>bytes</returns>
        public abstract byte[] Write(AssetCatalog catalog);

        /// <summary>
        /// Read Catalog
        /// </summary>
        /// <param name="data">bytes</param>
        /// <returns>catalog</returns>
        public abstract AssetCatalog Read(byte[] data);
    }

}