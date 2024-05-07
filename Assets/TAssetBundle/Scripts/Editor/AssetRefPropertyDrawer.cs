using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    [CustomPropertyDrawer(typeof(AssetRef), true)]
    internal class AssetRefPropertyDrawer : PropertyDrawer
    {
        private static readonly Type UnityObjectType = typeof(UnityEngine.Object);


        private static bool IsUnityObjectType(Type type)
        {
            if (type == UnityObjectType)
            {
                return true;
            }

            if (type.BaseType != null && IsUnityObjectType(type.BaseType))
            {
                return true;
            }

            return false;
        }

        private static AssetTypeAttribute GetAssetTypeAttribute(FieldInfo fieldInfo)
        {
            var assetTypeAttribute = fieldInfo.GetCustomAttribute<AssetTypeAttribute>();

            if (assetTypeAttribute == null)
            {
                var fieldType = fieldInfo.FieldType;

                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    fieldType = fieldType.GetGenericArguments()[0];
                }
                else if (fieldType.BaseType == typeof(Array))
                {
                    fieldType = fieldType.GetElementType();
                }

                assetTypeAttribute = fieldType.GetCustomAttribute<AssetTypeAttribute>();
            }

            return assetTypeAttribute;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var assetTypeAttribute = GetAssetTypeAttribute(fieldInfo);

            if (assetTypeAttribute == null)
            {
                DrawError(position, property, "Need AssetType Attribute");
                return;
            }

            if (!IsUnityObjectType(assetTypeAttribute.AssetType))
            {
                DrawError(position, property, "Invalid Asset Type");
                return;
            }

            var assetGUIDProperty = property.FindPropertyRelative("assetGUID");
            var assetPathProperty = property.FindPropertyRelative("assetPath");
            var assetGUID = assetGUIDProperty.stringValue;
            UnityEngine.Object asset = null;

            if (!string.IsNullOrEmpty(assetGUID))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                asset = AssetDatabase.LoadMainAssetAtPath(assetPath);

                if (asset == null)
                {
                    assetGUIDProperty.stringValue = string.Empty;
                    assetPathProperty.stringValue = string.Empty;
                }
            }

            string displayName;

            try
            {
                int pos = int.Parse(property.propertyPath.Split('[').LastOrDefault().TrimEnd(']'));
                displayName = "Element " + pos;
            }
            catch
            {
                displayName = property.displayName;
            }

            label = EditorGUI.BeginProperty(position, new GUIContent(displayName), property);
            EditorGUI.BeginChangeCheck();

            var newAsset = EditorGUI.ObjectField(position, label, asset, assetTypeAttribute.AssetType, false);

            if (EditorGUI.EndChangeCheck())
            {
                var assetPath = AssetDatabase.GetAssetPath(newAsset);
                assetPathProperty.stringValue = assetPath;
                assetGUIDProperty.stringValue = AssetDatabase.AssetPathToGUID(assetPath);                
            }

            EditorGUI.EndProperty();
        }

        private void DrawError(Rect position, SerializedProperty property, string message)
        {
            var color = GUI.color;
            GUI.color = Color.red;
            EditorGUI.LabelField(position, property.displayName, message);
            GUI.color = color;
        }
    }
}
