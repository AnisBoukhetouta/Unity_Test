using UnityEngine;
using System.Linq;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System;
using System.IO.Compression;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TAssetBundle
{

    public static class Util
    {
        private const string x2 = "x2";

        public static string BytesToHexString(byte[] bytes)
        {
            return string.Concat(bytes.Select(x => x.ToString(x2)));
        }

        public static string GetMD5Hash(byte[] bytes)
        {
            using (var md5 = MD5.Create())
            {
                return BytesToHexString(md5.ComputeHash(bytes));
            }
        }

        public static string GetMD5HashFromString(string inputString)
        {
            return GetMD5Hash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetMD5HashFromFile(string filePath)
        {
            return GetMD5Hash(File.ReadAllBytes(filePath));
        }

        public static byte[] GetSHA256HashFromString(string inputString)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(inputString));
            }
        }

        public static byte[] EncryptRijndael(byte[] bytes, byte[] key, byte[] iv)
        {
            using (var rijndael = RijndaelManaged.Create())
            {
                rijndael.Key = key;
                rijndael.IV = iv;

                using (var encryptor = rijndael.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                }
            }
        }

        public static byte[] DecryptRijndael(byte[] bytes, byte[] key, byte[] iv)
        {
            using (var rijndael = RijndaelManaged.Create())
            {
                rijndael.Key = key;
                rijndael.IV = iv;

                using (var decryptor = rijndael.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                }
            }
        }

        public static byte[] Compress(byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException("inputData must be non-null");

            using (var output = new MemoryStream())
            {
                using (var dstream = new DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
                {
                    dstream.Write(inputData, 0, inputData.Length);
                }

                return output.ToArray();
            }
        }


        public static byte[] Decompress(byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException("inputData must be non-null");

            using (var input = new MemoryStream(inputData))
            {
                using (var output = new MemoryStream())
                {
                    using (var dstream = new DeflateStream(input, CompressionMode.Decompress))
                    {
                        dstream.CopyTo(output);
                    }

                    return output.ToArray();
                }
            }
        }

        public static string GetDataPath(string relativePath = "")
        {
            return Path.Combine(Application.persistentDataPath, Defines.DataPathPrefix, relativePath);
        }
        

        public static void CreateDirectoryFromFilePath(string filePath)
        {
            var dirPath = Path.GetDirectoryName(filePath);

            if (Directory.Exists(dirPath))
                return;

            Directory.CreateDirectory(dirPath);
        }

        public static string GetAssetBundleName(string assetBundleName, string hash, bool withHash)
        {
            if (withHash)
            {
                return GetAssetBundleName(assetBundleName, hash);
            }
            else
            {
                return assetBundleName;
            }
        }

        public static string GetAssetBundleName(string assetBundleName, string hash)
        {
            return string.Format("{0}_{1}", assetBundleName, hash);
        }

        public static void ClearCachedAssets()
        {
            CachingWrapper.ClearCache();
        }

        public static void ClearCachedRemoteCatalogs()
        {
            if (Directory.Exists(GetDataPath()))
            {
                Directory.Delete(GetDataPath(), true);
            }
        }


#if UNITY_EDITOR
        public static BuildTarget GetActiveBuildTarget()
        {
            return EditorUserBuildSettings.activeBuildTarget;
        }
#endif
    }

}