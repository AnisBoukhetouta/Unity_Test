
#if !UNITY_WEBGL
#define SUPPORT_UNITY_CACHING
#else
using System.IO;
#endif

using UnityEngine;

namespace TAssetBundle
{
    internal static class CachingWrapper
    { 
        public static bool IsSupport()
        {
#if SUPPORT_UNITY_CACHING
            return true;
#else
            return false;
#endif
        }
        public static bool IsVersionCached(string url, Hash128 hash)
        {
#if SUPPORT_UNITY_CACHING
            return Caching.IsVersionCached(url, hash);
#else
            return true;
#endif
        }

        public static void ClearCache()
        {
#if SUPPORT_UNITY_CACHING
            Caching.ClearCache();            
#else
            if (Directory.Exists(GetPath()))
            {
                Directory.Delete(GetPath(), true);
            }
#endif
        }        

        public static string GetPath()
        {
#if SUPPORT_UNITY_CACHING
            return Caching.currentCacheForWriting.path;
#else
            return Path.Combine(Application.persistentDataPath, "TACache");
#endif
        }

        public static bool IsValid()
        {
#if SUPPORT_UNITY_CACHING
            return Caching.currentCacheForWriting.valid;
#else
            return true;
#endif
        }

        public static bool IsEnoughSpaceFree(long needSize)
        {
#if SUPPORT_UNITY_CACHING
            return Caching.currentCacheForWriting.spaceFree >= needSize;
#else
            return true;
#endif
        }
    }
    
}

