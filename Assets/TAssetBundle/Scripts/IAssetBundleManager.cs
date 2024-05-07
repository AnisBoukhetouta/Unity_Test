using System.Collections;
using UnityEngine;

namespace TAssetBundle
{
    /// <summary>
    /// 에셋 번들 매니저 인터페이스
    /// </summary>
    internal interface IAssetBundleManager
    {
        bool EnableDebuggingLog { get; }
        Settings Settings { get; }
        WebRequest WebRequest { get; }
        CryptoSerializer CryptoSerializer { get; }
        ITagComparer TagComparer { get; }
        string LocalPath { get; }
        string RemoteUrl { get; }
        IEnumerator ExistFile(string filePath, ReturnValue<bool> returnValue);
        IEnumerator LoadAssetBundleFromFile(InnerOperations innerOperations, string filePath, bool encrypt, Hash128 hash, ReturnValue<AssetBundle> returnValue);
    }
}