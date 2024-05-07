using UnityEngine;

namespace TAssetBundle
{
    internal class SceneAssetBundleUnloader : MonoBehaviour
    {
        private AssetBundleManager _manager;
        private string _assetBundleName;

        private bool _isApplicationQuit = false;

        public void Setting(AssetBundleManager manager, string assetBundleName)
        {
            _manager = manager;
            _assetBundleName = assetBundleName;

            if (_manager.EnableDebuggingLog)
            {
                Logger.Log("start SceneAssetBundleUnloader - " + assetBundleName);
            }
        }

        private void OnDestroy()
        {
            if (!_isApplicationQuit)
            {
                if (_manager.EnableDebuggingLog)
                {
                    Logger.Log("finish SceneAssetBundleUnloader - " + _assetBundleName);
                }

                _manager.UnloadAssetBundle(_assetBundleName, true);
            }
        }

        private void OnApplicationQuit()
        {
            _isApplicationQuit = true;
        }
    }
}

