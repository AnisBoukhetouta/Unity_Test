using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    [CustomPropertyDrawer(typeof(CompositionStrategyInfo))]
    public class CompositionStrategyInfoPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var strategyProperty = property.FindPropertyRelative("strategy");

            var height = EditorGUI.GetPropertyHeight(strategyProperty);

            position.height = height;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, strategyProperty, new GUIContent(property.displayName));
            var changed = EditorGUI.EndChangeCheck();

            var dataProperty = property.FindPropertyRelative("data");
            var strategy = strategyProperty.objectReferenceValue as TAssetBundleCompositionStrategy;
            var useData = strategy != null && strategy.IsUseData();

            if (changed)
            {
                dataProperty.managedReferenceValue = useData ? strategy.CreateData() : null;
            }

            if (useData)
            {
                EditorGUI.indentLevel++;
                position.y += height;
                position.height = EditorGUI.GetPropertyHeight(dataProperty, true);
                EditorGUI.PropertyField(position, dataProperty, true);
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var strategyProperty = property.FindPropertyRelative("strategy");
            var strategy = strategyProperty.objectReferenceValue as TAssetBundleCompositionStrategy;

            var height = EditorGUI.GetPropertyHeight(strategyProperty);

            if (strategy != null && strategy.IsUseData())
            {
                var dataProperty = property.FindPropertyRelative("data");
                height += EditorGUI.GetPropertyHeight(dataProperty, true);
            }

            return height;
        }
    }
}

