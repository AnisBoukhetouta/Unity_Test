using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    /// <summary>
    /// composition strategy info
    /// </summary>
    [Serializable]
    public class CompositionStrategyInfo
    {
        /// <summary>
        /// composition strategy
        /// </summary>
        public TAssetBundleCompositionStrategy strategy;

        /// <summary>
        /// composition strategy data
        /// </summary>
        [SerializeReference]
        public TAssetBundleCompositionStrategy.Data data;
    }


    /// <summary>
    /// asset bundle build info
    /// </summary>
    [Serializable]
    public class AssetBundleBuildInfo
    {
        /// <summary>
        /// build name
        /// </summary>
        public string buildName = string.Empty;

        /// <summary>
        /// build objects
        /// </summary>
        public List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
        public IEnumerable<string> ObjectPaths => objects.Select(obj => AssetDatabase.GetAssetPath(obj));
    }


    /// <summary>    
    /// TAssetBunldeManifest
    /// </summary>
    [CreateAssetMenu(menuName = "TAssetBundle/TAssetBundleManifest")]
    public class TAssetBundleManifest : ScriptableObject
    {
        /// <summary>
        /// manifest enable
        /// </summary>
        [Tooltip("Activation options (excluded from build when inactive)")]
        public bool enabled = true;

        /// <summary>
        /// asset bundles builtin
        /// </summary>
        [Tooltip("Are AssetBundles built into the app?")]
        public bool builtin = false;

        /// <summary>
        /// asset bundles encrypt
        /// </summary>
        [Tooltip("Are AssetBundles Encrypted?")]
        public bool encrypt = false;

        /// <summary>
        /// asset tag
        /// </summary>
        [Tooltip("Tags")]
        public TagInfo tag;

        /// <summary>
        /// composition strategy infos
        /// </summary>        
        public List<CompositionStrategyInfo> compositionStrategyInfos;

        /// <summary>
        /// Ignore assets are not made up of AssetBundle Build
        /// </summary>
        public List<UnityEngine.Object> ignoreAssets;

        /// <summary>
        /// asset bundle build infos
        /// </summary>        
        public List<AssetBundleBuildInfo> assetBundleBuildInfos;

        /// <summary>
        /// Manifest file path
        /// </summary>
        public string ManifestPath => AssetDatabase.GetAssetPath(this);

        /// <summary>
        /// Manifest Depth
        /// </summary>
        public int Depth => ManifestPath.Count(c => c == '/');

        /// <summary>
        /// checks if saved to disk
        /// </summary>
        public bool IsPersistent => EditorUtility.IsPersistent(this);        

        /// <summary>
        /// Event raised when manifest changes 
        /// </summary>
        public event Action<TAssetBundleManifest> OnChanged;        



        #region ContextMenu
        /// <summary>
        /// Save the manifest file
        /// </summary>
        [ContextMenu("Save")]
        public void Save()
        {
            MarkAsDirty();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Clear asset bundle build infos
        /// </summary>
        [ContextMenu("Clear Asset Bundle Build Infos")]
        public void ClearAssetBundleBuildInfos()
        {
            assetBundleBuildInfos.Clear();
            Save();
        }

        /// <summary>
        /// Run composition strategy
        /// </summary>
        [ContextMenu("Run Composition Strategy")]
        public void RunCompositionStrategy()
        {
            if (compositionStrategyInfos.Count == 0)
                return;

            Logger.Log("run composition strategy - " + ManifestPath);

            foreach (var info in compositionStrategyInfos)
            {
                info.strategy.Run(this, info.data);
            }

            RemoveBuildInfoWithNotIncludedAssets();

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Adds not included assets as a single AssetBundle.
        /// </summary>
        [ContextMenu("Add New Asset All Together")]
        public void AddNewAssetsAllTogether()
        {
            var notIncludedAssets = GetNotIncludedAssets();

            if (notIncludedAssets.Length > 0)
            {
                AddAssetBundleBuildInfo(new AssetBundleBuildInfo
                {
                    buildName = notIncludedAssets.First().name,
                    objects = notIncludedAssets.ToList()
                });

                Save();
            }
        }
        #endregion

        /// <summary>
        /// mark as dirty
        /// </summary>
        public void MarkAsDirty()
        {
            if (IsPersistent)
            {                
                EditorUtility.SetDirty(this);                
            }

            OnChanged?.Invoke(this);
        }

        /// <summary>
        /// Add asset bundle build information
        /// </summary>
        /// <param name="assetBundleBuildInfo"></param>
        public void AddAssetBundleBuildInfo(AssetBundleBuildInfo assetBundleBuildInfo)
        {
            Logger.Log(string.Format("add asset bundle - {0} [{1}]", assetBundleBuildInfo.buildName,
                            string.Join(", ", assetBundleBuildInfo.ObjectPaths.Select(path => Path.GetFileName(path)))));

            assetBundleBuildInfos.Add(assetBundleBuildInfo);

            MarkAsDirty();
        }

        /// <summary>
        /// Get assets not included in the manifest
        /// </summary>
        /// <returns>assets</returns>
        public UnityEngine.Object[] GetNotIncludedAssets()
        {
            var manifests = GetManifestTree();

            return GetValidAssets()
                .Where(asset => manifests.All(manifest => !manifest.IsIncludedAsset(asset)))
                .ToArray();
        }


        /// <summary>
        /// Get assets path not included in the manifest
        /// </summary>
        /// <returns>asset paths</returns>
        public string[] GetNotIncludedAssetPaths()
        {
            return GetNotIncludedAssets()
                .Select(asset => AssetDatabase.GetAssetPath(asset))
                .ToArray();
        }


        /// <summary>
        /// Remove tag
        /// </summary>
        /// <param name="removeTag">tag</param>
        public void RemoveTag(string removeTag)
        {
            tag.tags = tag.tags.Where(t => t != removeTag).ToArray();
            MarkAsDirty();
        }

        /// <summary>
        /// Rename tag
        /// </summary>
        /// <param name="oldTag">old tag</param>
        /// <param name="newTag">new tag</param>
        public void RenameTag(string oldTag, string newTag)
        {            
            tag.tags = tag.tags.Where(t => t != oldTag).Append(newTag).ToArray();
            MarkAsDirty();
        }

        #region INTERNAL
        
        private void OnEnable()
        {
            EditorApplication.projectChanged += OnProjectChanged;

            if (IsPersistent)
            {
                TAssetBundleTagUtil.GetTagRepository().AddTags(tag.tags);
            }
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void OnProjectChanged()
        {
            CheckValidate();
        }

        private void Reset()
        {
            CheckValidate();
        }

        private void OnValidate()
        {
            EditorApplication.delayCall += CheckValidate;
        }

        private void CheckValidate()
        {
            if (!IsPersistent)
                return;

            if(TAssetBundleManifestValidator.CheckValidate(this))
            {   
                EditorGUIUtility.PingObject(this);
            }

            MarkAsDirty();
        }

        internal string GetAssetBundleName(AssetBundleBuildInfo assetBundleBuildInfo)
        {
            var manifestPath = ManifestPath;
            var manifestExtension = Path.GetExtension(manifestPath);
            var manifestDirectoryPath = manifestPath.Substring(0, manifestPath.Length - manifestExtension.Length);
            var baseName = manifestDirectoryPath.Substring("Assets/".Length);

            return string.Format("{0}/{1}", baseName, assetBundleBuildInfo.buildName).ToLower();
        }

        private AssetBundleBuild GetAssetBundleBuild(AssetBundleBuildInfo assetBundleBuildInfo)
        {
            if (string.IsNullOrEmpty(assetBundleBuildInfo.buildName))
            {
                throw new InvalidDataException(string.Format("not exist asset bundle name - manifestPath:{0}, index:{1}",
                    ManifestPath, assetBundleBuildInfos.IndexOf(assetBundleBuildInfo)));
            }

            var assetPaths = new List<string>();

            CollectAssetPaths(assetPaths, assetBundleBuildInfo);

            var extensions = new HashSet<string>(assetPaths.Select(assetPath => Path.GetExtension(assetPath)));

            if (extensions.Contains(".unity") && extensions.Count > 1)
            {
                EditorGUIUtility.PingObject(this);

                throw new InvalidDataException(
                    string.Format(
                        "Can't build scenes and assets together with a one asset bundle - " +
                        "manifestPath:{0}, assetBundleName:{1}",
                        ManifestPath, assetBundleBuildInfo.buildName));
            }

            return new AssetBundleBuild
            {
                assetBundleName = GetAssetBundleName(assetBundleBuildInfo),
                assetNames = assetPaths.ToArray()
            };
        }

        internal List<AssetBundleBuild> GetAssetBundleBuilds()
        {
            List<AssetBundleBuild> assetBundleBuildList = new List<AssetBundleBuild>(assetBundleBuildInfos.Count);

            foreach (var assetBundleBuildInfo in assetBundleBuildInfos)
            {
                if(assetBundleBuildInfo.objects.Count == 0)
                {
                    continue;
                }

                var assetBundleBuild = GetAssetBundleBuild(assetBundleBuildInfo);

                if (assetBundleBuild.assetNames.Length > 0)
                {
                    assetBundleBuildList.Add(assetBundleBuild);
                }
            }

            return assetBundleBuildList;
        }

        internal void CollectAssetPaths(List<string> assetPaths, AssetBundleBuildInfo assetBundleBuildInfo)
        {
            foreach (var obj in assetBundleBuildInfo.objects)
            {
                if (obj == null)
                    continue;

                var assetPath = AssetDatabase.GetAssetPath(obj.GetInstanceID());

                if (string.IsNullOrEmpty(assetPath))
                {
                    Logger.Warning(string.Format("invalid asset - manifest:{0}, index:{1}",
                        ManifestPath, assetBundleBuildInfos.IndexOf(assetBundleBuildInfo)));

                    continue;
                }

                EditorUtil.CollectAssetPaths(assetPaths, assetPath, path => !EditorUtil.IsScript(path));
            }
        }

        internal static IEnumerable<TAssetBundleManifest> GetManifestAll()
        {
            return AssetDatabase.FindAssets("t: TAssetBundleManifest").Select(guid =>
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAssetAtPath<TAssetBundleManifest>(assetPath);
                
            }).OrderByDescending(manifest => manifest.Depth);
        }

        internal TAssetBundleManifest[] GetManifestTree()
        {
            return GetManifestTree(GetManifestDirectoryPath());
        }

        private static TAssetBundleManifest[] GetManifestTree(string targetPath)
        {
            return GetManifestAll()
                .Where(manifest => manifest.GetManifestDirectoryPath().StartsWith(targetPath))
                .ToArray();
        }

        internal string GetManifestDirectoryPath()
        {
            return EditorUtil.GetDirectoryPath(ManifestPath);
        }

        internal IEnumerable<UnityEngine.Object> GetValidAssets()
        {
            return EditorUtil.GetAssetPathsByDirectory(GetManifestDirectoryPath())
                .Select(path => AssetDatabase.LoadMainAssetAtPath(path))
                .Where(asset => !(asset is TAssetBundleManifest));
        }

        internal bool IsIncludedAsset(UnityEngine.Object asset)
        {
            if (IsParentAsset(asset))
                return true;

            if (ignoreAssets.Contains(asset))
                return true;

            foreach (var info in assetBundleBuildInfos)
            {
                if (info.objects.Contains(asset))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// remove asset bundle build info with not included assets
        /// </summary>
        private void RemoveBuildInfoWithNotIncludedAssets()
        {
            var removedCount = assetBundleBuildInfos.RemoveAll(buildInfo => buildInfo.objects.Count == 0);

            if (removedCount > 0)
            {
                MarkAsDirty();
            }
        }

        private bool IsParentAsset(UnityEngine.Object asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var manifestDirectoryPath = GetManifestDirectoryPath();

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                if (manifestDirectoryPath.StartsWith(assetPath))
                {
                    return true;
                }
            }

            return false;
        }

        internal void AddIgnoreAsset(UnityEngine.Object asset)
        {            
            ignoreAssets.Add(asset);
            MarkAsDirty();
        }

        #endregion
    }

}