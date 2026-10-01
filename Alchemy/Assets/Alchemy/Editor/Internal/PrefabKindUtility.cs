using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Alchemy.Inspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Alchemy.Editor
{
    internal static class PrefabKindUtility
    {
        static Dictionary<UnityEngine.Object, PrefabKind> cache;
        static bool releaseScheduled;

        // UnityEngine.Object equality treats destroyed objects as null, so key by reference.
        sealed class TargetReferenceComparer : IEqualityComparer<UnityEngine.Object>
        {
            public static readonly TargetReferenceComparer Instance = new();

            public bool Equals(UnityEngine.Object x, UnityEngine.Object y) => ReferenceEquals(x, y);

            public int GetHashCode(UnityEngine.Object obj) => RuntimeHelpers.GetHashCode(obj);
        }

        [InitializeOnLoadMethod]
        static void RegisterPrefabKindCacheInvalidation()
        {
            EditorApplication.hierarchyChanged += InvalidatePrefabKindCache;
            EditorApplication.projectChanged += InvalidatePrefabKindCache;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Undo.undoRedoPerformed += InvalidatePrefabKindCache;
            PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdated;
#if UNITY_2022_2_OR_NEWER
            PrefabUtility.prefabInstanceUnpacked += OnPrefabInstanceUnpacked;
#endif
        }

        // Drop cached kinds before deferred ShowIn/HideIn/EnableIn/DisableIn updates read them.
        internal static void InvalidatePrefabKindCache() => cache?.Clear();

        static void ScheduleCacheRelease()
        {
            if (releaseScheduled) return;
            releaseScheduled = true;
            EditorApplication.delayCall += ReleasePrefabKindCache;
        }

        static void ReleasePrefabKindCache()
        {
            releaseScheduled = false;
            cache?.Clear();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state) => InvalidatePrefabKindCache();

        static void OnPrefabInstanceUpdated(GameObject instance) => InvalidatePrefabKindCache();

#if UNITY_2022_2_OR_NEWER
        static void OnPrefabInstanceUnpacked(GameObject instance, PrefabUnpackMode unpackMode) => InvalidatePrefabKindCache();
#endif

        public static PrefabKind GetPrefabKind(UnityEngine.Object target)
        {
            if (target == null) return PrefabKind.None;
            if (cache != null && cache.TryGetValue(target, out var cached)) return cached;

            var kind = ComputePrefabKind(target);
            cache ??= new Dictionary<UnityEngine.Object, PrefabKind>(TargetReferenceComparer.Instance);
            cache[target] = kind;
            ScheduleCacheRelease();
            return kind;
        }

        static PrefabKind ComputePrefabKind(UnityEngine.Object target)
        {
            if (target == null) return PrefabKind.None;

            var gameObject = GetGameObject(target);
            if (gameObject == null) return PrefabKind.None;

            if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                if (TryGetPreviewPrefabAssetPath(gameObject, out var previewAssetPath))
                {
                    return IsNestedInstanceInPreview(gameObject)
                        ? PrefabKind.InstanceInPrefab
                        : GetAssetKindFromPath(previewAssetPath);
                }

                if (IsPersistentVariantAsset(gameObject))
                {
                    return PrefabKind.Variant;
                }

                return IsInstanceInPrefab(gameObject)
                    ? PrefabKind.InstanceInPrefab
                    : PrefabKind.InstanceInScene;
            }

            if (EditorUtility.IsPersistent(gameObject) || PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                return GetAssetKind(gameObject);
            }

            if (TryGetPreviewPrefabAssetPath(gameObject, out var assetPath))
            {
                return GetAssetKindFromPath(assetPath);
            }

            if (IsPrefabPreviewObject(gameObject))
            {
                return GetPreviewAssetKind(gameObject);
            }

            return PrefabKind.NonPrefabInstance;
        }

        static bool IsPersistentVariantAsset(GameObject gameObject)
        {
            if (!PrefabUtility.IsPartOfVariantPrefab(gameObject))
            {
                return false;
            }

            if (!EditorUtility.IsPersistent(gameObject) && !PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                return false;
            }

            var nearest = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
            return nearest == null || nearest == gameObject.transform.root.gameObject;
        }

        static bool IsNestedInstanceInPreview(GameObject gameObject)
        {
            var stage = PrefabStageUtility.GetPrefabStage(gameObject);
            var root = stage != null ? stage.prefabContentsRoot : gameObject.transform.root.gameObject;
            var nearest = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
            // Asset paths also match when a variant contains another instance of its base.
            // The contents root, not asset identity, distinguishes those two instances.
            return nearest != null && nearest != root;
        }

        static bool IsInstanceInPrefab(GameObject gameObject)
        {
            var nearest = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
            var outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            if (nearest != null && outermost != null && nearest != outermost)
            {
                return true;
            }

            if (EditorUtility.IsPersistent(gameObject) || PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                return true;
            }

            return IsPrefabPreviewObject(gameObject);
        }

        static bool IsPrefabPreviewObject(GameObject gameObject)
        {
            if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
            {
                return true;
            }

            return gameObject.scene.IsValid() && EditorSceneManager.IsPreviewScene(gameObject.scene);
        }

        static bool TryGetPreviewPrefabAssetPath(GameObject gameObject, out string assetPath)
        {
            assetPath = null;

            var stage = PrefabStageUtility.GetPrefabStage(gameObject);
            if (stage != null && !string.IsNullOrEmpty(stage.assetPath))
            {
                assetPath = stage.assetPath;
                return true;
            }

            if (!gameObject.scene.IsValid() || !EditorSceneManager.IsPreviewScene(gameObject.scene))
            {
                return false;
            }

            var scenePath = gameObject.scene.path;
            if (string.IsNullOrEmpty(scenePath))
            {
                return false;
            }

            if (AssetDatabase.LoadMainAssetAtPath(scenePath) != null)
            {
                assetPath = scenePath;
                return true;
            }

            return false;
        }

        static PrefabKind GetPreviewAssetKind(GameObject gameObject)
        {
            if (PrefabUtility.IsPartOfVariantPrefab(gameObject)) return PrefabKind.Variant;
            if (PrefabUtility.IsPartOfRegularPrefab(gameObject)) return PrefabKind.Regular;
            return PrefabKind.None;
        }

        static PrefabKind GetAssetKindFromPath(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            return asset == null ? PrefabKind.None : GetAssetKind(asset);
        }

        static PrefabKind GetAssetKind(UnityEngine.Object target)
        {
            return PrefabUtility.GetPrefabAssetType(target) switch
            {
                PrefabAssetType.Regular => PrefabKind.Regular,
                PrefabAssetType.Variant => PrefabKind.Variant,
                _ => PrefabKind.None,
            };
        }

        static GameObject GetGameObject(UnityEngine.Object target)
        {
            switch (target)
            {
                case GameObject gameObject:
                    return gameObject;
                case Component component:
                    return component.gameObject;
                default:
                    return null;
            }
        }
    }
}
