using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{
    public static class EditorUtil
    {
        public static string[] GetAssetPathsByDirectory(string directoryPath, bool includeFolder = true, bool includeFile = true)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                throw new ArgumentException("directoryPath");
            }

            if (!includeFolder && !includeFile)
            {
                return new string[0];
            }

            List<string> paths = new List<string>();

            if (includeFolder)
            {
                paths.AddRange(Directory.GetDirectories(directoryPath));
            }

            if (includeFile)
            {
                paths.AddRange(Directory.GetFiles(directoryPath).Where(path => !path.EndsWith(".meta")));
            }

            return paths.Select(path => path.Replace("\\", "/")).ToArray();
        }

        public static void Work(string path, Action<string> action)
        {
            action(path);

            if (Directory.Exists(path))
            {
                foreach (var directoryPath in Directory.GetDirectories(path))
                {
                    Work(directoryPath, action);
                }

                foreach (var filePath in Directory.GetFiles(path))
                {
                    action(filePath);
                }
            }
        }

        public static void RemoveEmptyDirectories(string targetPath)
        {
            Work(targetPath, path =>
            {
                if (!Directory.Exists(path))
                    return;

                if (Directory.GetDirectories(path).Length == 0 && Directory.GetFiles(path).Length == 0)
                {
                    Logger.Log("remove empty directory - " + path);
                    Directory.Delete(path);
                }
            });
        }

        public static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                Logger.Log("delete file - " + filePath);
                File.Delete(filePath);
            }
        }

        public static void DeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            Logger.Log("delete directory - " + path);
            Directory.Delete(path, true);
        }

        public static void CreateDirectory(string path)
        {
            if (File.Exists(path))
            {
                return;
            }

            if (Directory.Exists(path))
            {
                return;
            }

            Logger.Log("create directory - " + path);
            Directory.CreateDirectory(path);
        }

        public static T GetOrCreateScriptableFile<T>(string assetPath) where T : ScriptableObject
        {
            var scriptableObject = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (scriptableObject == null)
            {
                if (!Directory.Exists(Path.GetDirectoryName(assetPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                }

                scriptableObject = ScriptableObject.CreateInstance<T>();

                AssetDatabase.CreateAsset(scriptableObject, assetPath);
                EditorUtility.SetDirty(scriptableObject);
                AssetDatabase.Refresh();
            }

            return scriptableObject;
        }

        public static void CollectAssetPaths(List<string> assetPaths, string assetPath, Predicate<string> predicate = null)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                var assetGuids = AssetDatabase.FindAssets(string.Empty, new string[] { assetPath }).Distinct();

                foreach (var assetGuid in assetGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(assetGuid);

                    if (AssetDatabase.IsValidFolder(path))
                        continue;

                    if (predicate != null && !predicate(path))
                        continue;

                    assetPaths.Add(path);
                }
            }
            else
            {
                if (predicate != null && !predicate(assetPath))
                    return;

                assetPaths.Add(assetPath);
            }
        }

        public static string GetDirectoryPath(string path)
        {
            return Path.GetDirectoryName(path).Replace("\\", "/");
        }

        public static AssetBundleManifest BuildAssetBundle(string outputPath, AssetBundleBuild[] assetBundleBuilds, BuildTarget buildTarget, bool dryRun)
        {
            var option = BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.DisableLoadAssetByFileName;

            if (dryRun)
            {
                option |= BuildAssetBundleOptions.DryRunBuild;
            }

            CreateDirectory(outputPath);
            return BuildPipeline.BuildAssetBundles(outputPath, assetBundleBuilds, option, buildTarget);
        }

        public static bool IsScript(string path)
        {
            return path.EndsWith(".cs");
        }

        public static string GetProjectPath()
        {
            return Application.dataPath.Replace("/Assets", string.Empty);
        }


        public static bool DisplayDialogOkCancel(string message)
        {
            return EditorUtility.DisplayDialog("TAssetBundle", message, "Ok", "Cancel");
        }
    }



    public static class EditorGUIUtil
    {
        public static void LabelFieldColor(string label, Color color)
        {
            var originColor = GUI.contentColor;
            GUI.contentColor = color;
            EditorGUILayout.LabelField(label);
            GUI.contentColor = originColor;
        }

        public static Color BeginColor(Color color)
        {
            var originColor = GUI.color;

            if (color != default && originColor != color)
            {
                GUI.color = color;
            }

            return originColor;
        }

        public static void EndColor(Color color)
        {
            GUI.color = color;
        }

        public static Color BeginContentColor(Color color)
        {
            var originColor = GUI.contentColor;

            if (color != default && originColor != color)
            {
                GUI.contentColor = color;
            }

            return originColor;
        }

        public static void EndContentColor(Color color)
        {
            GUI.contentColor = color;
        }

        public static bool FoldoutHeaderGroup(bool foldout, 
            string label,
            Color color = default,
            Action inGroup = null)
        {
            color = BeginColor(color);
            var newFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, label);
            EndColor(color);

            inGroup?.Invoke();

            EditorGUILayout.EndFoldoutHeaderGroup();

            return newFoldout;
        }

        public static bool FoldoutHeaderGroup(string foldoutKey, string label, 
            Color color = default, 
            bool defaultValue = true, 
            Action inGroup = null)
        {
            var foldout = EditorPrefs.GetBool(foldoutKey, defaultValue);
            var newFoldout = FoldoutHeaderGroup(foldout, label, color, inGroup);

            if(foldout != newFoldout)
            {
                EditorPrefs.SetBool(foldoutKey, newFoldout);
            }

            return newFoldout;
        }

    }

}