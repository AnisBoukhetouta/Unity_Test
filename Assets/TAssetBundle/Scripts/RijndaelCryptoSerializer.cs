using System;

namespace TAssetBundle
{
    /// <summary>
    /// Default Crypto Serializer
    /// </summary>
    public class RijndaelCryptoSerializer : CryptoSerializer
    {
        private byte[] _key;
        private byte[] _iv;

        public override void Init(string key)
        {
            char[] chars = key.ToCharArray();
            Array.Reverse(chars);

            _key = Util.GetSHA256HashFromString(new string(chars));
            _iv = new byte[16];
            Array.Copy(_key, _iv, _iv.Length);
        }

        public override byte[] Encrypt(byte[] bytes)
        {
            return Util.EncryptRijndael(bytes, _key, _iv);
        }

        public override byte[] Decrypt(byte[] bytes)
        {
            return Util.DecryptRijndael(bytes, _key, _iv);
        }

    }

}