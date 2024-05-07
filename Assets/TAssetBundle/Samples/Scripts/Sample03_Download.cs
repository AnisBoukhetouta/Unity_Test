using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace TAssetBundle.Samples
{
    public class Sample03_Download : MonoBehaviour
    {
        public Text text;
        public string remoteDownloadUrl; //your remote storage address

        public GameObjectAssetRef needDownloadAsset;
        public SceneAssetRef testScene;

        private void Awake()
        {
            //[BuildTarget] in url will be changed to your BuildTarget
            //[AppVersion] in url will be changed to your Application.version
            AssetManager.SetRemoteUrl(remoteDownloadUrl);
            AssetManager.SetWebRequestBeforeSendCallback(OnWebRequestBeforeSend);
            AssetManager.SetWebRequestResultCallback(OnWebRequestResult);
        }


        private void OnGUI()
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("Check Catalog Update"))
            {
                var checkCatalogUpdateAsync = AssetManager.CheckCatalogUpdateAsync();

                checkCatalogUpdateAsync.OnComplete += (result) =>
                {
                    text.text = "need catalog update - " + result;
                };
            }

            if (GUILayout.Button("Update Catalog"))
            {
                var updateCatalogAsync = AssetManager.UpdateCatalogAsync();

                updateCatalogAsync.OnComplete += (result) =>
                {
                    text.text = "update catalog - " + result;
                };
            }

            if (GUILayout.Button("Get Download Size All"))
            {
                var downloadSizeAsync = AssetManager.GetDownloadSizeAsync();

                downloadSizeAsync.OnComplete += size =>
                {
                    text.text = "download size - " + FileSizeFormmater.FormatSize(size);
                };
            }

            if (GUILayout.Button("Download All"))
            {
                var downloadAsync = AssetManager.DownloadAsync();
                downloadAsync.OnProgress += OnDownloadProgress;
                downloadAsync.OnComplete += OnDownloadComplete;
            }

            if (GUILayout.Button("Get Download Size By Tags"))
            {
                var downloadSizeAsync = AssetManager.GetDownloadSizeByTagsAsync(new string[] { "character" });

                downloadSizeAsync.OnComplete += size =>
                {
                    text.text = "download size - " + FileSizeFormmater.FormatSize(size);
                };
            }

            if (GUILayout.Button("Download By Tags"))
            {
                var downloadAsync = AssetManager.DownloadByTagsAsync(new string[] { "character" });
                downloadAsync.OnProgress += OnDownloadProgress;
                downloadAsync.OnComplete += OnDownloadComplete;
            }

            if (GUILayout.Button("Get Download Size By Assets"))
            {
                var downloadSizeAsync = AssetManager.GetDownloadSizeByAssetsAsync(new AssetRef[] { needDownloadAsset });

                downloadSizeAsync.OnComplete += size =>
                {
                    text.text = "download size - " + FileSizeFormmater.FormatSize(size);
                };
            }

            if (GUILayout.Button("Download By Assets"))
            {
                var downloadAsync = AssetManager.DownloadByAssetsAsync(new AssetRef[] { needDownloadAsset });
                downloadAsync.OnProgress += OnDownloadProgress;
                downloadAsync.OnComplete += OnDownloadComplete;
            }

            if (GUILayout.Button("Load TestScene"))
            {
                AssetManager.LoadSceneAsync(testScene);
            }

            if (GUILayout.Button("Clear Cached Asset Bundles"))
            {
                Util.ClearCachedAssets();
            }

            GUILayout.EndVertical();
        }

        private void OnDownloadProgress(AssetBundleDownloadInfo downloadInfo)
        {
            double value = downloadInfo.DownloadedSize / (double)downloadInfo.TotalDownloadSize;

            text.text = string.Format("downloading - {0:0.00}%, {1}/{2}",
                value * 100,
                FileSizeFormmater.FormatSize(downloadInfo.DownloadedSize),
                FileSizeFormmater.FormatSize(downloadInfo.TotalDownloadSize));
        }

        private void OnDownloadComplete(AssetBundleDownloadInfo downloadInfo)
        {
            if (downloadInfo.TotalDownloadSize == 0)
            {
                text.text = "don't need to download";
            }
            else if (downloadInfo.IsDownloadComplete())
            {
                text.text = "download complete";
            }
            else
            {
                text.text = "download fail";
            }
        }

        private void OnWebRequestBeforeSend(UnityWebRequest webRequest, WebRequestCommand command)
        {
            //Set additional options specific to UnityWebRequet
            Logger.Log($"before send [{webRequest.method}] - {webRequest.url}");
        }

        private void OnWebRequestResult(UnityWebRequest webRequest, WebRequestCommand command)
        {
            if (webRequest.IsSuccess())
            {
            }
            else
            {
                var error = webRequest.GetError(); //equal to AssetManager.GetLastWebRequestError()

                Logger.Error($"error - code:{error.responseCode}, message:{error.message}");
            }
        }
    }
}