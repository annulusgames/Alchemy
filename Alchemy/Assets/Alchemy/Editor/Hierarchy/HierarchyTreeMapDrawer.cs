using System.Collections.Generic;
using Alchemy.Hierarchy;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    public class HierarchyTreeMapDrawer : HierarchyDrawer
    {
        private static readonly Dictionary<string, Texture2D> TextureCached = new();

#if UNITY_6000_4_OR_NEWER
        private static readonly Dictionary<EntityId, bool> LastSiblingCached = new();
#else
        private static readonly Dictionary<int, bool> LastSiblingCached = new();
#endif

        // Sibling order changes with the hierarchy, so one subscription can drop the whole cache.
        static HierarchyTreeMapDrawer()
        {
            EditorApplication.hierarchyChanged += ClearLastSiblingCache;
        }

        internal static void ClearLastSiblingCache()
        {
            if (LastSiblingCached.Count == 0) return;
            LastSiblingCached.Clear();
            // hierarchyChanged is deferred, so a repaint may already have drawn stale lines.
            EditorApplication.RepaintHierarchyWindow();
        }

        public static Texture2D TreeMapCurrent
        {
            get
            {
                if (TextureCached.TryGetValue(nameof(TreeMapCurrent), out var tex)) return tex;
                tex = AssetHelper.FindAssetWithPath<Texture2D>("tree_map_current.png", "Editor/Hierarchy/Textures");
                TextureCached[nameof(TreeMapCurrent)] = tex;
                return tex;
            }
        }

        public static Texture2D TreeMapLast
        {
            get
            {
                if (TextureCached.TryGetValue(nameof(TreeMapLast), out var tex)) return tex;
                tex = AssetHelper.FindAssetWithPath<Texture2D>("tree_map_last.png", "Editor/Hierarchy/Textures");
                TextureCached[nameof(TreeMapLast)] = tex;
                return tex;
            }
        }

        public static Texture2D TreeMapLevel
        {
            get
            {
                if (TextureCached.TryGetValue(nameof(TreeMapLevel), out var tex)) return tex;
                tex = AssetHelper.FindAssetWithPath<Texture2D>("tree_map_level.png", "Editor/Hierarchy/Textures");
                TextureCached[nameof(TreeMapLevel)] = tex;
                return tex;
            }
        }

        public static Texture2D TreeMapLine
        {
            get
            {
                if (TextureCached.TryGetValue(nameof(TreeMapLine), out var tex)) return tex;
                tex = AssetHelper.FindAssetWithPath<Texture2D>("tree_map_line.png", "Editor/Hierarchy/Textures");
                TextureCached[nameof(TreeMapLine)] = tex;
                return tex;
            }
        }

        internal static bool IsLastSibling(Transform transform)
        {
#if UNITY_6000_4_OR_NEWER
            var id = transform.GetEntityId();
#else
            var id = transform.GetInstanceID();
#endif
            if (LastSiblingCached.TryGetValue(id, out var isLast)) return isLast;

            var parent = transform.parent;
            if (parent == null) isLast = transform.GetSiblingIndex() == transform.gameObject.scene.rootCount - 1;
            else isLast = transform.GetSiblingIndex() == parent.childCount - 1;

            LastSiblingCached[id] = isLast;
            return isLast;
        }

#if UNITY_6000_4_OR_NEWER
        public override void OnGUI(EntityId instanceID, Rect selectionRect)
#else
        public override void OnGUI(int instanceID, Rect selectionRect)
#endif
        {
            var settings = AlchemySettings.GetOrCreateSettings();
            if (!settings.ShowTreeMap) return;

#if UNITY_6000_4_OR_NEWER
            var gameObject = EditorUtility.EntityIdToObject(instanceID) as GameObject;
#else
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
#endif
            if (gameObject == null) return;

            var tempColor = GUI.color;

            selectionRect.width = 14;
            selectionRect.height = 16;

            int childCount = gameObject.transform.childCount;
            int level = Mathf.RoundToInt(selectionRect.x / 14f);
            var t = gameObject.transform;
            Transform parent = null;

            for (int i = 0, j = level - 1; j >= 0; i++, j--)
            {
                selectionRect.x = 14 * j;
                if (i == 0)
                {
                    if (childCount == 0)
                    {
                        GUI.color = settings.TreeMapColor;
                        GUI.DrawTexture(selectionRect, TreeMapLine);
                    }

                    t = gameObject.transform;
                }
                else if (i == 1)
                {
                    GUI.color = settings.TreeMapColor;
                    if (IsLastSibling(t))
                    {
                        GUI.DrawTexture(selectionRect, TreeMapLast);
                    }
                    else
                    {
                        GUI.DrawTexture(selectionRect, TreeMapCurrent);
                    }

                    t = parent;
                }
                else
                {
                    if (!IsLastSibling(t)) GUI.DrawTexture(selectionRect, TreeMapLevel);

                    t = parent;
                }

                if (t != null) parent = t.parent;
                else break;
            }

            GUI.color = tempColor;
        }
    }
}
