using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace TAssetBundle
{

    /// <summary>
    /// 따로 관리되는 원격 에셋번들 제공자
    /// 기본 유니티 에셋 번들 제공자를 사용하지 않을 때 사용된다 (ex: 암호화 에셋번들)
    /// </summary>
    internal class SpecificManagedAssetBundleProvider : RemoteAssetBundleProvider
    {
        private readonly AssetBundleCacheManager _cacheManager;
        public SpecificManagedAssetBundleProvider(IAssetBundleManager manager) : base(manager)
        {
            _cacheManager = new AssetBundleCacheManager();
            _cacheManager.OnRemovedVersion += OnRemovedVersion;
        }

        private string GetFilePath(AssetBundleRuntimeInfo info)
        {
            return Path.Combine(CachingWrapper.GetPath(), GetRelativePath(info));
        }

        private string GetRelativePath(AssetBundleRuntimeInfo info)
        {
            var suffix = info.Encrypt ? "_" : string.Empty;

            return info.GetName(true) + suffix;
        }

        public override IEnumerator LoadAssetBundle(InnerOperations innerOperations, AssetBundleRuntimeInfo info, ReturnValue<AssetBundle> returnValue)
        {
            var filePath = GetFilePath(info);

            if (!File.Exists(filePath))
            {
                yield break;
            }

            yield return Manager.LoadAssetBundleFromFile(innerOperations, filePath, info.Encrypt, info.Hash, returnValue);
        }

        public override IEnumerator IsCached(AssetBundleRuntimeInfo info, ReturnValue<bool> returnValue)
        {
            yield return Manager.ExistFile(GetFilePath(info), returnValue);
        }

        protected override WebRequestAsync DownloadAsync(InnerOperations innerOperations, string url, AssetBundleRuntimeInfo info)
        {
            return Manager.WebRequest.GetAsync(innerOperations, url);
        }

        protected override async Task PostProcessDownloadAssetBundle(AssetBundleDownloadInfo.DownloadInfo downloadInfo)
        {
            var info = downloadInfo.assetBundleInfo;
            await PostProcessAssetBundle(downloadInfo.requestAsync.Request.downloadHandler.data,
                info);
        }

        private async Task PostProcessAssetBundle(byte[] bytes, AssetBundleRuntimeInfo assetBundleInfo)
        {
            if (Manager.EnableDebuggingLog)
            {
                Logger.Log("start post process assetbundle - " + assetBundleInfo.AssetBundleName);
            }

            while (!CachingWrapper.IsValid())
            {
                await Task.Yield();
            }

            //임시파일과 lz4로 재압축 했을때 예상 파일 사이즈
            var needFileSize = assetBundleInfo.Size * 4;

            //따로 관리되는 에셋번들은 유니티 캐싱으로 관리되지 않기 때문에
            //가용 디스크 용량이 부족하다면 사용안하는 이전 버전을 지워준다
            if (!CachingWrapper.IsEnoughSpaceFree(needFileSize))
            {
                _cacheManager.RemoveNotUsedAssetBundleVersions();
            }

            var path = GetFilePath(assetBundleInfo);
            var tempPath = path + "_temp";

            var directoryPath = Path.GetDirectoryName(tempPath);

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            File.WriteAllBytes(tempPath, bytes);

            var recompressRequest = await AssetBundle.RecompressAssetBundleAsync(tempPath, tempPath, BuildCompression.LZ4Runtime, 0, ThreadPriority.High).ToTask();

            if (recompressRequest.result != AssetBundleLoadResult.Success)
            {
                File.Delete(tempPath);
                Logger.Error(string.Format("fail to recompress assetbundle - {0}({1})",
                    recompressRequest.humanReadableResult, recompressRequest.result));
                return;
            }

            bytes = File.ReadAllBytes(tempPath);

            if (assetBundleInfo.Encrypt)
            {
                bytes = Manager.CryptoSerializer.Encrypt(bytes);
            }

            File.WriteAllBytes(path, bytes);
            File.Delete(tempPath);

            _cacheManager.UpdateVersion(assetBundleInfo.AssetBundleName, new AssetBundleCacheVersion
            {
                hash = assetBundleInfo.HashString,
                path = GetRelativePath(assetBundleInfo)
            });

            if (Manager.EnableDebuggingLog)
            {
                Logger.Log("complete post process assetbundle - " + assetBundleInfo.AssetBundleName);
            }
        }

        private void OnRemovedVersion(AssetBundleCacheVersion version)
        {
            var filePath = Path.Combine(CachingWrapper.GetPath(), version.path);

            if (File.Exists(filePath))
            {
                Logger.Log("delete removed version - " + version.path);
                File.Delete(filePath);
            }
        }
    }

}
