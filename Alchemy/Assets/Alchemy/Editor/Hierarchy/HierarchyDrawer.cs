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
}
