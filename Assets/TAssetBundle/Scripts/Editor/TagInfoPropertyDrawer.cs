using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    [CustomPropertyDrawer(typeof(TagInfo))]
    [CanEditMultipleObjects]
    internal class TagInfoPropertyDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 30f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {   
            var tagsProperty = property.FindPropertyRelative("tags");

            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(position, property.isExpanded, $"{label.text} [{tagsProperty.arraySize}]");

            EditorGUI.EndFoldoutHeaderGroup();

            if (property.isExpanded)
            {
                position.y += EditorGUIUtility.singleLineHeight;
                position = DrawTags(position, tagsProperty);

                position.width = ButtonWidth;
                position.x = EditorGUIUtility.currentViewWidth - position.width;

                if (EditorGUI.DropdownButton(position, new GUIContent("+"), FocusType.Passive))
                {
                    DrawMenu(tagsProperty);
                }
            }
        }

        private static Rect DrawTags(Rect position, SerializedProperty tagsProperty)
        {
            for (int i = 0; i < tagsProperty.arraySize; ++i)
            {
                var tagProp = tagsProperty.GetArrayElementAtIndex(i);

                if (tagProp.propertyType != SerializedPropertyType.String)
                {
                    break;
                }

                DrawTag(position, tagProp.stringValue, () =>
                {
                    tagsProperty.DeleteArrayElementAtIndex(i);
                });

                position.y += EditorGUIUtility.singleLineHeight;
            }

            return position;
        }

        private static void DrawMenu(SerializedProperty tagsProperty)
        {            
            var tags = new Dictionary<string, int>();

            for (int i = 0; i < tagsProperty.arraySize; ++i)
            {
                var tagProp = tagsProperty.GetArrayElementAtIndex(i);

                if (tagProp.propertyType != SerializedPropertyType.String)
                    break;

                tags.Add(tagProp.stringValue, i);
            }

            var menu = new GenericMenu();
            var tagRepository = TAssetBundleTagUtil.GetTagRepository();

            foreach (var tag in tagRepository.tags)
            {
                menu.AddItem(new GUIContent(tag), tags.ContainsKey(tag), data =>
                {
                    if (tags.TryGetValue(tag, out int index))
                    {
                        tagsProperty.DeleteArrayElementAtIndex(index);
                    }
                    else
                    {
                        tagsProperty.arraySize++;
                        tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = tag;
                    }

                    tagsProperty.serializedObject.ApplyModifiedProperties();

                }, tag);
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Edit Tag"), false, () =>
            {
                TAssetBundleTagEditorWindow.OpenWindow();
            });

            menu.ShowAsContext();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;

            if (property.isExpanded)
            {
                height += EditorGUIUtility.singleLineHeight;

                var tagsProperty = property.FindPropertyRelative("tags");
                height += tagsProperty.arraySize * EditorGUIUtility.singleLineHeight;
            }

            return height;
        }

        private static void DrawTag(Rect position, string tag, Action onRemove)
        {
            position.width = EditorGUIUtility.currentViewWidth - ButtonWidth - 50f;
            EditorGUI.SelectableLabel(position, tag);

            position.x = EditorGUIUtility.currentViewWidth - ButtonWidth;
            position.width = ButtonWidth;

            if (GUI.Button(position, "-"))
            {
                onRemove?.Invoke();
            }
        }
    }
}
