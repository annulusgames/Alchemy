using System.Linq;
using Alchemy.Hierarchy;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    public sealed class HierarchyToggleDrawer : HierarchyDrawer
    {
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
            if (gameObject.TryGetComponent<HierarchyObject>(out _)) return;

            var settings = AlchemySettings.GetOrCreateSettings();

            if (settings.ShowHierarchyToggles)
            {
                var rect = selectionRect;
                rect.x = rect.xMax - 2.7f;
                rect.width = 16f;

                if (IsNewPrefabWorkflow() && IsPrefab(gameObject))
                    rect.x -= 16.7f;

                var active = GUI.Toggle(rect, gameObject.activeSelf, string.Empty);
                if (active != gameObject.activeSelf)
                {
                    Undo.RecordObject(gameObject, $"{(active ? "Activate" : "Deactivate")} GameObject '{gameObject.name}'");
                    gameObject.SetActive(active);
                    EditorUtility.SetDirty(gameObject);
                }
            }

            if (settings.ShowComponentIcons)
            {
                var rect = selectionRect;
                rect.x = rect.xMax - (settings.ShowHierarchyToggles ? 18.7f : 2.7f);

                if (IsNewPrefabWorkflow() && IsPrefab(gameObject))
                    rect.x -= 16.7f;

                rect.y += 1f;
                rect.width = 14f;
                rect.height = 14f;

                var components = gameObject
                    .GetComponents<Component>()
                    .AsEnumerable()
                    .Reverse();

                var existsScriptIcon = false;
                foreach (var component in components)
                {
                    var image = AssetPreview.GetMiniThumbnail(component);
                    if (image == null) continue;

                    if (image == EditorIcons.CsScriptIcon.image)
                    {
                        if (existsScriptIcon) continue;
                        existsScriptIcon = true;
                    }

                    DrawIcon(ref rect, image, IsEnabled(component) ? Color.white : new(1f, 1f, 1f, 0.5f));
                }
            }
        }

        static void DrawIcon(ref Rect rect, Texture image, Color color)
        {
            var defaultColor = GUI.color;
            GUI.color = color;

            GUI.DrawTexture(rect, image, ScaleMode.ScaleToFit);
            rect.x -= rect.width;

            GUI.color = defaultColor;
        }

        static bool IsEnabled(Component component)
        {
            var property = component.GetType().GetProperty("enabled", typeof(bool));
            return (bool)(property?.GetValue(component, null) ?? true);
        }
        private static bool IsPrefab(GameObject gameObj)
        {
            if (gameObj == null)
                return false;
            var prefabType = PrefabUtility.GetPrefabAssetType(gameObj);
            return prefabType != PrefabAssetType.NotAPrefab;
        }
        private static bool IsNewPrefabWorkflow()
        {
#if UNITY_2018_3_OR_NEWER
            return true;
#else
            return false;
#endif
        }
    }

}
