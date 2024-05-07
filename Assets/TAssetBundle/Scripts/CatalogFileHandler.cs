using System.IO;

namespace TAssetBundle
{
    /// <summary>
    /// Catalog File Save And Load Handler
    /// </summary>
    public class CatalogFileHandler
    {
        private readonly CatalogSerializer _catalogSerializer;
        private readonly CryptoSerializer _cryptoSerializer;
        private readonly string _fileExtensions;
        private readonly bool _compress;
        private readonly bool _encrypt;

        public CryptoSerializer CryptoSerializer => _cryptoSerializer;

        public CatalogFileHandler(CatalogSerializer catalogSerializer,
            CryptoSerializer cryptoSerializer,
            string fileExtensions,
            bool compress,
            bool encrypt)
        {
            _catalogSerializer = catalogSerializer;
            _cryptoSerializer = cryptoSerializer;
            _fileExtensions = fileExtensions;
            _compress = compress;
            _encrypt = encrypt;
        }

        /// <summary>
        /// Save Catalog
        /// </summary>
        /// <param name="catalog">catalog</param>
        /// <param name="catalogPath">catalog path</param>
        public void Save(AssetCatalog catalog, string catalogPath)
        {
            var bytes = _catalogSerializer.Write(catalog);

            if (_compress)
            {
                Logger.Log("compress catalog - " + catalogPath);
                bytes = Util.Compress(bytes);
            }

            if (_encrypt)
            {
                Logger.Log("encrypt catalog - " + catalogPath);
                bytes = _cryptoSerializer.Encrypt(bytes);
            }

            File.WriteAllBytes(catalogPath + _fileExtensions, bytes);
            File.WriteAllText(catalogPath + Defines.HashFileExtensions, Util.GetMD5Hash(bytes));
        }


        /// <summary>
        /// Load Catalog from file
        /// </summary>
        /// <param name="catalogPath">catalog path</param>
        /// <returns>catalog</returns>
        public AssetCatalog LoadFromFile(string catalogPath)
        {
            string catalogDataPath = catalogPath + _fileExtensions;

            if (!File.Exists(catalogDataPath))
                return null;

            return Load(File.ReadAllBytes(catalogDataPath));
        }



        /// <summary>
        /// Load Catalog
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <returns>catalog</returns>
        public AssetCatalog Load(byte[] bytes)
        {
            var hash = Util.GetMD5Hash(bytes);

            if (_encrypt)
            {
                bytes = _cryptoSerializer.Decrypt(bytes);
            }

            if (_compress)
            {
                bytes = Util.Decompress(bytes);
            }

            var catalog = _catalogSerializer.Read(bytes);
            catalog.Init(hash);

            return catalog;
        }

        /// <summary>
        /// Delete Catalog
        /// </summary>
        /// <param name="catalogPath">catalog path</param>
        public void DeleteCatalog(string catalogPath)
        {
            File.Delete(catalogPath + _fileExtensions);
            File.Delete(catalogPath + Defines.HashFileExtensions);
        }
    }

}
