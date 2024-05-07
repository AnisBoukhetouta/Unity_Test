using System.Collections;
using UnityEngine;

namespace TAssetBundle
{
    /// <summary>
    /// 에셋 번들 제공자 인터페이스
    /// </summary>
    internal interface IAssetBundleProvider
    {
        IEnumerator IsCached(AssetBundleRuntimeInfo info, ReturnValue<bool> returnValue);
        IEnumerator LoadAssetBundle(InnerOperations innerOperations, AssetBundleRuntimeInfo info, ReturnValue<AssetBundle> returnValue);        
    }
}
