using System.Collections.Generic;
using Alchemy.Hierarchy;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    public sealed class HierarchySeparatorDrawer : HierarchyDrawer
    {
        static Color SeparatorColor => new(0.5f, 0.5f, 0.5f);

#if UNITY_6000_4_OR_NEWER
        static readonly Dictionary<EntityId, bool> HasSeparatorCached = new();
#else
        static readonly Dictionary<int, bool> HasSeparatorCached = new();
#endif

        // Instance ids are reused as rows are destroyed. Adding or removing the component on an
        // existing object does not raise hierarchyChanged, so structural object changes and undo/redo drop it too.
        static HierarchySeparatorDrawer()
        {
            EditorApplication.hierarchyChanged += ClearSeparatorCache;
            Undo.undoRedoPerformed += ClearSeparatorCache;
            ObjectChangeEvents.changesPublished += OnObjectChanges;
        }

        static void OnObjectChanges(ref ObjectChangeEventStream stream)
        {
            if (!HierarchyCacheRefresh.ContainsStructuralChange(ref stream)) return;
            ClearSeparatorCache();
        }

        internal static void ClearSeparatorCache()
        {
            if (HasSeparatorCached.Count == 0) return;
            HasSeparatorCached.Clear();
            // The callback is deferred, so a repaint may already have drawn a stale row.
            HierarchyCacheRefresh.RequestRepaint();
        }

#if UNITY_6000_4_OR_NEWER
        public override void OnGUI(EntityId instanceID, Rect selectionRect)
#else
        public override void OnGUI(int instanceID, Rect selectionRect)
#endif
        {
            if (!TryGetSeparator(instanceID, out _)) return;

            DrawBackground(instanceID, selectionRect);

            var lineRect = selectionRect.AddY(selectionRect.height * 0.5f).AddXMax(14f).SetHeight(1f);
            EditorGUI.DrawRect(lineRect, SeparatorColor);
        }

#if UNITY_6000_4_OR_NEWER
        internal static bool TryGetSeparator(EntityId instanceID, out GameObject gameObject)
#else
        internal static bool TryGetSeparator(int instanceID, out GameObject gameObject)
#endif
        {
            if (HasSeparatorCached.TryGetValue(instanceID, out var hasSeparator))
            {
                if (!hasSeparator)
                {
                    gameObject = null;
                    return false;
                }

                gameObject = ResolveGameObject(instanceID);
                return gameObject != null;
            }

            gameObject = ResolveGameObject(instanceID);
            if (gameObject == null) return false;

            hasSeparator = gameObject.TryGetComponent<HierarchySeparator>(out _);
            HasSeparatorCached[instanceID] = hasSeparator;
            if (hasSeparator) return true;

            gameObject = null;
            return false;
        }

#if UNITY_6000_4_OR_NEWER
        static GameObject ResolveGameObject(EntityId instanceID)
#else
        static GameObject ResolveGameObject(int instanceID)
#endif
        {
#if UNITY_6000_4_OR_NEWER
            return EditorUtility.EntityIdToObject(instanceID) as GameObject;
#else
            return EditorUtility.InstanceIDToObject(instanceID) as GameObject;
#endif
        }
    }
}
