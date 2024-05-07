using System;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    public class AssetListWindow : EditorWindow
    {
        private UnityEngine.Object[] _assets;
        private Vector2 _scrollPosition;
        private Action<UnityEngine.Object> _customDrawer;

        public static void Show(string title, UnityEngine.Object[] assets, Action<UnityEngine.Object> customDrawer = null)
        {
            var window = GetWindow<AssetListWindow>(title);
            window.Setting(assets, customDrawer);
            window.Show();
        }


        private void Setting(UnityEngine.Object[] assets, Action<UnityEngine.Object> customDrawer)
        {
            _assets = assets;
            _customDrawer = customDrawer;
        }

        private void OnGUI()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollScope.scrollPosition;

                foreach (var asset in _assets)
                {
                    DrawAsset(asset);
                }
            }
        }

        private void DrawAsset(UnityEngine.Object asset)
        {
            if(_customDrawer != null)
            {
                _customDrawer(asset);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(asset, asset.GetType(), false, GUILayout.Width(300f));
                EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(asset));
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}