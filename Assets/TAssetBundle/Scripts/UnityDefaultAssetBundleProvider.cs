using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace TAssetBundle
{
    /// <summary>
    /// 유니티 기본 에셋 번들 제공자
    /// </summary>
    internal class UnityDefaultAssetBundleProvider : RemoteAssetBundleProvider
    {
        public UnityDefaultAssetBundleProvider(IAssetBundleManager manager) : base(manager)
        {
        }

        public override IEnumerator IsCached(AssetBundleRuntimeInfo info, ReturnValue<bool> returnValue)
        {
            if (CachingWrapper.IsSupport())
            {
                returnValue.Value = CachingWrapper.IsVersionCached(info.GetName(Manager.Settings.build.appendHashFromFileName), info.Hash);
            }
            else
            {
                yield return null;
#if UNITY_2022_1_OR_NEWER                
                //2022년 이후 유니티가 내부적으로 사용하는 WebGL cache db에 접근하는 방법을 아직 모르겠다
                returnValue.Value = true;
#else                
                returnValue.Value = File.Exists(string.Format("{0}/UnityCache/Shared/{1}/{2}/__info",
                    Application.persistentDataPath,
                    Path.GetFileNameWithoutExtension(info.GetName(Manager.Settings.build.appendHashFromFileName)),
                    info.HashString));
#endif
            }
        }

        public override IEnumerator LoadAssetBundle(InnerOperations innerOperations, AssetBundleRuntimeInfo info, ReturnValue<AssetBundle> returnValue)
        {
            var assetBundleUrl = GetRemoteAssetBundleUrl(info);
            var assetBundleAsync = Manager.WebRequest.GetAssetBundleAsync(innerOperations, assetBundleUrl, info.Hash);

            yield return assetBundleAsync;

            if (assetBundleAsync.Request.IsSuccess())
            {
                returnValue.Value = DownloadHandlerAssetBundle.GetContent(assetBundleAsync.Request);
            }
        }

        protected override WebRequestAsync DownloadAsync(InnerOperations innerOperations, string assetBundleUrl, AssetBundleRuntimeInfo info)
        {
            return Manager.WebRequest.GetAssetBundleAsync(innerOperations, assetBundleUrl, info.Hash);
        }
    }

}
