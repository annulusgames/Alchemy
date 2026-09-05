using System;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    internal static class ChildObjectsOnlyValidation
    {
        public static bool IsSupportedReferenceType(Type type)
        {
            if (type == null) return false;
            if (type == typeof(UnityEngine.Object)) return true;
            if (type == typeof(GameObject)) return true;
            return typeof(Component).IsAssignableFrom(type);
        }

        public static bool IsSupportedProperty(SerializedProperty property)
        {
            if (property == null) return false;

            try
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    var fieldInfo = property.GetFieldInfo();
                    if (fieldInfo == null) return false;
                    return IsSupportedReferenceType(property.GetPropertyType());
                }

                if (property.propertyType == SerializedPropertyType.String) return false;
                if (!property.isArray) return false;

                var arrayFieldInfo = property.GetFieldInfo();
                if (arrayFieldInfo == null) return false;

                var elementType = property.GetPropertyType(true);
                return IsSupportedReferenceType(elementType);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static Transform GetOwnerTransform(UnityEngine.Object target)
        {
            if (target is Component component && component != null)
            {
                return component.transform;
            }

            if (target is GameObject gameObject && gameObject != null)
            {
                return gameObject.transform;
            }

            return null;
        }

        public static Transform GetOwnerTransform(SerializedObject serializedObject)
        {
            try
            {
                return GetOwnerTransform(serializedObject?.targetObject);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string DefaultErrorMessage(string displayName, bool includeSelf)
        {
            var name = ObjectNames.NicifyVariableName(displayName);
            return includeSelf
                ? name + " must be this GameObject, a descendant, or a component on those objects."
                : name + " must be a descendant GameObject or a component on a descendant.";
        }

        public static bool IsValid(UnityEngine.Object value, Transform owner, bool includeSelf)
        {
            if (value == null) return true;
            if (owner == null) return false;
            if (!IsSceneHierarchyObject(value)) return false;

            var gameObject = GetGameObject(value);
            if (gameObject == null) return false;

            var transform = gameObject.transform;
            if (!transform.IsChildOf(owner)) return false;
            if (!includeSelf && transform == owner) return false;
            return true;
        }

        public static bool IsSerializedPropertyValid(SerializedProperty property, bool includeSelf)
        {
            if (!TryAccessProperty(property, out var serializedObject, out var path))
            {
                return false;
            }

            UnityEngine.Object[] targets;
            bool hasPendingEdits;
            try
            {
                targets = serializedObject.targetObjects;
                hasPendingEdits = serializedObject.hasModifiedProperties;
            }
            catch (Exception)
            {
                return false;
            }

            if (targets == null || targets.Length == 0)
            {
                return false;
            }

            // Pending inspector edits live on the shared SerializedObject. Do not Update/Apply
            // them; the shared property still has the in-progress values for the edited target.
            if (hasPendingEdits)
            {
                if (!IsPropertyValid(property, GetOwnerTransform(serializedObject), includeSelf))
                {
                    return false;
                }
            }

            // Multi-object shared properties use the minimum arraySize, so trailing values on
            // longer targets are invisible unless each target is read through an isolated object.
            var startIndex = hasPendingEdits ? 1 : 0;
            for (var i = startIndex; i < targets.Length; i++)
            {
                var target = targets[i];
                if (target == null) return false;

                var isolated = new SerializedObject(target);
                try
                {
                    var isolatedProperty = isolated.FindProperty(path);
                    if (isolatedProperty == null) return false;
                    if (!IsPropertyValid(isolatedProperty, GetOwnerTransform(target), includeSelf))
                    {
                        return false;
                    }
                }
                finally
                {
                    isolated.Dispose();
                }
            }

            return true;
        }

        public static bool IsPropertyValid(SerializedProperty property, Transform owner, bool includeSelf)
        {
            if (property == null) return false;

            try
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    return IsValid(property.objectReferenceValue, owner, includeSelf);
                }

                if (property.isArray && property.propertyType != SerializedPropertyType.String)
                {
                    for (var i = 0; i < property.arraySize; i++)
                    {
                        var element = property.GetArrayElementAtIndex(i);
                        if (element.propertyType != SerializedPropertyType.ObjectReference)
                        {
                            return false;
                        }

                        if (!IsValid(element.objectReferenceValue, owner, includeSelf))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        public static bool TryAccessProperty(
            SerializedProperty property,
            out SerializedObject serializedObject,
            out string path)
        {
            serializedObject = null;
            path = null;
            if (property == null) return false;

            try
            {
                serializedObject = property.serializedObject;
                if (serializedObject == null) return false;
                _ = serializedObject.targetObject;
                path = property.propertyPath;
                if (string.IsNullOrEmpty(path)) return false;

                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    _ = property.objectReferenceValue;
                }
                else if (property.isArray)
                {
                    _ = property.arraySize;
                }

                return true;
            }
            catch (Exception)
            {
                serializedObject = null;
                path = null;
                return false;
            }
        }

        public static bool IsSceneHierarchyObject(UnityEngine.Object value)
        {
            var gameObject = GetGameObject(value);
            if (gameObject == null) return false;
            if (EditorUtility.IsPersistent(value) || EditorUtility.IsPersistent(gameObject))
            {
                return false;
            }

            return gameObject.scene.IsValid();
        }

        public static GameObject GetGameObject(UnityEngine.Object value)
        {
            switch (value)
            {
                case GameObject gameObject:
                    return gameObject;
                case Component component:
                    return component.gameObject;
                default:
                    return null;
            }
        }
    }
}
