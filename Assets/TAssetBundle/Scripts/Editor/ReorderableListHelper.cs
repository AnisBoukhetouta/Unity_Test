using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TAssetBundle.Editor
{
    public static class ReorderableListHelper
    {
        public static ReorderableList Create(SerializedProperty serializedProperty,
            bool draggable = true,
            bool displayHeader = true,
            bool displayAddButton = true,
            bool displayRemoveButton = true,
            float space = 5f)
        {
            var reorderableList = new ReorderableList(serializedProperty.serializedObject,
                serializedProperty, draggable, displayHeader, displayAddButton, displayRemoveButton);

            if(displayHeader)
            {
                reorderableList.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, serializedProperty.displayName);
                };
            }
            else
            {
                reorderableList.headerHeight = 2f;
            }

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                EditorGUI.indentLevel++;
                var element = serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, element, true);
                EditorGUI.indentLevel--;
            };

            reorderableList.elementHeightCallback = (int index) =>
            {
                var element = serializedProperty.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, true) + space;
            };

            return reorderableList;
        }


        public static bool DoLayoutListWithFoldout(this ReorderableList reorderableList, string foldoutKey, string label, Color color = default)
        {
            var foldout = EditorGUIUtil.FoldoutHeaderGroup(foldoutKey
                , string.Format("{0} [{1}]", label, reorderableList.count)
                , color
                , true);

            if(foldout)
            {
                reorderableList.DoLayoutList();
            }

            return foldout;
        }
    }

}
