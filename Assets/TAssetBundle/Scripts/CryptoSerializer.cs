using UnityEngine;

namespace TAssetBundle
{
    /// <summary>
    /// Crypto Serializer
    /// </summary>
    public abstract class CryptoSerializer : ScriptableObject
    {
        /// <summary>
        /// Initialize Crypto Serializer
        /// </summary>
        /// <param name="key">Settings.BuildSetting.cryptoKey</param>
        public abstract void Init(string key);

        /// <summary>
        /// Encrypt
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <returns>bytes</returns>
        public abstract byte[] Encrypt(byte[] bytes);

        /// <summary>
        /// Decrypt
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <returns>bytes</returns>
        public abstract byte[] Decrypt(byte[] bytes);
    }

}