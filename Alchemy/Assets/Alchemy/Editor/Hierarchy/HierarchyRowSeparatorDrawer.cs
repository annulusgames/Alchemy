using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    public class HierarchyRowSeparatorDrawer : HierarchyDrawer
    {
#if UNITY_6000_4_OR_NEWER
        public override void OnGUI(EntityId instanceID, Rect selectionRect)
#else
        public override void OnGUI(int instanceID, Rect selectionRect)
#endif
        {
            var settings = AlchemySettings.GetOrCreateSettings();
            if (!settings.ShowSeparator) return;
            var rect = new Rect { y = selectionRect.y, width = selectionRect.width + selectionRect.x, height = 1, x = 0 };

            EditorGUI.DrawRect(rect, settings.SeparatorColor);

            if (!settings.ShowRowShading) return;
            selectionRect.width += selectionRect.x;
            selectionRect.x = 0;
            selectionRect.height -= 1;
            selectionRect.y += 1;
            EditorGUI.DrawRect(selectionRect, Mathf.FloorToInt((selectionRect.y - 4) / 16 % 2) == 0 ? settings.EvenRowColor : settings.OddRowColor);
        }
    }
}
