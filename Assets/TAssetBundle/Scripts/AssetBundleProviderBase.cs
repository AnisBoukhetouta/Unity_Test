using System.Collections;
using UnityEngine;

namespace TAssetBundle
{
    /// <summary>
    /// 에셋 번들 제공자 기본 클래스
    /// </summary>
    internal abstract class AssetBundleProviderBase : IAssetBundleProvider
    {
        protected IAssetBundleManager Manager { get; private set; }

        protected AssetBundleProviderBase(IAssetBundleManager manager)
        {
            Manager = manager;
        }

        public abstract IEnumerator IsCached(AssetBundleRuntimeInfo info, ReturnValue<bool> returnValue);
        public abstract IEnumerator LoadAssetBundle(InnerOperations innerOperations, AssetBundleRuntimeInfo info, ReturnValue<AssetBundle> returnValue);
        
    }

}
