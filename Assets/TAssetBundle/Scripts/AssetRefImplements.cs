using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TAssetBundle
{

    /// <summary>
    /// Scene Asset Reference
    /// </summary>
    [Serializable]
#if UNITY_EDITOR
    [AssetType(typeof(SceneAsset))]
#endif
    public class SceneAssetRef : AssetRef
    {
    }


    /// <summary>
    /// GameObject Asset Reference
    /// </summary>
    [Serializable]
    [AssetType(typeof(GameObject))]
    public class GameObjectAssetRef : AssetRef
    {
    }
}