using System.Collections;
using System.IO;
using UnityEngine;

namespace TAssetBundle
{
    internal class LocalAssetBundleProvider : AssetBundleProviderBase
    {
        public LocalAssetBundleProvider(IAssetBundleManager manager) : base(manager)
        {
        }

        public override IEnumerator LoadAssetBundle(InnerOperations innerOperations, AssetBundleRuntimeInfo info,
            ReturnValue<AssetBundle> returnAssetBundle)
        {
            var filePath = GetFilePath(info);

#if UNITY_EDITOR
            var request = AssetBundle.LoadFromFileAsync(filePath);

            innerOperations.Add(request);

            yield return request;

            returnAssetBundle.Value = request.assetBundle;
#else
            yield return Manager.LoadAssetBundleFromFile(innerOperations, filePath, info.Encrypt, info.Hash, returnAssetBundle);
#endif
        }

        public override IEnumerator IsCached(AssetBundleRuntimeInfo info, ReturnValue<bool> returnValue)
        {
            yield return Manager.ExistFile(GetFilePath(info), returnValue);
        }

        private string GetFilePath(AssetBundleRuntimeInfo info)
        {
            bool withHash = true;

#if UNITY_EDITOR
            if (!Manager.Settings.build.appendHashFromFileName)
                withHash = false;
#endif
            var relativeFilePath = info.GetName(withHash) + Manager.Settings.build.assetBundleFileExtensions;

            return Path.Combine(Manager.LocalPath, relativeFilePath);
        }
    }

}
