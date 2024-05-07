using System.Collections.Generic;
using System.Linq;


namespace TAssetBundle
{
    /// <summary>
    /// ReferenceCount
    /// </summary>
    public interface IReferenceCount
    {
        int ReferenceCount { get; }
    }


    /// <summary>
    /// Active Asset Bundle Information
    /// </summary>
    public interface IActiveAssetBundleInfo : IReferenceCount
    {
        AssetBundleRuntimeInfo AssetBundleInfo { get; }
        IActiveAssetBundleInfo[] Dependencies { get; }
    }

    /// <summary>
    /// Asset Information
    /// </summary>
    public interface IAssetInfo : IReferenceCount
    {
        string AssetPath { get; }
        UnityEngine.Object Asset { get; }
        IActiveAssetBundleInfo ActiveAssetBundle { get; }
    }

    /// <summary>
    /// Asset Handle
    /// </summary>
    public interface IAssetHandle
    {
        bool IsLoaded { get; }
        IAssetInfo Info { get; }
    }

    public interface IAssetHandle<T> : IAssetHandle where T : UnityEngine.Object
    {
        T Get();
    }

    /// <summary>
    /// Scene Loading Information
    /// </summary>
    public struct LoadSceneInfo
    {
        public delegate bool AllowSceneActivationCallback(float progress);

        public string sceneNameOrPath;
        public UnityEngine.SceneManagement.LoadSceneMode loadSceneMode;
        public AllowSceneActivationCallback allowSceneActivation;
    }


    /// <summary>
    /// AssetBundle download information
    /// </summary>
    public class AssetBundleDownloadInfo
    {
        /// <summary>
        /// Download State
        /// </summary>
        public enum EDownloadState
        {
            Progress,
            Success,
            Failed,
        }

        /// <summary>
        /// One AssetBundle download information
        /// </summary>
        public class DownloadInfo
        {
            public AssetBundleRuntimeInfo assetBundleInfo;
            public long downloadedSize;
            public WebRequestAsync requestAsync;
            public EDownloadState state = EDownloadState.Progress;

            public bool IsDownloadComplete => state == EDownloadState.Success && downloadedSize == assetBundleInfo.Size;

            public void Complete(EDownloadState state)
            {
                this.state = state;
                requestAsync = null;
            }
        }

        public long DownloadedSize => downloads.Sum(info => info.downloadedSize);
        public long TotalDownloadSize => downloads.Sum(info => info.assetBundleInfo.Size);

        public bool IsDownloadComplete()
        {
            return downloads.Count(info => info.IsDownloadComplete) == downloads.Count;
        }

        public readonly List<DownloadInfo> downloads = new List<DownloadInfo>();
    }
}