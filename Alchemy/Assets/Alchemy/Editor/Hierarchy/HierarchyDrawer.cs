using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    /// <summary>
    /// Base class for adding custom drawing processing to hierarchy items.
    /// </summary>
    public abstract class HierarchyDrawer
    {
#if UNITY_6000_4_OR_NEWER
        public abstract void OnGUI(EntityId instanceID, Rect selectionRect);
#else
        public abstract void OnGUI(int instanceID, Rect selectionRect);
#endif

        protected static Rect GetBackgroundRect(Rect selectionRect)
        {
            return selectionRect.AddXMax(20f);
        }

#if UNITY_6000_4_OR_NEWER
        protected static void DrawBackground(EntityId instanceID, Rect selectionRect)
#else
        protected static void DrawBackground(int instanceID, Rect selectionRect)
#endif
        {
            var backgroundRect = GetBackgroundRect(selectionRect);

            Color backgroundColor;
            var e = Event.current;
            var isHover = backgroundRect.Contains(e.mousePosition);

            if (Selection.Contains(instanceID))
            {
                backgroundColor = EditorColors.HighlightBackground;
            }
            else if (isHover)
            {
                backgroundColor = EditorColors.HighlightBackgroundInactive;
            }
            else
            {
                backgroundColor = EditorColors.WindowBackground;
            }

            EditorGUI.DrawRect(backgroundRect, backgroundColor);
        }
    }

    // Header and separator caches drop on the same notifications, so one Hierarchy repaint is queued per tick.
    internal static class HierarchyCacheRefresh
    {
        static bool repaintQueued;

        internal static void RequestRepaint()
        {
            if (repaintQueued) return;
            repaintQueued = true;
            EditorApplication.delayCall += FlushRepaint;
        }

        static void FlushRepaint()
        {
            repaintQueued = false;
            EditorApplication.RepaintHierarchyWindow();
        }

        // Property edits and asset events do not add or remove HierarchyHeader / HierarchySeparator.
        internal static bool ContainsStructuralChange(ref ObjectChangeEventStream stream)
        {
            if (!stream.isCreated) return false;

            for (var i = 0; i < stream.length; i++)
            {
                if (IsStructuralChange(stream.GetEventType(i))) return true;
            }

            return false;
        }

        internal static bool IsStructuralChange(ObjectChangeKind kind)
        {
            switch (kind)
            {
                case ObjectChangeKind.ChangeScene:
                case ObjectChangeKind.CreateGameObjectHierarchy:
                case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                case ObjectChangeKind.ChangeGameObjectStructure:
                case ObjectChangeKind.ChangeGameObjectParent:
                case ObjectChangeKind.DestroyGameObjectHierarchy:
                case ObjectChangeKind.UpdatePrefabInstances:
                    return true;
                default:
                    return false;
            }
        }
    }
}
