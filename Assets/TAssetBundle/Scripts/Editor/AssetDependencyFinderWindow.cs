using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{

    internal class AssetDependencyFinderWindow : EditorWindow
    {
        private const float ObjectFieldWidth = 300f;
        private const string FolderSearchFilterKey = "TA.ADF.FolderSearchFilterKey";
        private const string RegexFilterKey = "TA.ADF.RegexFilterKey";
        private const string VisibleScriptKey = "TA.ADF.VisibleScript";

        private UnityEngine.Object[] _targets = new UnityEngine.Object[0];
        private readonly HashSet<string> _dependencies = new HashSet<string>();
        private readonly List<string> _filteredPaths = new List<string>();        
        private Vector2 _scrollPosition;

        private bool _sortOrderName;
        private bool _sortOrderPath;
        private bool _visibleScript;

        #region MENU
        private static AssetDependencyFinderWindow OpenWindow()
        {
            var window = GetWindow<AssetDependencyFinderWindow>();
            window.titleContent = new GUIContent("Dependency Finder");
            window.Show();
            return window;
        }


        [MenuItem("Assets/[TAssetBundle] Run Dependency Finder", priority = 0)]
        private static void MenuSelectDependencies()
        {
            var window = OpenWindow();

            if (Selection.assetGUIDs.Length != 0)
            {
                var assets = Selection.assetGUIDs.Select(guid => AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
                window.SelectTargets(assets);
            }
        }
        #endregion

        private void OnEnable()
        {
            CollectTargetDependencies();
        }

        private void OnGUI()
        {

            var visibleScript = EditorPrefs.GetBool(VisibleScriptKey, false);
            _visibleScript = EditorGUILayout.Toggle("Show Scripts", visibleScript);

            if (visibleScript != _visibleScript)
            {
                EditorPrefs.SetBool(VisibleScriptKey, _visibleScript);
                ApplyFilter();                
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Target Objects");            
            
            for(int i=0; i<_targets.Length; ++i)
            {
                EditorGUI.BeginChangeCheck();

                var target = EditorGUILayout.ObjectField(_targets[i], typeof(UnityEngine.Object), false);

                if (EditorGUI.EndChangeCheck())
                {
                    var newTargets = _targets.ToArray();
                    newTargets[i] = target;
                    SelectTargets(newTargets);
                }
            }            

            EditorGUILayout.EndHorizontal();

            if (_targets.Length == 0)
                return;

            DrawPrefsTextField("Folder Search Filter", GetFolderSearchFilter(), newFilter =>
            {
                EditorPrefs.SetString(FolderSearchFilterKey, newFilter);
                CollectTargetDependencies();
            });

            DrawPrefsTextField("Regex Filter", GetRegexFilter(), newFilter =>
            {
                EditorPrefs.SetString(RegexFilterKey, newFilter);
                ApplyFilter();
            });

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Count: " + _filteredPaths.Count);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawSort("Object", _sortOrderName, () =>
            {
                _sortOrderName = !_sortOrderName;
                ApplySortName();
            }, GUILayout.Width(ObjectFieldWidth));

            DrawSort("Asset Path", _sortOrderPath, () =>
            {
                _sortOrderPath = !_sortOrderPath;
                ApplySortPath();
            });

            EditorGUILayout.EndHorizontal();
            DrawDependencies();
        }

        private void DrawPrefsTextField(string label, string value, Action<string> onChanged)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);

            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUILayout.TextField(value);
            if (EditorGUI.EndChangeCheck())
            {
                onChanged?.Invoke(newValue);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSort(string label, bool value, Action onChanged, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(string.Format("{0} {1}", label, value ? "▲" : "▼"), options))
            {
                onChanged?.Invoke();
            }
        }

        private void DrawDependencies()
        {
            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollViewScope.scrollPosition;

                foreach (var assetPath in _filteredPaths)
                {
                    DrawAsset(assetPath);
                }
            }
        }

        private void DrawAsset(string assetPath)
        {            
            EditorGUILayout.BeginHorizontal();

            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            EditorGUILayout.ObjectField(asset, asset.GetType(), false, GUILayout.Width(ObjectFieldWidth));
            EditorGUILayout.LabelField(assetPath);
            EditorGUILayout.EndHorizontal();
        }

        private void SelectTargets(UnityEngine.Object[] targets)
        {
            _targets = targets;
            CollectTargetDependencies();            
        }

        private void CollectTargetDependencies()
        {
            _scrollPosition = Vector2.zero;
            _dependencies.Clear();
            _filteredPaths.Clear();

            if (_targets.Length == 0)
                return;

            foreach(var target in _targets)
            {
                CollectTargetDependencies(target);
            }

            ApplyFilter();
        }

        private void CollectTargetDependencies(UnityEngine.Object target)
        {
            var targetPath = AssetDatabase.GetAssetPath(target);
            var assetPaths = new List<string>();

            if (AssetDatabase.IsValidFolder(targetPath))
            {
                var assetGuids = AssetDatabase.FindAssets(GetFolderSearchFilter(), new string[] { targetPath });

                assetPaths.AddRange(assetGuids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)));
            }
            else
            {
                assetPaths.Add(targetPath);
            }

            var dependencies = AssetDatabase.GetDependencies(assetPaths.ToArray(), true)
                .Where(assetPath => !AssetDatabase.IsValidFolder(assetPath));
            

            foreach (var assetPath in dependencies)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);

                if (assetPath.EndsWith(".asset"))
                {
                    if (asset is TAssetBundleManifest)
                        continue;

                    if (asset is TAssetBundleCompositionStrategy)
                        continue;
                }

                _dependencies.Add(assetPath);
            }
        }

        private string GetFolderSearchFilter()
        {
            return EditorPrefs.GetString(FolderSearchFilterKey);
        }

        private string GetRegexFilter()
        {
            return EditorPrefs.GetString(RegexFilterKey, string.Empty);
        }

        private void ApplySortPath()
        {
            _filteredPaths.Sort((x, y) =>
            {
                if (_sortOrderPath)
                    return x.CompareTo(y);
                else
                    return y.CompareTo(x);
            });
        }

        private void ApplySortName()
        {
            _filteredPaths.Sort((x, y) =>
            {
                var xname = Path.GetFileName(x);
                var yname = Path.GetFileName(y);

                if (_sortOrderName)
                    return xname.CompareTo(yname);
                else
                    return yname.CompareTo(xname);
            });
        }

        private void ApplyFilter()
        {
            var regexFilter = GetRegexFilter();
            _filteredPaths.Clear();
            _filteredPaths.AddRange(_dependencies.Where(assetPath =>
                FilterAsset(regexFilter, assetPath)));
        }

        private bool FilterAsset(string regexFilter, string assetPath)
        {
            if(!Regex.IsMatch(assetPath, regexFilter, RegexOptions.IgnoreCase))
            {
                return false;
            }

            if (!_visibleScript && assetPath.EndsWith(".cs"))
            {
                return false;
            }
            
            return true;
        }
    }

}
