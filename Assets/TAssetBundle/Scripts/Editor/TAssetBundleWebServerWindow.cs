using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    internal class TAssetBundleWebServerWindow : EditorWindow
    {
        public static void OpenWindow()
        {
            if (HttpListener.IsSupported)
            {
                var window = GetWindow<TAssetBundleWebServerWindow>();
                window.titleContent = new GUIContent("Web Server Test");
                window.Show();
            }
            else
            {
                EditorUtility.DisplayDialog("TAssetBundle", "Not Supported Web Server", "Ok");
            }
        }

        private void OnGUI()
        {
            var active = TAssetBundleWebServer.IsActive();
            var newActive = EditorGUILayout.Toggle("Active", active);

            if (active != newActive)
            {
                TAssetBundleWebServer.SetActive(newActive);
            }

            var port = TAssetBundleWebServer.GetPort();
            var newPort = EditorGUILayout.IntField(new GUIContent("Port"), port);

            if (port != newPort)
            {
                TAssetBundleWebServer.SetPort(newPort);
            }

            var rootFolder = TAssetBundleWebServer.GetRootFolder();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Root Folder", rootFolder);

            if (GUILayout.Button("Select"))
            {
                var projectPath = EditorUtil.GetProjectPath();
                var folderPath = projectPath + rootFolder;

                if (!Directory.Exists(folderPath))
                    folderPath = projectPath;

                var newRootFolder = EditorUtility.OpenFolderPanel("Server Data Folder", folderPath, string.Empty);

                if (string.IsNullOrEmpty(newRootFolder))
                {
                }
                else if (!newRootFolder.StartsWith(projectPath))
                {
                    Logger.Warning("invalid root folder path - " + newRootFolder);
                }
                else
                {
                    newRootFolder = newRootFolder.Substring(projectPath.Length);

                    if (rootFolder != newRootFolder)
                    {
                        TAssetBundleWebServer.SetRootFolder(newRootFolder);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (TAssetBundleWebServer.IsActive())
            {
                string url = TAssetBundleWebServer.GetPrefix() + Util.GetActiveBuildTarget();
                EditorGUILayout.LabelField("URL");
                EditorGUILayout.SelectableLabel(url);

                var assetRootPath = Path.Combine(TAssetBundleWebServer.GetRootPath(), Util.GetActiveBuildTarget().ToString());

                if (!Directory.Exists(assetRootPath))
                {
                    EditorGUIUtil.LabelFieldColor("Need to build the Asset Bundle", Color.red);
                }
                else if (AssetBundleBuilder.Settings.editorPlayMode == EEditorPlayMode.EditorAsset)
                {
                    EditorGUIUtil.LabelFieldColor("Editor Asset Mode", Color.red);
                }
            }
        }
    }

}
