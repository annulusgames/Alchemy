using Alchemy.Hierarchy;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    internal static class HierarchyObjectCreationMenu
    {
        [MenuItem("GameObject/Alchemy/Header", false)]
        static void CreateHeader(MenuCommand menuCommand)
        {
            var obj = new GameObject("Header");
            obj.AddComponent<HierarchyHeader>();
            GameObjectUtility.SetParentAndAlign(obj, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(obj, "Create " + obj.name);
            Selection.activeObject = obj;
        }

        [MenuItem("GameObject/Alchemy/Separator", false)]
        static void CreateSeparator(MenuCommand menuCommand)
        {
            var obj = new GameObject("Separator");
            obj.AddComponent<HierarchySeparator>();
            GameObjectUtility.SetParentAndAlign(obj, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(obj, "Create " + obj.name);
            Selection.activeObject = obj;
        }
    }
}
