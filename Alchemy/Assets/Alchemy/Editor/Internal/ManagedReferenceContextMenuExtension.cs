#if UNITY_2021_3_OR_NEWER
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor {
    public static class ManagedReferenceContextMenuExtension {

        private const string CLIPBOARD_KEY = "AlchemyExtensions.CopyAndPasteManagedProperty";
        private const string COPIED_PROPERTY_PATH_KEY = "AlchemyExtensions.CopiedManagedPropertyPath";

        private static readonly GUIContent CopyProperty = new("Copy Property");
        private static readonly GUIContent PasteProperty = new("Paste Property");

        [InitializeOnLoadMethod]
        private static void Initialize() {
            EditorApplication.contextualPropertyMenu += OnContextualPropertyMenu;
        }

        private static void OnContextualPropertyMenu(GenericMenu menu, SerializedProperty property) {
            if (property.propertyType == SerializedPropertyType.ManagedReference) {

                SerializedProperty clonedProperty = property.Copy();
                menu.AddItem(CopyProperty, false, Copy, clonedProperty);

                string copiedPropertyPath = SessionState.GetString(COPIED_PROPERTY_PATH_KEY, string.Empty);
                if (!string.IsNullOrEmpty(copiedPropertyPath)) {
                    menu.AddItem(PasteProperty, false, Paste, clonedProperty);
                }
                else {
                    menu.AddDisabledItem(PasteProperty);
                }

                menu.AddSeparator("");
            }
        }

        private static void Copy(object serializedPropertyObject) {
            SerializedProperty property = (SerializedProperty)serializedPropertyObject;
            string json = JsonUtility.ToJson(property.managedReferenceValue);
            SessionState.SetString(COPIED_PROPERTY_PATH_KEY, property.propertyPath);
            SessionState.SetString(CLIPBOARD_KEY, json);
        }

        private static void Paste(object serializedPropertyObject) {
            SerializedProperty property = (SerializedProperty)serializedPropertyObject;
            string json = SessionState.GetString(CLIPBOARD_KEY, string.Empty);
            if (string.IsNullOrEmpty(json)) {
                return;
            }

            Undo.RecordObject(property.serializedObject.targetObject, PasteProperty.text);
            JsonUtility.FromJsonOverwrite(json, property.managedReferenceValue);
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif