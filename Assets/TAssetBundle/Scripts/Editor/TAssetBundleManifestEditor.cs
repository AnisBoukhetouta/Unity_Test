using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TAssetBundle.Editor
{
    [CustomEditor(typeof(TAssetBundleManifest))]
    [CanEditMultipleObjects]
    internal class TAssetBundleManifestEditor : UnityEditor.Editor
    {
        private const float ListYOffset = 5f;

        private TAssetBundleManifest manifest;
        private SerializedProperty enabled;
        private SerializedProperty builtin;
        private SerializedProperty encrypt;
        private SerializedProperty tag;
        private ReorderableList compositionStrategyList;
        private ReorderableList ignoreAssetList;
        private ReorderableList assetBundleBuildList;
        private UnityEngine.Object[] _notIncludedAssets;

        private void OnEnable()
        {
            manifest = target as TAssetBundleManifest;
            enabled = serializedObject.FindProperty("enabled");
            builtin = serializedObject.FindProperty("builtin");
            encrypt = serializedObject.FindProperty("encrypt");
            tag = serializedObject.FindProperty("tag");

            compositionStrategyList = ReorderableListHelper.Create(serializedObject.FindProperty("compositionStrategyInfos")
                , displayHeader:false);
            compositionStrategyList.onAddCallback = (ReorderableList list) =>
            {
                manifest.compositionStrategyInfos.Add(new CompositionStrategyInfo());
                manifest.MarkAsDirty();
            };

            ignoreAssetList = ReorderableListHelper.Create(serializedObject.FindProperty("ignoreAssets")
                , displayHeader: false);
            ignoreAssetList.onAddCallback = (ReorderableList list) =>
            {
                manifest.ignoreAssets.Add(null);
                manifest.MarkAsDirty();
            };
            ignoreAssetList.onRemoveCallback = (ReorderableList list) =>
            {
                manifest.ignoreAssets.RemoveAt(list.index);
                manifest.MarkAsDirty();
            };

            assetBundleBuildList = ReorderableListHelper.Create(serializedObject.FindProperty("assetBundleBuildInfos")
                , displayHeader: false);
            assetBundleBuildList.onAddCallback = (ReorderableList list) =>
            {
                manifest.assetBundleBuildInfos.Add(new AssetBundleBuildInfo());
                manifest.MarkAsDirty();
            };

            manifest.OnChanged += OnChangedManifest;

            OnChangedManifest(manifest);
        }

        private void OnDisable()
        {
            manifest.OnChanged -= OnChangedManifest;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(enabled);
            EditorGUILayout.PropertyField(builtin);
            EditorGUILayout.PropertyField(encrypt);
            EditorGUILayout.PropertyField(tag);
            EditorGUILayout.Space(ListYOffset);

            compositionStrategyList.DoLayoutListWithFoldout("TABM.CS." + manifest.ManifestPath,
                "Composition Strategies");
            EditorGUILayout.Space(ListYOffset);

            if(compositionStrategyList.count > 0)
            {
                ignoreAssetList.DoLayoutListWithFoldout("TABM.IO." + manifest.ManifestPath,
                "Ignore Assets");
                EditorGUILayout.Space(ListYOffset);

                if (_notIncludedAssets != null &&
                    _notIncludedAssets.Length > 0)
                {
                    DrawNotIncludedAssets();
                    EditorGUILayout.Space(ListYOffset);
                }
            }

            assetBundleBuildList.DoLayoutListWithFoldout("TABM.ABB." + manifest.ManifestPath, 
                "Asset Bundle Build");

            serializedObject.ApplyModifiedProperties();            
        }

        private void OnChangedManifest(TAssetBundleManifest manifest)
        {
            if (manifest.IsPersistent)
            {
                _notIncludedAssets = manifest.GetNotIncludedAssets();
            }
        }

        private void DrawNotIncludedAssets()
        {
            EditorGUILayout.BeginHorizontal();
            var foldout = EditorGUIUtil.FoldoutHeaderGroup("TABM.NIA." + manifest.GetManifestDirectoryPath(),
                string.Format("Not Included Assets [{0}]", _notIncludedAssets.Length), Color.red);

            if (GUILayout.Button("Add Ignore All", GUILayout.Width(100)))
            {
                foreach (var asset in _notIncludedAssets)
                {
                    manifest.ignoreAssets.Add(asset);
                }

                manifest.MarkAsDirty();
            }

            EditorGUILayout.EndHorizontal();


            if (foldout)
            {
                foreach (var asset in _notIncludedAssets)
                {
                    DrawNotIncludedAsset(asset);
                }
            }
        }

        private void DrawNotIncludedAsset(UnityEngine.Object asset)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(asset, asset.GetType(), false);
            GUILayout.FlexibleSpace();
            
            if(GUILayout.Button("Add Ignore"))
            {
                manifest.ignoreAssets.Add(asset);
                manifest.MarkAsDirty();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

}
