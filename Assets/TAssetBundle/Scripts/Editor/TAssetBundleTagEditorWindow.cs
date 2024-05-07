using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TAssetBundle.Editor
{
    internal class TAssetBundleTagEditorWindow : EditorWindow
    {
        private Vector2 _scrollPosition = Vector2.zero;
        private TAssetBundleTagRepository _tagRepository;
        private ReorderableList _reorderableList;

        
        public static void OpenWindow()
        {
            var window = GetWindow<TAssetBundleTagEditorWindow>("Tag Editor");
            window.Show();
        }

        private void OnEnable()
        {
            _tagRepository = TAssetBundleTagUtil.GetTagRepository();

            _reorderableList = new ReorderableList(_tagRepository.tags, typeof(string),
                true, true, true, true);

            _reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Tag List");
            };

            _reorderableList.onAddCallback = (ReorderableList list) =>
            {
                EditorApplication.delayCall += ShowNewTagTextField;
            };

            _reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var tag = _tagRepository.tags[index];
                EditorGUI.LabelField(rect, tag);

                if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    ShowTagContextMenu(tag);
                }
            };

            _reorderableList.onRemoveCallback = (ReorderableList list) =>
            {
                RemoveTag(_tagRepository.tags[list.index]);
            };

            _reorderableList.onChangedCallback = (ReorderableList list) =>
            {
                EditorUtility.SetDirty(_tagRepository);
            };
        }

        private void ShowNewTagTextField()
        {
            TextFieldEditorWindow.Show(position,
                "Add New Tag",
                ObjectNames.GetUniqueName(_tagRepository.tags.ToArray(), "New Tag"),
                "Save",
                OnAddNewTag, tag => !_tagRepository.tags.Contains(tag));
        }

        private void ShowTagContextMenu(string tag)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Rename"), false, () =>
            {
                TextFieldEditorWindow.Show(position, "Rename Tag", tag, "Apply", value =>
                {
                    RenameTag(tag, value);

                }, newTag => !_tagRepository.tags.Contains(newTag));
            });

            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                RemoveTag(tag);
            });

            var usedManifests = GetManifestWithTags(tag);

            if(usedManifests.Length > 0)
            {
                menu.AddItem(new GUIContent("Show Used Manifests"), false, () =>
                {
                    AssetListWindow.Show($"[{tag}] Manifests", usedManifests);
                });
            }

            menu.ShowAsContext();
        }

        private void OnGUI()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollScope.scrollPosition;
                _reorderableList.DoLayoutList();
            }
        }

        private TAssetBundleManifest[] GetManifestWithTags(string tag)
        {
            return TAssetBundleManifest.GetManifestAll().Where(manifest => manifest.tag.tags.Contains(tag)).ToArray();
        }

        private void OnAddNewTag(string tag)
        {
            _tagRepository.tags.Add(tag);
            EditorUtility.SetDirty(_tagRepository);
        }

        private void RenameTag(string oldTag, string newTag)
        {
            var manifests = GetManifestWithTags(oldTag);

            if (manifests.Length > 0)
            {
                if (!EditorUtil.DisplayDialogOkCancel($"A manifest using the [{oldTag}] exists. Change all to [{newTag}]?"))
                    return;

                foreach (var manifest in manifests)
                {
                    manifest.RenameTag(oldTag, newTag);
                }
            }

            _tagRepository.tags.Remove(oldTag);
            _tagRepository.tags.Add(newTag);
        }


        private void RemoveTag(string tag)
        {
            var manifests = GetManifestWithTags(tag);

            if(manifests.Length > 0)
            {
                if (!EditorUtil.DisplayDialogOkCancel($"A manifest using [{tag}] exists. Are you sure you want to delete all?"))
                    return;

                foreach(var manifest in manifests)
                {
                    manifest.RemoveTag(tag);
                }
            }

            if (_tagRepository.tags.Remove(tag))
            {
                EditorUtility.SetDirty(_tagRepository);
            }
        }
    }

    [CustomEditor(typeof(TAssetBundleTagRepository))]
    internal class TagRepositoryInspectorDrawer : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            base.OnInspectorGUI();
            GUI.enabled = true;

            if (GUILayout.Button("Edit", GUILayout.MaxHeight(30)))
            {
                TAssetBundleTagEditorWindow.OpenWindow();
            }
        }
    }
}
