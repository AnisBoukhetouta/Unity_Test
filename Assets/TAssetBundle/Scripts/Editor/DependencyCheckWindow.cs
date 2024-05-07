using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    internal class DependencyCheckWindow : EditorWindow
    {
        private const string AssetBundlesFoldoutKey = "DC.AssetBundlesFoldout";
        private const string AssetsFoldoutKey = "DC.AssetsFoldout";

        private readonly Dictionary<string, TAssetBundleManifest> _manifests = new Dictionary<string, TAssetBundleManifest>(StringComparer.Ordinal);
        private readonly Dictionary<string, UnityEngine.Object> _assets = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
        private readonly Dictionary<string, string[]> _assetBundleAssets = new Dictionary<string, string[]>(StringComparer.Ordinal);
        private readonly List<string> _visibleAssets = new List<string>();
        private AssetBundleManifest _manifest;
        private Vector2 _scrollPosition;

                
        public static void OpenWindow()
        {
            var window = GetWindow<DependencyCheckWindow>("Dependency Checker");
            window.Show();
            window.Setup();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Check Dependencies"))
            {
                Setup();
            }

            if (_manifest == null)
                return;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Expand All"))
            {
                SetExpandAll(true);
            }

            if (GUILayout.Button("Shrink All"))
            {
                SetExpandAll(false);
            }
            EditorGUILayout.EndHorizontal();

            if (_visibleAssets.Count > 0)
            {
                if (GUILayout.Button("Clear Assets"))
                {
                    _visibleAssets.Clear();
                }
            }

            DrawAssetBundles();
        }

        private void OnDisable()
        {
            Clear();
        }

        private void OnProjectChange()
        {
            Clear();
        }

        private void Clear()
        {
            _manifests.Clear();
            _assets.Clear();
            _assetBundleAssets.Clear();
            _manifest = null;
        }

        private void Setup()
        {
            Clear();

            var manifests = AssetBundleBuilder.GetAllManifests().ToArray();
            var buildTarget = Util.GetActiveBuildTarget();
            var outputPath = AssetBundleBuilder.GetOutputPath(buildTarget);
            var assetBundleBuilds = AssetBundleBuilder.GetAssetBundleBuilds(manifests).ToArray();

            _manifest = AssetBundleBuilder.DryRunBuild(outputPath, assetBundleBuilds, buildTarget);

            foreach (var assetBundleBuild in assetBundleBuilds)
            {
                _assetBundleAssets[assetBundleBuild.assetBundleName] = assetBundleBuild.assetNames;
            }
        }

        private void SetExpandAll(bool expand)
        {
            foreach (var assetBundleName in _assetBundleAssets.Keys)
            {
                EditorPrefs.SetBool(AssetBundlesFoldoutKey + assetBundleName, expand);
            }
        }

        private void DrawAssetBundles()
        {
            var assetBundles = _manifest.GetAllAssetBundles();

            if (assetBundles.Length == 0)
                return;

            var savedFoldout = EditorPrefs.GetBool(AssetBundlesFoldoutKey, true);

            var foldout = EditorGUILayout.BeginFoldoutHeaderGroup(savedFoldout, "Asset Bundles - Count: " + assetBundles.Length);
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (foldout != savedFoldout)
            {
                EditorPrefs.SetBool(AssetBundlesFoldoutKey, foldout);
            }

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollScope.scrollPosition;

                if (foldout)
                {
                    ++EditorGUI.indentLevel;

                    foreach (var assetBundleName in assetBundles)
                    {
                        DrawAssetBundle(assetBundleName);
                    }

                    --EditorGUI.indentLevel;
                }

                DrawAssets();
            }
        }

        private void DrawAssets()
        {
            if (_visibleAssets.Count == 0)
                return;

            var savedFoldout = EditorPrefs.GetBool(AssetsFoldoutKey, true);

            var foldout = EditorGUILayout.BeginFoldoutHeaderGroup(savedFoldout,
                "Assets - Count: " + _visibleAssets.Count);

            if (foldout != savedFoldout)
            {
                EditorPrefs.SetBool(AssetsFoldoutKey, foldout);
            }

            if (foldout)
            {
                ++EditorGUI.indentLevel;

                foreach (var assetPath in _visibleAssets)
                {
                    DrawAsset(assetPath);
                }

                --EditorGUI.indentLevel;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private bool DrawAssetBundle(string assetBundleName)
        {
            EditorGUILayout.BeginHorizontal();

            if (!_manifests.TryGetValue(assetBundleName, out TAssetBundleManifest manifest))
            {
                manifest = TAssetBundleManifestUtil.GetManifest(assetBundleName);
                _manifests.Add(assetBundleName, manifest);
            }

            EditorGUILayout.ObjectField(manifest, manifest.GetType(), false, GUILayout.MaxWidth(300f));

            if (GUILayout.Button("Assets", GUILayout.Width(60f)))
            {
                SetVisibleAssets(assetBundleName);
            }

            var dependencies = _manifest.GetDirectDependencies(assetBundleName);
            var foldout = false;

            if (dependencies.Length > 0)
            {
                var foldoutKey = AssetBundlesFoldoutKey + assetBundleName;
                var savedFoldout = EditorPrefs.GetBool(foldoutKey, true);

                foldout = EditorGUILayout.Foldout(savedFoldout,
                    string.Format("{0} ({1})", assetBundleName, dependencies.Length), true);

                if (foldout != savedFoldout)
                {
                    EditorPrefs.SetBool(foldoutKey, foldout);
                }
            }
            else
            {
                EditorGUILayout.LabelField(assetBundleName);
            }

            EditorGUILayout.EndHorizontal();

            if (foldout)
            {
                ++EditorGUI.indentLevel;

                foreach (var dependent in dependencies)
                {
                    DrawAssetBundle(dependent);
                }

                --EditorGUI.indentLevel;
            }

            return foldout;
        }

        private void DrawAsset(string assetPath)
        {
            if (!_assets.TryGetValue(assetPath, out UnityEngine.Object asset))
            {
                asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                _assets.Add(assetPath, asset);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(asset, asset.GetType(), false, GUILayout.Width(300f));
            EditorGUILayout.LabelField(assetPath);
            EditorGUILayout.EndHorizontal();
        }

        private void SetVisibleAssets(string assetBundleName)
        {
            _visibleAssets.Clear();
            if (_assetBundleAssets.TryGetValue(assetBundleName, out string[] assetPaths))
            {
                _visibleAssets.AddRange(assetPaths);
            }
        }
    }

}
