using Alchemy.Hierarchy;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    public sealed class HierarchySeparatorDrawer : HierarchyDrawer
    {
        static Color SeparatorColor => new(0.5f, 0.5f, 0.5f);

#if UNITY_6000_4_OR_NEWER
        public override void OnGUI(EntityId instanceID, Rect selectionRect)
#else
        public override void OnGUI(int instanceID, Rect selectionRect)
#endif
        {
#if UNITY_6000_4_OR_NEWER
            var gameObject = EditorUtility.EntityIdToObject(instanceID) as GameObject;
#else
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
#endif
            if (gameObject == null) return;
            if (!gameObject.TryGetComponent<HierarchySeparator>(out _)) return;

            DrawBackground(instanceID, selectionRect);

            var lineRect = selectionRect.AddY(selectionRect.height * 0.5f).AddXMax(14f).SetHeight(1f);
            EditorGUI.DrawRect(lineRect, SeparatorColor);
        }
    }
}
