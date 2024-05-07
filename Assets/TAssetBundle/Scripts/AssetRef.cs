using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TAssetBundle
{
    /// <summary>
    /// Asset Reference
    /// </summary>
    [Serializable]
#if UNITY_EDITOR
    public class AssetRef : ISerializationCallbackReceiver
#else
    public class AssetRef
#endif
    {
#if UNITY_EDITOR
        [SerializeField]
        private string assetGUID;
#endif
        [SerializeField]
        private string assetPath;

        /// <summary>
        /// path to the asset
        /// </summary>
#if UNITY_EDITOR
        public string Path => string.IsNullOrEmpty(assetGUID) ? assetPath : AssetDatabase.GUIDToAssetPath(assetGUID);        
#else
        public string Path => assetPath;
#endif

        /// <summary>
        /// is it valid asset path?
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(Path);

        /// <summary>
        /// File name without extension
        /// </summary>
        public string FileName => System.IO.Path.GetFileNameWithoutExtension(Path);

#if UNITY_EDITOR
        public void OnAfterDeserialize()
        {
        }

        public void OnBeforeSerialize()
        {
            if (!string.IsNullOrEmpty(assetGUID))
            {
                assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
            }            
        }
#endif

        public override string ToString()
        {
            return assetPath;
        }
    }

    /// <summary>
    /// AssetType Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AssetTypeAttribute : Attribute
    {
        /// <summary>
        /// Unity Asset Type
        /// </summary>
        public Type AssetType { get; private set; }

        public AssetTypeAttribute(Type assetType)
        {
            AssetType = assetType;
        }
    }

}
