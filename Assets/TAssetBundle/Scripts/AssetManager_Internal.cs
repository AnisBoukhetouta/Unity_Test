using System;
using UnityEngine;

namespace TAssetBundle
{
    public static partial class AssetManager
    {
        #region INTERNAL
        private static Settings _settings;
        private static IAssetProvider _assetProvider;


#if !UNITY_EDITOR
        static AssetManager()
        {
            Initialize();
        }
#endif

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#endif
        static void Initialize()
        {
            _settings = Resources.Load<Settings>(Defines.SettingFileName);

            if (_settings == null)
            {
                throw new InvalidOperationException("not exist setting file - " + Defines.SettingFileName);
            }

            if (_settings.enableDebuggingLog)
            {
                Logger.Log("version - " + Defines.Version);
                Logger.Log("buildTarget - " + _settings.GetBuildTarget());
            }

#if UNITY_EDITOR
            if (_settings.editorPlayMode == EEditorPlayMode.EditorAsset)
            {
                _assetProvider = new EditorAssetProvider(_settings);
            }
            else
            {
                _assetProvider = new AssetBundleAssetProvider(_settings);
            }

            if (_settings.enableDebuggingLog)
            {
                Logger.Log("Editor Play Mode - " + _settings.editorPlayMode);
            }
#else
            _assetProvider = new AssetBundleAssetProvider(_settings);
#endif
        }
        #endregion
    }

}
