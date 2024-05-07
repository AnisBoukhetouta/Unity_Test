using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor.Cache
{
    internal class BuildCache
    {
        const string CachePath = "TAssetBundleCache";

        public static BuildInfo GetBuildInfo(string catalogName,
            Settings.BuildSetting buildSetting,
            TAssetBundleManifest[] manifestList)
        {
            var assetBundleCacheInfos = new List<BuildInfo.AssetBundleCacheInfo>();

            var fileInfoMap = new Dictionary<string, BuildInfo.FileInfo>(StringComparer.Ordinal);

            foreach (var manifest in manifestList)
            {
                foreach (var assetBundleBuildInfo in manifest.assetBundleBuildInfos)
                {
                    var assetPaths = new List<string>();

                    manifest.CollectAssetPaths(assetPaths, assetBundleBuildInfo);

                    if (assetPaths.Count == 0)
                        continue;

                    var dependencies = assetPaths.SelectMany(assetPath =>
                    {
                        return AssetDatabase.GetDependencies(assetPath).Where(d =>
                        {
                            return Path.HasExtension(d) && !EditorUtil.IsScript(d);
                        });

                    }).ToArray();


                    assetPaths.AddRange(dependencies);

                    var fileInfos = assetPaths.Distinct().Select(path =>
                    {
                        if (!fileInfoMap.TryGetValue(path, out BuildInfo.FileInfo fileInfo))
                        {
                            fileInfo = BuildInfo.FileInfo.Create(path);
                            fileInfoMap.Add(path, fileInfo);
                        }

                        return fileInfo;

                    }).ToArray();

                    assetBundleCacheInfos.Add(new BuildInfo.AssetBundleCacheInfo
                    {
                        assetBundleName = manifest.GetAssetBundleName(assetBundleBuildInfo),
                        files = fileInfos,
                    });
                }
            }

            return new BuildInfo
            {
                catalogName = catalogName,
                catalogVersion = Defines.CatalogVersion,
                build = buildSetting,
                assetBundleCacheInfos = assetBundleCacheInfos.ToArray()
            };
        }

        public class PreprocessResult
        {
            public BuildInfo cachedBuildInfo;
            public List<AssetBundleInfo> cachedAssetBundles = new List<AssetBundleInfo>();

            public bool IsCached()
            {
                return cachedBuildInfo != null && cachedAssetBundles.Count > 0;
            }
        }

        public static PreprocessResult PreprocessBuildCache(BuildTarget buildTarget,
            CatalogFileHandler catalogFileHandler,
            string outputPath,
            TAssetBundleManifest[] manifests,
            BuildInfo buildInfo)
        {
            var result = new PreprocessResult();
            var cachedBuildInfo = LoadBuildInfo(buildTarget);

            if (cachedBuildInfo == null ||
                cachedBuildInfo.build != buildInfo.build ||
                cachedBuildInfo.catalogVersion != buildInfo.catalogVersion)
            {
                return result;
            }

            result.cachedBuildInfo = cachedBuildInfo;

            var cachedAssetPath = GetCacheAssetPath(buildTarget);
            var cachedCatalogPath = Path.Combine(cachedAssetPath, cachedBuildInfo.catalogName);
            var cachedCatalog = catalogFileHandler.LoadFromFile(cachedCatalogPath);

            if (cachedCatalog == null)
            {
                ClearBuildCache(buildTarget);
                return result;
            }

            FileUtil.CopyFileOrDirectory(cachedAssetPath, outputPath);

            var diffList = buildInfo.GetDiffList(cachedCatalog, cachedBuildInfo);

            var removedAssetBundles = new List<AssetBundleInfo>();

            foreach (var diff in diffList)
            {
                if (diff.IsEqual)
                {
                    var cachedAssetBundle = cachedCatalog.FindAssetBundle(diff.current.assetBundleName);

                    if (cachedAssetBundle != null)
                    {
                        var filePath = cachedAssetBundle.GetFileName(buildInfo.build.appendHashFromFileName) + buildInfo.build.assetBundleFileExtensions;
                        var cachedAssetBundlePath = Path.Combine(outputPath, filePath);

                        if (File.Exists(cachedAssetBundlePath))
                        {
                            result.cachedAssetBundles.Add(cachedAssetBundle);
                        }
                    }
                }
                else if (diff.IsNotUsed)
                {
                    var cachedAssetBundle = cachedCatalog.FindAssetBundle(diff.prev.assetBundleName);

                    if (cachedAssetBundle != null)
                    {
                        removedAssetBundles.Add(cachedAssetBundle);
                        var filePath = cachedAssetBundle.GetFileName(buildInfo.build.appendHashFromFileName) + buildInfo.build.assetBundleFileExtensions;

                        Logger.Log("delete not used asset bundle - " + filePath);
                        EditorUtil.DeleteFile(Path.Combine(outputPath, filePath));
                    }
                }
            }

            //에셋번들이 지워지기만 했다면 캐시된 카탈로그를 사용하기 때문에 업데이트 해준다
            if (removedAssetBundles.Count > 0)
            {
                cachedCatalog.assetBundleInfos = cachedCatalog.assetBundleInfos.Except(removedAssetBundles).ToArray();
            }

            var assetBundleBuilds = new List<AssetBundleBuild>();

            foreach (var manifest in manifests)
            {
                assetBundleBuilds.AddRange(manifest.GetAssetBundleBuilds());
            }

            if (assetBundleBuilds.Count == 0)
            {
                ClearBuildCache(buildTarget);
                EditorUtil.DeleteDirectory(outputPath);
                return result;
            }

            var assetBundleManifest = EditorUtil.BuildAssetBundle(outputPath, assetBundleBuilds.ToArray(), buildTarget, true);

            if (assetBundleManifest == null)
            {
                throw new InvalidOperationException("fail to dry run build - " + buildTarget);
            }

            var assetBundleNames = assetBundleManifest.GetAllAssetBundles();
            var allDependencies = new Dictionary<string, string[]>(assetBundleNames.Length, StringComparer.Ordinal);
            var cachedAssetBundleNames = new HashSet<string>(result.cachedAssetBundles.Select(c => c.assetBundleName));

            foreach(var assetBundleName in assetBundleNames)
            {
                allDependencies[assetBundleName] = assetBundleManifest.GetAllDependencies(assetBundleName);
            }

            foreach (var assetBundleName in allDependencies.Keys)
            {
                var dependencies = allDependencies[assetBundleName];

                //캐시되지 않은 에셋번들이라면
                if (!cachedAssetBundleNames.Contains(assetBundleName))
                {
                    //종속된 에셋번들들도 캐시에서 지워준다
                    foreach (var dependentAssetBundleName in dependencies)
                    {
                        if (cachedAssetBundleNames.Remove(dependentAssetBundleName))
                        {
                            Logger.Log("except cached asset bundle - " + dependentAssetBundleName);
                        }
                    }
                }
                else
                {
                    //종속된 에셋번들이 캐시되지 않았으면 캐시에서 제외한다
                    foreach (var dependentAssetBundleName in dependencies)
                    {
                        if (cachedAssetBundleNames.Contains(dependentAssetBundleName))
                            continue;

                        cachedAssetBundleNames.Remove(assetBundleName);
                        Logger.Log("except cached asset bundle - " + assetBundleName);
                        break;
                    }

                    //다른 에셋번들에서 종속중인데 에셋번들이 캐시에 없다면 제외한다
                    foreach (var pair in allDependencies)
                    {
                        if (pair.Key == assetBundleName)
                            continue;

                        if (!pair.Value.Contains(assetBundleName))
                            continue;

                        if (cachedAssetBundleNames.Contains(pair.Key))
                            continue;

                        Logger.Log("except cached asset bundle - " + assetBundleName);
                        cachedAssetBundleNames.Remove(assetBundleName);
                        break;
                    }
                }
            }

            result.cachedBuildInfo = cachedBuildInfo;
            result.cachedAssetBundles.RemoveAll(c => !cachedAssetBundleNames.Contains(c.assetBundleName));

            return result;
        }

        public static string GetCacheRootPath(BuildTarget buildTarget)
        {
            return Path.Combine(CachePath, buildTarget.ToString());
        }

        public static string GetCacheAssetPath(BuildTarget buildTarget)
        {
            return Path.Combine(CachePath, buildTarget.ToString(), buildTarget.ToString());
        }

        public static void SaveCache(BuildTarget buildTarget, string outputPath, BuildInfo buildInfo)
        {
            Logger.Log("save cache - " + GetCacheRootPath(buildTarget));

            string cachedPath = Path.Combine(GetCacheRootPath(buildTarget), buildTarget.ToString());

            EditorUtil.DeleteDirectory(cachedPath);
            EditorUtil.CreateDirectory(GetCacheRootPath(buildTarget));
            FileUtil.CopyFileOrDirectory(outputPath, cachedPath);

            SaveBuildInfo(buildTarget, buildInfo);
        }

        public static void ClearBuildCache(BuildTarget buildTarget)
        {
            Logger.Log("clear cache - " + GetCacheRootPath(buildTarget));
            EditorUtil.DeleteDirectory(GetCacheRootPath(buildTarget));
        }

        private static string GetBuildInfoPath(BuildTarget buildTarget)
        {
            return Path.Combine(GetCacheRootPath(buildTarget), "build_info.json");
        }

        public static void SaveBuildInfo(BuildTarget buildTarget, BuildInfo info)
        {
            var filePath = GetBuildInfoPath(buildTarget);
            EditorUtil.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, JsonUtility.ToJson(info, true));
        }

        private static BuildInfo LoadBuildInfo(BuildTarget buildTarget)
        {
            var filePath = GetBuildInfoPath(buildTarget);

            if (!File.Exists(filePath))
            {
                return null;
            }

            return JsonUtility.FromJson<BuildInfo>(File.ReadAllText(filePath));
        }
    }
}