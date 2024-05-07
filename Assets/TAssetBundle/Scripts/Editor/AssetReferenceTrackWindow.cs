using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace TAssetBundle.Editor
{
    internal class AssetReferenceTrackWindow : EditorWindow
    {
        private const string UpdateIntervalKey = "ARTW.UpdateInterval";
        private const string AssetBundlesFoldoutKey = "ARTW.ActiveAssetBundlesFoldout";
        private const string AssetsFoldoutKey = "ARTW.AssetsFoldout";

        private const float DefaultUpdateInterval = 0.5f;

        private float _updateInterval;
        private Vector2 _scrollPosition;
        private double _lastRefreshedTime = 0;

        private readonly Dictionary<string, TAssetBundleManifest> _assetBundleManifests = new Dictionary<string, TAssetBundleManifest>(StringComparer.Ordinal);

        
        public static void OpenWindow()
        {
            var window = GetWindow<AssetReferenceTrackWindow>("Asset Reference Tracker");
            window.Show();
        }


        private void OnEnable()
        {
            EditorApplication.update += OnUpdate;
            EditorApplication.projectChanged += OnProjectChanged;
            _updateInterval = EditorPrefs.GetFloat(UpdateIntervalKey, DefaultUpdateInterval);
            Refresh();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void Refresh()
        {
            _lastRefreshedTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private void OnGUI()
        {
            var updateInterval = EditorGUILayout.FloatField("Update Interval (Seconds)", _updateInterval);

            if (updateInterval != _updateInterval)
            {
                _updateInterval = updateInterval;
                EditorPrefs.SetFloat(UpdateIntervalKey, updateInterval);
            }

            if (!Application.isPlaying)
            {
                EditorGUIUtil.LabelFieldColor("Not Playing", Color.red);
                return;
            }

            EditorGUILayout.LabelField("Play Mode - " + AssetBundleBuilder.Settings.editorPlayMode);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Expand All"))
            {
                SetExpandAll(true);
            }

            if (GUILayout.Button("Shrink All"))
            {
                SetExpandAll(false);
            }

            GUILayout.EndHorizontal();

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollScope.scrollPosition;

                DrawActiveAssetBundles(AssetManager.GetActiveAssetBundles());
                DrawAssets(AssetManager.GetLoadedAssetHandles());
            }
        }

        private void OnProjectChanged()
        {
            _assetBundleManifests.Clear();
            Refresh();
        }

        private void OnUpdate()
        {
            var currentTime = EditorApplication.timeSinceStartup;

            var elapsed = currentTime - _lastRefreshedTime;

            if (elapsed < DefaultUpdateInterval)
            {
                return;
            }

            Refresh();
        }

        private void SetExpandAll(bool expand)
        {
            foreach (var assetBundle in AssetManager.GetActiveAssetBundles())
            {
                var foldoutKey = AssetBundlesFoldoutKey + assetBundle.AssetBundleInfo.AssetBundleName;

                EditorPrefs.SetBool(foldoutKey, expand);
            }

            foreach (var assetHandle in AssetManager.GetLoadedAssetHandles())
            {
                var foldoutKey = AssetsFoldoutKey + assetHandle.Info.AssetPath;

                EditorPrefs.SetBool(foldoutKey, expand);
            }
        }

        private void DrawActiveAssetBundles(IEnumerable<IActiveAssetBundleInfo> activeAssetBundles)
        {
            if (!activeAssetBundles.Any())
                return;

            var savedFoldout = EditorPrefs.GetBool(AssetBundlesFoldoutKey, true);
            var foldout = EditorGUILayout.BeginFoldoutHeaderGroup(savedFoldout, "Active Asset Bundles - Count: " + activeAssetBundles.Count());

            if (savedFoldout != foldout)
            {
                EditorPrefs.SetBool(AssetBundlesFoldoutKey, foldout);
            }

            if (foldout)
            {
                ++EditorGUI.indentLevel;

                foreach (var activeAssetBundle in activeAssetBundles)
                {
                    DrawActiveAssetBundle(activeAssetBundle);
                }

                --EditorGUI.indentLevel;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(10f);
        }


        private void DrawAssets(IEnumerable<IAssetHandle> assetHandles)
        {
            if (!assetHandles.Any())
                return;

            var savedFoldout = EditorPrefs.GetBool(AssetsFoldoutKey, true);
            var foldout = EditorGUILayout.BeginFoldoutHeaderGroup(savedFoldout, "Assets - Count: " + assetHandles.Count());

            if (savedFoldout != foldout)
            {
                EditorPrefs.SetBool(AssetsFoldoutKey, foldout);
            }

            if (foldout)
            {
                ++EditorGUI.indentLevel;

                foreach (var assetHandle in assetHandles)
                {
                    DrawAsset(assetHandle);
                }

                --EditorGUI.indentLevel;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(10f);
        }

        private void DrawActiveAssetBundle(IActiveAssetBundleInfo activeAssetBundle)
        {
            EditorGUILayout.BeginHorizontal();

            var assetBundleFileName = activeAssetBundle.AssetBundleInfo.AssetBundleName;
            var content = string.Format("[{0}] - {1}", activeAssetBundle.ReferenceCount, assetBundleFileName);
            var foldout = false;

            var manifest = GetManifest(activeAssetBundle.AssetBundleInfo.AssetBundleName);
            EditorGUILayout.ObjectField(manifest, manifest.GetType(), false, GUILayout.MaxWidth(300f));

            if (activeAssetBundle.Dependencies.Length > 0)
            {
                var foldoutKey = AssetBundlesFoldoutKey + activeAssetBundle.AssetBundleInfo.AssetBundleName;
                var savedFoldout = EditorPrefs.GetBool(foldoutKey, false);
                foldout = EditorGUILayout.Foldout(savedFoldout, content, true);

                if (foldout != savedFoldout)
                {
                    EditorPrefs.SetBool(foldoutKey, foldout);
                }
            }
            else
            {
                EditorGUILayout.LabelField(content);
            }

            EditorGUILayout.EndHorizontal();

            if (foldout)
            {
                ++EditorGUI.indentLevel;

                foreach (var dependent in activeAssetBundle.Dependencies)
                {
                    DrawActiveAssetBundle(dependent);
                }

                --EditorGUI.indentLevel;
            }
        }

        private void DrawAsset(IAssetHandle assetHandle)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(assetHandle.Info.Asset, assetHandle.Info.Asset.GetType(), false,
                GUILayout.Width(300f));

            var content = string.Format("[{0}] - {1}", assetHandle.Info.ReferenceCount, assetHandle.Info.AssetPath);
            var foldout = false;

            if (assetHandle.Info.ActiveAssetBundle != null)
            {
                if (GUILayout.Button("Select", GUILayout.Width(50f)))
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(assetHandle.Info.AssetPath);
                    EditorGUIUtility.PingObject(asset);
                    Selection.activeObject = asset;
                }

                var foldoutKey = AssetsFoldoutKey + assetHandle.Info.AssetPath;
                var savedFoldout = EditorPrefs.GetBool(foldoutKey, false);
                foldout = EditorGUILayout.Foldout(savedFoldout, content, true);

                if (foldout != savedFoldout)
                {
                    EditorPrefs.SetBool(foldoutKey, foldout);
                }
            }
            else
            {
                EditorGUILayout.LabelField(content);
            }

            EditorGUILayout.EndHorizontal();

            if (foldout)
            {
                ++EditorGUI.indentLevel;
                DrawActiveAssetBundle(assetHandle.Info.ActiveAssetBundle);
                --EditorGUI.indentLevel;
            }
        }

        private TAssetBundleManifest GetManifest(string assetBundleName)
        {
            if (!_assetBundleManifests.TryGetValue(assetBundleName, out TAssetBundleManifest manifest))
            {
                manifest = TAssetBundleManifestUtil.GetManifest(assetBundleName);
                _assetBundleManifests.Add(assetBundleName, manifest);
            }

            return manifest;
        }

    }

}
