using System.IO;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    internal static class AssetHelper
    {
        public static T FindAssetWithPath<T>(string nameAsset, string relativePath, bool outsideScope = false) where T : Object
        {
            string path = outsideScope ? $"{relativePath}/{nameAsset}" : AssetInPackagePath(relativePath, nameAsset);
            var t = AssetDatabase.LoadAssetAtPath(path, typeof(T));
            if (t == null) Debug.LogError($"Couldn't load the {nameof(T)} at path :{path}");
            return t as T;
        }

        private static string AssetInPackagePath(string relativePath, string nameAsset) { return GetPathInCurrentEnvironent($"{relativePath}/{nameAsset}"); }

        private static string GetPathInCurrentEnvironent(string fullRelativePath)
        {
            var upmPath = $"Packages/com.annulusgames.alchemy/{fullRelativePath}";
            var normalPath = $"Assets/Alchemy/{fullRelativePath}";
            return !File.Exists(Path.GetFullPath(upmPath)) ? normalPath : upmPath;
        }
    }
}