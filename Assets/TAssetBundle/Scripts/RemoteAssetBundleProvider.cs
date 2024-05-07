using System;
using System.Threading.Tasks;

namespace TAssetBundle
{

    internal abstract class RemoteAssetBundleProvider : AssetBundleProviderBase
    {
        protected RemoteAssetBundleProvider(IAssetBundleManager manager) : base(manager)
        {
        }

        protected abstract WebRequestAsync DownloadAsync(InnerOperations innerOperations, string url, AssetBundleRuntimeInfo info);

        protected virtual Task PostProcessDownloadAssetBundle(AssetBundleDownloadInfo.DownloadInfo downloadInfo)
        {
            return Task.CompletedTask;
        }


        public string GetRemoteAssetBundleUrl(AssetBundleRuntimeInfo info)
        {
            var relativePath = info.GetName(Manager.Settings.build.appendHashFromFileName) + Manager.Settings.build.assetBundleFileExtensions;
            return string.Format("{0}/{1}", Manager.RemoteUrl, relativePath);
        }

        public void RequestDownload(InnerOperations innerOperations, 
            AssetBundleDownloadInfo.DownloadInfo downloadInfo,
            Action<AssetBundleDownloadInfo.DownloadInfo> onProgress,
            Action<AssetBundleDownloadInfo.DownloadInfo> onComplete)
        {
            string assetBundleUrl = GetRemoteAssetBundleUrl(downloadInfo.assetBundleInfo);

            if (Manager.EnableDebuggingLog)
            {
                Logger.Log("download assetbundle - " + assetBundleUrl);
            }

            downloadInfo.requestAsync = DownloadAsync(innerOperations, assetBundleUrl, downloadInfo.assetBundleInfo);

            downloadInfo.requestAsync.Command.onProgress = request =>
            {
                downloadInfo.downloadedSize = (long)request.downloadedBytes;
                onProgress?.Invoke(downloadInfo);
            };

            downloadInfo.requestAsync.OnComplete += async (request) =>
            {
                if (request.Request.IsSuccess())
                {
                    await PostProcessDownloadAssetBundle(downloadInfo);

                    downloadInfo.Complete(AssetBundleDownloadInfo.EDownloadState.Success);

                    if (Manager.EnableDebuggingLog)
                    {
                        Logger.Log("complete to download assetbundle - " + assetBundleUrl);
                    }
                }
                else
                {
                    Logger.Warning("fail to download assetbundle - " + assetBundleUrl);
                    downloadInfo.Complete(AssetBundleDownloadInfo.EDownloadState.Failed);
                }

                onComplete?.Invoke(downloadInfo);
            };
        }
    }
}
