using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    internal static class EditorMenu
    {
        [MenuItem("TAssetBundle/Settings", priority = 10)]
        public static void ShowSettingFile()
        {
            Selection.activeObject = AssetBundleBuilder.Settings;
        }

        [MenuItem("TAssetBundle/Clear Build Cache", priority = 11)]
        public static void ClearCache()
        {
            if (EditorUtil.DisplayDialogOkCancel("Clear Build Cache - " + Util.GetActiveBuildTarget()))
            {
                Cache.BuildCache.ClearBuildCache(Util.GetActiveBuildTarget());
            }
        }

        [MenuItem("TAssetBundle/Build Asset Bundle", priority = 12)]
        public static void BuildAssetBundle()
        {
            if (EditorUtil.DisplayDialogOkCancel("Build Asset Bundle - " + Util.GetActiveBuildTarget()))
            {
                AssetBundleBuilder.BuildAssetBundle();
            }
        }

        [MenuItem("TAssetBundle/Run All Composition Strategy", priority = 13)]
        public static void RunAllCompositionStrategy()
        {
            if (EditorUtil.DisplayDialogOkCancel("Run All Composition Strategy"))
            {
                AssetBundleBuilder.RunAllCompositionStrategy();
            }
        }

        [MenuItem("TAssetBundle/Clear All Asset Bundle Build Infos (Composition Strategy Only)", priority = 14)]
        public static void ClearAllAssetBundleBuildInfosOnlyCompositionStrategy()
        {
            if (EditorUtil.DisplayDialogOkCancel("Clear All Asset Bundle Build Infos - " + Util.GetActiveBuildTarget()))
            {
                AssetBundleBuilder.ClearAllAssetBundleBuildInfos();
            }
        }

        /// <summary>
        /// Open the build output folder in the editor
        /// </summary>
        [MenuItem("TAssetBundle/Open Build Folder", priority = 15)]
        public static void OpenBuildFolder()
        {
            EditorUtility.RevealInFinder(AssetBundleBuilder.GetOutputPath(Util.GetActiveBuildTarget()));
        }

        /// <summary>
        /// Open the downloaded cached assets folder in the editor 
        /// </summary>
        [MenuItem("TAssetBundle/Open Cached Assets Folder", priority = 16)]
        public static void OpenCachedAssetsFolder()
        {
            EditorUtility.RevealInFinder(Caching.currentCacheForWriting.path);
        }

        /// <summary>
        /// delete downloaded cached assets
        /// </summary>
        [MenuItem("TAssetBundle/Clear Cached Assets", priority = 17)]
        public static void ClearCachedAssets()
        {
            Util.ClearCachedAssets();
        }

        /// <summary>
        /// delete downloaded cached catalogs
        /// </summary>
        [MenuItem("TAssetBundle/Clear Cached Catalog", priority = 18)]
        public static void ClearCachedCatalog()
        {
            Util.ClearCachedRemoteCatalogs();
        }

        [MenuItem("TAssetBundle/TAssetBundle Browser", priority = 50)]
        public static void TAssetBundleBrowser()
        {
            TAssetBundleBrowserWindow.OpenWindow();
        }

        [MenuItem("TAssetBundle/Tag Editor", priority = 51)]
        public static void TagEditor()
        {
            TAssetBundleTagEditorWindow.OpenWindow();
        }

        [MenuItem("TAssetBundle/Dependency Checker", priority = 52)]
        public static void DependencyChecker()
        {
            DependencyCheckWindow.OpenWindow();
        }        

        [MenuItem("TAssetBundle/Web Server Test", priority = 53)]
        public static void WebServerTest()
        {
            TAssetBundleWebServerWindow.OpenWindow();
        }

        [MenuItem("TAssetBundle/Asset Reference Tracker", priority = 100)]
        public static void AssetReferenceTracker()
        {
            AssetReferenceTrackWindow.OpenWindow();
        }


        [MenuItem("TAssetBundle/Help/Manual", priority = 200)]
        public static void HelpManual()
        {
            Application.OpenURL("https://tigu77.github.io/TAssetBundle-api-doc/manual/manual.html");
        }

        [MenuItem("TAssetBundle/Help/Api Documents", priority = 201)]
        public static void HelpApiDocuments()
        {
            Application.OpenURL("https://tigu77.github.io/TAssetBundle-api-doc/api/index.html");
        }

        [MenuItem("TAssetBundle/Help/Video Tutorial", priority = 202)]
        public static void HelpVideoTutorial()
        {
            Application.OpenURL("https://www.youtube.com/playlist?list=PLB3Wee-5ukiFD7RUFiFaxbp8OQ8PTbJ0d");
        }


        [MenuItem("TAssetBundle/Help/Forum", priority = 203)]
        public static void HelpForum()
        {
            Application.OpenURL("https://forum.unity.com/threads/released-tassetbundle-powerful-asset-bundle-integrated-management-system.1441315/");
        }

        [MenuItem("TAssetBundle/Help/Release Notes", priority = 204)]
        public static void HelpReleaseNotes()
        {            
            Application.OpenURL("https://assetstore.unity.com/packages/tools/utilities/tassetbundle-221122#releases");
        }
    }

}
