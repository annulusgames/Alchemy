using System.Collections.Generic;
using Alchemy.Hierarchy;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    public sealed class HierarchyHeaderDrawer : HierarchyDrawer
    {
        static Color HeaderColor => EditorGUIUtility.isProSkin ? new(0.45f, 0.45f, 0.45f, 0.5f) : new(0.55f, 0.55f, 0.55f, 0.5f);
        static GUIStyle labelStyle;

#if UNITY_6000_4_OR_NEWER
        static readonly Dictionary<EntityId, bool> HasHeaderCached = new();
#else
        static readonly Dictionary<int, bool> HasHeaderCached = new();
#endif

        // Instance ids are reused as rows are destroyed. Adding or removing the component on an
        // existing object does not raise hierarchyChanged, so structural object changes and undo/redo drop it too.
        static HierarchyHeaderDrawer()
        {
            EditorApplication.hierarchyChanged += ClearHeaderCache;
            Undo.undoRedoPerformed += ClearHeaderCache;
            ObjectChangeEvents.changesPublished += OnObjectChanges;
        }

        static void OnObjectChanges(ref ObjectChangeEventStream stream)
        {
            if (!HierarchyCacheRefresh.ContainsStructuralChange(ref stream)) return;
            ClearHeaderCache();
        }

        internal static void ClearHeaderCache()
        {
            if (HasHeaderCached.Count == 0) return;
            HasHeaderCached.Clear();
            // The callback is deferred, so a repaint may already have drawn a stale row.
            HierarchyCacheRefresh.RequestRepaint();
        }

#if UNITY_6000_4_OR_NEWER
        public override void OnGUI(EntityId instanceID, Rect selectionRect)
#else
        public override void OnGUI(int instanceID, Rect selectionRect)
#endif
        {
            if (!TryGetHeader(instanceID, out var gameObject)) return;

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                };
            }

            DrawBackground(instanceID, selectionRect);

            var headerRect = selectionRect.AddXMax(14f).AddYMax(-1f);
            EditorGUI.DrawRect(headerRect, HeaderColor);
            EditorGUI.LabelField(headerRect, gameObject.name, labelStyle);
        }

#if UNITY_6000_4_OR_NEWER
        internal static bool TryGetHeader(EntityId instanceID, out GameObject gameObject)
#else
        internal static bool TryGetHeader(int instanceID, out GameObject gameObject)
#endif
        {
            if (HasHeaderCached.TryGetValue(instanceID, out var hasHeader))
            {
                if (!hasHeader)
                {
                    gameObject = null;
                    return false;
                }

                gameObject = ResolveGameObject(instanceID);
                return gameObject != null;
            }

            gameObject = ResolveGameObject(instanceID);
            if (gameObject == null) return false;

            hasHeader = gameObject.TryGetComponent<HierarchyHeader>(out _);
            HasHeaderCached[instanceID] = hasHeader;
            if (hasHeader) return true;

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
