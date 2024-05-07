using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace TAssetBundle.Editor
{
    [InitializeOnLoad]
    public static class AssetBundleBuilder
    {
        #region Settings
        /// <summary>
        /// TAssetBundle settings
        /// </summary>
        public static Settings Settings => AssetDatabase.LoadAssetAtPath<Settings>(Defines.SettingFilePath);

        /// <summary>
        /// save the settings file
        /// </summary>
        public static void SaveSettings()
        {
            EditorUtility.SetDirty(Settings);
            AssetDatabase.SaveAssets();
        }

        #endregion

        #region Event
        /// <summary>
        /// Event raised when the build completed
        /// </summary>
        public static event Action<BuildTarget> OnBuildCompleted;
        #endregion

        #region Public

        /// <summary>
        /// Only manifests using composition strategies clear asset bundle build infos. 
        /// </summary>
        public static void ClearAllAssetBundleBuildInfos()
        {
            Logger.Log("clear asset bundle build infos composition strategy only");

            foreach (var manifest in GetAllManifests().Where(iter => iter.compositionStrategyInfos.Count > 0))
            {
                manifest.ClearAssetBundleBuildInfos();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// run all composition strategy
        /// </summary>
        public static void RunAllCompositionStrategy()
        {
            Logger.Log("run all composition strategy");

            foreach (var manifest in GetAllManifests())
            {
                manifest.RunCompositionStrategy();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Get all manifests sorted by depth
        /// </summary>
        /// <returns>TAssetBundleManifest Collection</returns>
        public static IEnumerable<TAssetBundleManifest> GetAllManifests()
        {
            return TAssetBundleManifest.GetManifestAll();
        }

        /// <summary>
        /// Build assets for the current platform
        /// </summary>
        public static void BuildAssetBundle()
        {
            BuildAssetBundle(Util.GetActiveBuildTarget(), Settings.catalogName);
        }

        /// <summary>
        /// Build assets for that platform
        /// </summary>
        /// <param name="buildTarget">target platform</param>
        public static void BuildAssetBundle(BuildTarget buildTarget)
        {
            BuildAssetBundle(buildTarget, Settings.catalogName);
        }

        /// <summary>
        /// Build the assets with the target platform and catalog name
        /// </summary>
        /// <param name="buildTarget">target platform</param>
        /// <param name="catalogName">catalog name</param>
        public static void BuildAssetBundle(BuildTarget buildTarget, string catalogName)
        {
            var buildManifests = GetAllManifests().Where(manifest => manifest.enabled).ToArray();

            if (buildManifests.Length == 0)
            {
                Logger.Warning("not found TAssetBundleManifest");
                return;
            }

            try
            {
                BuildAssetBundle(buildTarget, catalogName, buildManifests);
            }
            catch (Exception e)
            {
                string outputPath = Path.Combine(Settings.assetBundleOutputPath, buildTarget.ToString());
                EditorUtil.DeleteDirectory(outputPath);
                throw e;
            }
        }

        /// <summary>
        /// Get the build output path of the target platform
        /// </summary>
        /// <param name="buildTarget">target platform</param>
        /// <returns>output path</returns>
        public static string GetOutputPath(BuildTarget buildTarget)
        {
            return Path.Combine(Settings.assetBundleOutputPath, buildTarget.ToString());
        }

        /// <summary>
        /// Clear the build cache of the target platform
        /// </summary>
        /// <param name="buildTarget">target platform</param>
        public static void ClearBuildCache(BuildTarget buildTarget)
        {
            Cache.BuildCache.ClearBuildCache(buildTarget);
        }


        /// <summary>
        /// Run a dry build on the target platform and check validation
        /// </summary>
        /// <param name="outputPath">output path</param>
        /// <param name="assetBundleBuilds">asset bundle builds</param>
        /// <param name="buildTarget">target platform</param>
        /// <returns>AssetBundleManifest</returns>
        public static AssetBundleManifest DryRunBuild(string outputPath, AssetBundleBuild[] assetBundleBuilds, BuildTarget buildTarget)
        {
            var assetBundleManifest = EditorUtil.BuildAssetBundle(outputPath, assetBundleBuilds, buildTarget, true);

            if (assetBundleManifest != null)
            {
                CheckValidAssetBundleManifest(assetBundleManifest);
            }

            return assetBundleManifest;
        }

        /// <summary>
        /// Get asset bundle build information of target manifests
        /// </summary>
        /// <param name="manifests">manifests</param>
        /// <returns>AssetBundleBuild list</returns>
        public static List<AssetBundleBuild> GetAssetBundleBuilds(TAssetBundleManifest[] manifests)
        {
            return manifests.SelectMany(manifest => manifest.GetAssetBundleBuilds()).ToList();
        }
        #endregion

        #region Internal
        static AssetBundleBuilder()
        {
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            TAssetBundleTagUtil.GetTagRepository();
            EditorUtil.GetOrCreateScriptableFile<Settings>(Defines.SettingFilePath);
        }

        private static void CheckDuplicateAssets(TAssetBundleManifest[] manifests)
        {
            Dictionary<string, TAssetBundleManifest> assetManifestMap = new Dictionary<string, TAssetBundleManifest>(StringComparer.OrdinalIgnoreCase);

            List<string> assetPaths = new List<string>();

            foreach (var manifest in manifests)
            {
                foreach (var assetBundleBuildInfo in manifest.assetBundleBuildInfos)
                {
                    assetPaths.Clear();
                    manifest.CollectAssetPaths(assetPaths, assetBundleBuildInfo);

                    foreach (var assetPath in assetPaths)
                    {
                        if (assetManifestMap.TryGetValue(assetPath, out TAssetBundleManifest includedManifest))
                        {
                            var text = string.Format("the asset has already been included in the other assetbundle - " +
                                "includedManifest:{0}, manifest:{1}, assetPath:{2}",
                                includedManifest.ManifestPath, manifest.ManifestPath, assetPath);

                            EditorGUIUtility.PingObject(manifest);
                            throw new InvalidDataException(text);
                        }
                        else
                        {
                            assetManifestMap.Add(assetPath, manifest);
                        }
                    }
                }
            }
        }

        private static AssetBundleBuild[] GetAssetBundleBuilds(TAssetBundleManifest[] manifests, List<AssetBundleInfo> cachedAssetBundles)
        {
            var assetBundleBuilds = GetAssetBundleBuilds(manifests);

            var cachedAssetBundleNames = new HashSet<string>(cachedAssetBundles.Select(cachedAssetBundle => cachedAssetBundle.assetBundleName));

            assetBundleBuilds.RemoveAll(build => cachedAssetBundleNames.Contains(build.assetBundleName));

            return assetBundleBuilds.ToArray();
        }

        private static void BuildAssetBundle(BuildTarget buildTarget,
            string catalogName,
            TAssetBundleManifest[] manifests)
        {
            Logger.Log(string.Format("build asset bundle - platform:{0}, catalogName:{1}", buildTarget, catalogName));

            var stopwatch = Stopwatch.StartNew();

            CheckDuplicateAssets(manifests);

            string outputPath = GetOutputPath(buildTarget);
            EditorUtil.DeleteDirectory(outputPath);

            var catalogFileHandler = Settings.GetCatalogFileHandler();
            Cache.BuildCache.PreprocessResult cacheResult;

            var buildInfo = Cache.BuildCache.GetBuildInfo(catalogName, Settings.build, manifests);

            if (Settings.useBuildCache)
            {
                cacheResult = Cache.BuildCache.PreprocessBuildCache(
                    buildTarget,
                    catalogFileHandler,
                    outputPath,
                    manifests,
                    buildInfo);

                if (cacheResult.cachedBuildInfo != null)
                {
                    catalogFileHandler.DeleteCatalog(Path.Combine(outputPath, cacheResult.cachedBuildInfo.catalogName));
                }

                foreach (var cachedAssetBundle in cacheResult.cachedAssetBundles)
                {
                    Logger.Log(string.Format("build skip cached asset bundle - {0}, hash:{1}",
                        cachedAssetBundle.assetBundleName, cachedAssetBundle.hashString));
                }
            }
            else
            {
                cacheResult = new Cache.BuildCache.PreprocessResult();
            }

            bool result = true;
            var assetBundleBuilds = GetAssetBundleBuilds(manifests, cacheResult.cachedAssetBundles);

            if (assetBundleBuilds.Length > 0)
            {
                var assetBundleManifest = BuildAssetBundles(outputPath, assetBundleBuilds, buildTarget);
                var assetBundleInfos = GetAssetBundleInfos(assetBundleManifest, cacheResult.cachedAssetBundles, outputPath);
                CreateAssetCatalog(buildTarget, assetBundleInfos, outputPath, catalogName);
                Cache.BuildCache.SaveCache(buildTarget, outputPath, buildInfo);
            }
            else if (cacheResult.IsCached())
            {
                CreateAssetCatalog(buildTarget, cacheResult.cachedAssetBundles, outputPath, catalogName);
                Cache.BuildCache.SaveCache(buildTarget, outputPath, buildInfo);
            }
            else
            {
                result = false;
            }

            stopwatch.Stop();

            if (result)
            {
                AssetDatabase.Refresh();

                Logger.Log(string.Format("build finish asset bundle - platform:{0}, catalogName:{1}, elapsedSeconds:{2}",
                    buildTarget, catalogName, stopwatch.Elapsed.TotalSeconds));

                OnBuildCompleted?.Invoke(buildTarget);
            }
            else
            {
                Logger.Warning("not found buildable assets");
            }
        }

        private static AssetBundleManifest BuildAssetBundles(string outputPath, AssetBundleBuild[] assetBundleBuilds, BuildTarget buildTarget)
        {
            DryRunBuild(outputPath, assetBundleBuilds, buildTarget);
            DumpAssetBundleBuilds(assetBundleBuilds);
            return EditorUtil.BuildAssetBundle(outputPath, assetBundleBuilds, buildTarget, false);
        }

        private static void DumpAssetBundleBuilds(AssetBundleBuild[] assetBundleBuilds)
        {
            string assetBundleNames = string.Join(", ", assetBundleBuilds.Select(assetBundleBuild =>
            {
                if (string.IsNullOrEmpty(assetBundleBuild.assetBundleVariant))
                {
                    return assetBundleBuild.assetBundleName;
                }

                return string.Format("{0}.{1}", assetBundleBuild.assetBundleName, assetBundleBuild.assetBundleVariant);
            }));


            var sb = new StringBuilder();

            sb.AppendLine("build asset bundles - count: " + assetBundleBuilds.Length);

            foreach (var assetBundleBuild in assetBundleBuilds)
            {
                sb.AppendLine(assetBundleBuild.assetBundleName);
            }

            Logger.Log(sb.ToString());
        }

        private static void SettingAssetBundleExtraInfo(AssetBundleInfo assetBundleInfo)
        {
            var manifest = TAssetBundleManifestUtil.GetManifest(assetBundleInfo.assetBundleName);

            if (manifest == null)
            {
                throw new FileNotFoundException("not found manifest - " + assetBundleInfo.assetBundleName);
            }

            assetBundleInfo.encrypt = manifest.encrypt;
            assetBundleInfo.builtin = manifest.builtin;
            assetBundleInfo.tags = manifest.tag.tags;
        }


        private static AssetCatalog CreateAssetCatalog(
            BuildTarget buildTarget,
            List<AssetBundleInfo> assetBundleInfos,
            string outputPath,
            string catalogName)
        {
            Logger.Log("create asset catalog - " + Path.Combine(outputPath, catalogName));

            foreach (var assetBundleInfo in assetBundleInfos)
            {
                SettingAssetBundleExtraInfo(assetBundleInfo);
            }

            AdjustBuiltinDependencies(assetBundleInfos);

            assetBundleInfos.Sort((x, y) =>
            {
                if (x.dependencies.Length != y.dependencies.Length)
                {
                    return x.dependencies.Length.CompareTo(y.dependencies.Length);
                }
                else
                {
                    return x.assetBundleName.CompareTo(y.assetBundleName);
                }
            });

            var catalog = new AssetCatalog
            {
                buildVersion = Defines.Version,
                catalogVersion = Defines.CatalogVersion,
                buildNumber = Settings.buildNumber,
                assetBundleInfos = assetBundleInfos.ToArray()
            };

            Settings.GetCatalogFileHandler().Save(catalog, Path.Combine(outputPath, catalogName));
            RenameAssetBundles(catalog, outputPath);
            RemoveManifestFiles(buildTarget, outputPath);
            RemoveNotUsedAssetBundles(catalog, outputPath);

            return catalog;
        }

        private static List<AssetBundleInfo> GetAssetBundleInfos(AssetBundleManifest manifest,
            List<AssetBundleInfo> cachedAssetBundles,
            string outputPath)
        {
            var assetBundleInfos = new List<AssetBundleInfo>(cachedAssetBundles);

            foreach (var assetBundleName in manifest.GetAllAssetBundles())
            {
                var assetBundleInfo = CreateAssetBundleInfo(manifest, outputPath, assetBundleName);
                assetBundleInfos.Add(assetBundleInfo);
            }

            return assetBundleInfos;
        }

        private static void CheckValidAssetBundleManifest(AssetBundleManifest manifest)
        {
            var assetBundleMap = new Dictionary<string, HashSet<string>>();

            foreach (var assetBundleName in manifest.GetAllAssetBundles())
            {
                assetBundleMap[assetBundleName] = new HashSet<string>(manifest.GetAllDependencies(assetBundleName));
            }

            foreach (var key in assetBundleMap.Keys.ToArray())
            {
                var dependencies = assetBundleMap[key];

                foreach (var dependentAssetBundle in dependencies)
                {
                    if (assetBundleMap[dependentAssetBundle].Contains(key))
                    {
                        throw new InvalidDataException($"cross reference assetBundle - {key} <-> {dependentAssetBundle}");
                    }
                }
            }
        }

        private static AssetBundleInfo CreateAssetBundleInfo(AssetBundleManifest manifest,
            string outputPath,
            string assetBundleName)
        {
            var assetBundleInfo = new AssetBundleInfo();
            string assetBundlePath = Path.Combine(outputPath, assetBundleName);
            var fileInfo = new FileInfo(assetBundlePath);
            var assetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            assetBundleInfo.assetBundleName = assetBundleName;
            assetBundleInfo.hashString = manifest.GetAssetBundleHash(assetBundleName).ToString();
            assetBundleInfo.dependencies = manifest.GetDirectDependencies(assetBundleName);
            assetBundleInfo.size = fileInfo.Length;
            assetBundleInfo.assetPaths = assetBundle.GetAllAssetNames();
            assetBundleInfo.scenePaths = assetBundle.GetAllScenePaths();
            assetBundle.Unload(true);

            return assetBundleInfo;
        }
        private static void AdjustBuiltinDependencies(List<AssetBundleInfo> assetBundleInfos)
        {
            foreach (var assetBundleInfo in assetBundleInfos)
            {
                if (!assetBundleInfo.builtin)
                    continue;

                AdjustBuiltinDependencies(assetBundleInfos, assetBundleInfo);
            }
        }

        private static void AdjustBuiltinDependencies(List<AssetBundleInfo> assetBundleInfos, AssetBundleInfo assetBundleInfo)
        {
            foreach (var dependent in assetBundleInfo.dependencies)
            {
                var dependentAssetBundleInfo = assetBundleInfos.Find(info => info.assetBundleName == dependent);

                if (dependentAssetBundleInfo.builtin)
                    continue;

                Logger.Warning(string.Format("force builtin dependent assetbundle - {0}, dependent:{1}",
                        assetBundleInfo.assetBundleName, dependentAssetBundleInfo.assetBundleName));

                dependentAssetBundleInfo.builtin = true;

                AdjustBuiltinDependencies(assetBundleInfos, dependentAssetBundleInfo);
            }
        }

        private static void RenameAssetBundles(AssetCatalog catalog, string outputPath)
        {
            foreach (var assetBundleInfo in catalog.assetBundleInfos)
            {
                var srcPath = Path.Combine(outputPath, assetBundleInfo.assetBundleName);
                var filename = assetBundleInfo.GetFileName(Settings.build.appendHashFromFileName);
                var destPath = Path.Combine(outputPath, filename + Settings.build.assetBundleFileExtensions);

                if (!File.Exists(srcPath))
                {
                    //이미 빌드 캐시 된 에셋번들은 새로 빌드 되지 않는다
                    if (File.Exists(destPath))
                    {
                        continue;
                    }
                    else
                    {
                        throw new InvalidOperationException("not exist asset bundle - " + srcPath);
                    }
                }

                File.Copy(srcPath, destPath, true);
                File.Delete(srcPath);
            }
        }


        private static void RemoveManifestFiles(BuildTarget buildTarget, string targetPath)
        {
            var platformName = buildTarget.ToString();

            File.Delete(Path.Combine(targetPath, platformName));

            EditorUtil.Work(targetPath, filePath =>
            {
                if (!File.Exists(filePath))
                    return;

                if (Path.GetExtension(filePath) == ".manifest")
                {
                    File.Delete(filePath);
                }
            });
        }

        private static void RemoveNotUsedAssetBundles(AssetCatalog catalog, string outputPath)
        {
            var usedFilePathSet = new HashSet<string>(catalog.assetBundleInfos.Select(info =>
            {
                return info.GetFileName(Settings.build.appendHashFromFileName) + Settings.build.assetBundleFileExtensions;
            }));

            EditorUtil.Work(outputPath, path =>
            {
                if (Directory.Exists(path))
                    return;

                if (Path.GetDirectoryName(path) == outputPath)
                    return;

                var filePath = path.Substring(outputPath.Length + 1).Replace("\\", "/");

                if (!usedFilePathSet.Contains(filePath))
                {
                    Logger.Log("remove not used file - " + path);
                    EditorUtil.DeleteFile(path);
                }
            });

            EditorUtil.RemoveEmptyDirectories(outputPath);
        }

        private static string GetStreamingAssetPath()
        {
            return Path.Combine("Assets/StreamingAssets", Defines.StreamingPathPrefix);
        }

        public static void PreprocessBuild(BuildTarget platform)
        {
            var buildTarget = platform.ToString();

            if (Settings.buildTarget != buildTarget)
            {
                Settings.buildTarget = buildTarget;
                SaveSettings();
            }

            var catalogPath = Path.Combine(Settings.assetBundleOutputPath, buildTarget, Settings.catalogName);
            var catalogFileHandler = Settings.GetCatalogFileHandler();
            var catalog = catalogFileHandler.LoadFromFile(catalogPath);

            if (catalog == null)
            {
                Logger.Warning(string.Format("not found catalog {0} - asset bundle build first", platform));
                return;
            }

            var streamingPath = GetStreamingAssetPath();

            EditorUtil.DeleteDirectory(streamingPath);
            EditorUtil.CreateDirectory(streamingPath);

            if (Settings.buildIncludeCatalog)
            {
                catalogFileHandler.Save(catalog, Path.Combine(streamingPath, Settings.catalogName));
                AssetDatabase.Refresh();
            }

            if (Settings.buildIncludeAssetBundle != EBuildIncludeAssetBundle.None)
            {
                var builtinAssetBundleInfos = GetBuiltinAssetBundleInfos(catalog, Settings.buildIncludeAssetBundle);

                foreach (var assetBundleInfo in builtinAssetBundleInfos)
                {
                    PushBuiltinAssetBundle(streamingPath, platform, catalog, assetBundleInfo);
                }

                AssetDatabase.Refresh();
            }
        }

        private static async void PushBuiltinAssetBundle(string streamingPath,
            BuildTarget platform,
            AssetCatalog catalog,
            AssetBundleInfo assetBundleInfo)
        {
            CheckBuiltinAssetBundleDependencies(catalog, assetBundleInfo, Settings.buildIncludeAssetBundle);

            var assetBundleName = Util.GetAssetBundleName(assetBundleInfo.assetBundleName, assetBundleInfo.hashString, Settings.build.appendHashFromFileName);
            var assetBundlePath = assetBundleName + Settings.build.assetBundleFileExtensions;
            string sourcePath = Path.Combine(Settings.assetBundleOutputPath, platform.ToString(), assetBundlePath);

            if (!File.Exists(sourcePath))
            {
                throw new BuildFailedException("not found builtin asset bundle - " + sourcePath);
            }

            var localAssetBundlePath = Util.GetAssetBundleName(assetBundleInfo.assetBundleName, assetBundleInfo.hashString);

            string destPath = Path.Combine(streamingPath, localAssetBundlePath + Settings.build.assetBundleFileExtensions);

            EditorUtil.CreateDirectory(Path.GetDirectoryName(destPath));

            File.Copy(sourcePath, destPath, true);

            var manifest = TAssetBundleManifestUtil.GetManifest(assetBundleInfo.assetBundleName);

            if (manifest == null)
            {
                throw new BuildFailedException("not found TAssetBundleManifest - " + assetBundleInfo.assetBundleName);
            }

            if (Settings.useBuiltinAssetBundleLZ4Recompress)
            {
                await LZ4RecompressAssetBundle(destPath, destPath);
            }

            if (assetBundleInfo.encrypt)
            {
                Logger.Log("encrypt asset bundle - " + destPath);
                File.WriteAllBytes(destPath, Settings.GetCryptoSerializer().Encrypt(File.ReadAllBytes(destPath)));
            }
        }

        public static void PostprocessBuild(BuildTarget buildTarget)
        {
            var streamingPath = GetStreamingAssetPath();

            if (Directory.Exists(streamingPath))
            {
                EditorUtil.DeleteDirectory(streamingPath);
                File.Delete(streamingPath + ".meta");
                AssetDatabase.Refresh();
            }
        }

        private static async Task LZ4RecompressAssetBundle(string srcPath, string destPath)
        {
            Logger.Log("builtin asset bundle lz4 recompress start - " + srcPath);
            var asyncOperation = AssetBundle.RecompressAssetBundleAsync(srcPath, destPath, BuildCompression.LZ4Runtime, 0, ThreadPriority.High);

            await asyncOperation.ToTask();

            if (asyncOperation.result != AssetBundleLoadResult.Success)
            {
                throw new BuildFailedException($"assetbundle recompress error - " + asyncOperation.humanReadableResult);
            }

            Logger.Log("builtin asset bundle lz4 recompress complete - " + destPath);
        }

        private static bool IsBuiltinAssetBundle(AssetBundleInfo assetBundleInfo, EBuildIncludeAssetBundle buildIncludeAssetBundle)
        {
            return buildIncludeAssetBundle == EBuildIncludeAssetBundle.All || assetBundleInfo.builtin;
        }

        private static void CheckBuiltinAssetBundleDependencies(AssetCatalog catalog, AssetBundleInfo assetBundleInfo, EBuildIncludeAssetBundle buildIncludeAssetBundle)
        {
            foreach (var dependent in assetBundleInfo.dependencies)
            {
                var dependentInfo = catalog.FindAssetBundle(dependent);

                if (IsBuiltinAssetBundle(dependentInfo, buildIncludeAssetBundle))
                    continue;

                var message = string.Format("dependent assetbundle is not builtin - {0}, dependent:{1}",
                    assetBundleInfo.assetBundleName, dependentInfo.assetBundleName);

                throw new BuildFailedException(message);
            }
        }

        private static List<AssetBundleInfo> GetBuiltinAssetBundleInfos(AssetCatalog catalog, EBuildIncludeAssetBundle buildIncludeAssetBundle)
        {
            return catalog.assetBundleInfos.Where(assetBundleInfo =>
            {
                return IsBuiltinAssetBundle(assetBundleInfo, buildIncludeAssetBundle);

            }).ToList();
        }
        #endregion
    }
}