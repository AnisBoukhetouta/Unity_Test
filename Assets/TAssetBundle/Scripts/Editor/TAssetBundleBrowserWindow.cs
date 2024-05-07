using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    internal class TAssetBundleBrowserWindow : EditorWindow
    {
        private const float ObjectFieldWidth = 300f;


        private class CachedInfo
        {
            public UnityEngine.Object[] notIncludedAssets;
        }

        private readonly List<TAssetBundleManifest> _manifests = new List<TAssetBundleManifest>();
        private readonly Dictionary<TAssetBundleManifest, CachedInfo> _cachedInfos = new Dictionary<TAssetBundleManifest, CachedInfo>();
        private Vector2 _scrollPosition;
        private bool _showIncludedAssets;
        private bool _showNotIncludedAssets;
        private bool _showIgnoreAssets;

        
        public static TAssetBundleBrowserWindow OpenWindow()
        {
            var window = GetWindow<TAssetBundleBrowserWindow>("TAssetBundle Browser");
            window.Show();
            return window;
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged += Refresh;
            Setup();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= Refresh;
            Clear();
        }

        private void Setup()
        {
            _manifests.Clear();
            _manifests.AddRange(AssetBundleBuilder.GetAllManifests());

            foreach (var manifest in _manifests)
            {
                manifest.OnChanged += OnChangedManifest;
            }
        }

        private void Clear()
        {
            foreach (var manifest in _manifests)
            {
                manifest.OnChanged -= OnChangedManifest;
            }

            _manifests.Clear();
            _cachedInfos.Clear();
        }

        private void Refresh()
        {
            Clear();
            Setup();
        }

        private void OnGUI()
        {
            _showIncludedAssets = DrawToggle("TABB.ShowIncludedAssets", "Show Included Assets");
            _showNotIncludedAssets = DrawToggle("TABB.ShowNotIncludedAssets", "Show Not Included Assets");
            _showIgnoreAssets = DrawToggle("TABB.ShowIgnoreAssets", "Show Ignore Assets");

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Expand All"))
            {
                SetExpandAll(true);
            }

            if (GUILayout.Button("Shrink All"))
            {
                SetExpandAll(false);
            }

            if (GUILayout.Button("Refresh"))
            {
                Refresh();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear All Asset Bundle Build Infos"))
            {
                AssetBundleBuilder.ClearAllAssetBundleBuildInfos();
                Refresh();
            }

            if (GUILayout.Button("Run All Composition Stretegy"))
            {
                AssetBundleBuilder.RunAllCompositionStrategy();
                Refresh();
            }

            if (GUILayout.Button("Build Asset Bundle"))
            {
                EditorApplication.delayCall += EditorMenu.BuildAssetBundle;
            }

            if (GUILayout.Button("Check Dependencies"))
            {
                DependencyCheckWindow.OpenWindow();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(string.Format("Total Asset Bundle Build [{0}]", 
                _manifests.Sum(manifest => manifest.assetBundleBuildInfos.Count)));

            DrawManifests();
        }

        private bool DrawToggle(string key, string label)
        {
            var value = EditorPrefs.GetBool(key, true);
            var newValue = EditorGUILayout.Toggle(label, value);

            if (value != newValue)
            {
                value = newValue;
                EditorPrefs.SetBool(key, value);
            }

            return value;
        }

        private void DrawManifests()
        {
            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollViewScope.scrollPosition;

                foreach (var manifest in _manifests)
                {
                    DrawManifest(manifest);
                    EditorGUILayout.Space(10);
                }
            }
        }

        private void SetExpandAll(bool expand)
        {
            foreach (var manifest in _manifests)
            {
                EditorPrefs.SetBool(GetFoldoutKey(manifest), expand);
            }
        }

        private string GetFoldoutKey(TAssetBundleManifest manifest)
        {
            return "TAssetBundle.AssetBrowser.Foldout." + manifest.ManifestPath;
        }

        private void DrawManifest(TAssetBundleManifest manifest)
        {
            var foldoutKey = GetFoldoutKey(manifest);
            var foldout = EditorPrefs.GetBool(foldoutKey, true);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(manifest, manifest.GetType(), false, GUILayout.Width(ObjectFieldWidth));

            var header = manifest.ManifestPath;

            if (manifest.tag.tags != null && manifest.tag.tags.Length > 0)
            {
                header += string.Format(" - [{0}]", string.Join(", ", manifest.tag.tags));
            }

            var newFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, header);
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndHorizontal();
            if (foldout != newFoldout)
            {
                foldout = newFoldout;
                EditorPrefs.SetBool(foldoutKey, foldout);
            }

            EditorGUI.indentLevel++;

            if (foldout)
            {
                if (manifest.compositionStrategyInfos.Count > 0)
                {
                    if (_showIgnoreAssets)
                    {
                        DrawIgnoreAssets(manifest);
                    }

                    if (_showNotIncludedAssets)
                    {
                        DrawNotIncludedAssets(manifest);
                    }
                }

                if (_showIncludedAssets)
                {
                    DrawIncludedAssets(manifest);
                }

            }

            EditorGUI.indentLevel--;
        }

        private void DrawIncludedAssets(TAssetBundleManifest manifest)
        {   
            if (manifest.assetBundleBuildInfos.Count == 0)
                return;


            EditorGUILayout.LabelField($"Asset Bundle Build [{manifest.assetBundleBuildInfos.Count}]");
            ++EditorGUI.indentLevel;
            foreach (var info in manifest.assetBundleBuildInfos)
            {
                DrawAssetBundleBuildInfo(manifest, info);
            }
            --EditorGUI.indentLevel;
        }

        private void DrawAssetBundleBuildInfo(TAssetBundleManifest manifest, AssetBundleBuildInfo info)
        {
            EditorGUILayout.LabelField($"[{info.buildName}]");

            EditorGUI.indentLevel++;
            foreach (var asset in info.objects)
            {   
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(asset, asset.GetType(), false, GUILayout.Width(ObjectFieldWidth));
                EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(asset));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        private void DrawIgnoreAssets(TAssetBundleManifest manifest)
        {
            if (manifest.ignoreAssets.Count == 0)
                return;

            EditorGUILayout.LabelField($"Ignore Assets [{manifest.ignoreAssets.Count}]");

            EditorGUI.indentLevel++;
            foreach (var asset in manifest.ignoreAssets.Where(asset => asset != null))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(asset, asset.GetType(), false, GUILayout.Width(ObjectFieldWidth));
                EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(asset));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        private void DrawNotIncludedAssets(TAssetBundleManifest manifest)
        {            
            if (!_cachedInfos.TryGetValue(manifest, out CachedInfo cachedInfo))
            {
                cachedInfo = new CachedInfo
                {
                    notIncludedAssets = manifest.GetNotIncludedAssets()
                };

                _cachedInfos.Add(manifest, cachedInfo);
            }

            if (cachedInfo.notIncludedAssets.Length == 0)
                return;

            EditorGUILayout.BeginHorizontal();


            var color = EditorGUIUtil.BeginContentColor(Color.red);
            EditorGUILayout.LabelField($"Not Included Assets [{cachedInfo.notIncludedAssets.Length}]", 
                GUILayout.Width(200));
            EditorGUIUtil.EndContentColor(color);

            if (GUILayout.Button("Add Ignore All", GUILayout.Width(100)))
            {
                foreach (var asset in cachedInfo.notIncludedAssets)
                {
                    manifest.ignoreAssets.Add(asset);
                }

                manifest.MarkAsDirty();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            foreach (var asset in cachedInfo.notIncludedAssets)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(asset, asset.GetType(), false, GUILayout.Width(ObjectFieldWidth));
                if(GUILayout.Button("Add Ignore", GUILayout.Width(80)))
                {
                    manifest.AddIgnoreAsset(asset);
                }

                EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(asset));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        private void OnChangedManifest(TAssetBundleManifest manifest)
        {
            _cachedInfos.Remove(manifest);
        }
    }

}
