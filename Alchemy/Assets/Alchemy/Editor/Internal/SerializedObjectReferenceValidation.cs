using System;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    internal static class SerializedObjectReferenceValidation
    {
        public static bool IsSupportedProperty(SerializedProperty property, Func<Type, bool> isSupportedType)
        {
            if (property == null || isSupportedType == null) return false;

            try
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    var fieldInfo = property.GetFieldInfo();
                    if (fieldInfo == null) return false;
                    return isSupportedType(property.GetPropertyType());
                }

                if (property.propertyType == SerializedPropertyType.String) return false;
                if (!property.isArray) return false;

                var arrayFieldInfo = property.GetFieldInfo();
                if (arrayFieldInfo == null) return false;

                var elementType = property.GetPropertyType(true);
                return isSupportedType(elementType);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsSerializedPropertyValid(
            SerializedProperty property,
            Func<UnityEngine.Object, Func<UnityEngine.Object, bool>> createValidator)
        {
            if (createValidator == null) return false;
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

            // Do not Update/Apply: pending inspector edits must stay on the shared object.
            // A single target already exposes those edits, so validate it in place.
            // Mixed values and array tails need an isolated copy of each target.
            if (targets.Length == 1)
            {
                var target = targets[0];
                if (target == null) return false;
                return IsPropertyValid(property, createValidator(target));
            }

            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (target == null) return false;

                var isolated = new SerializedObject(target);
                try
                {
                    var isolatedProperty = isolated.FindProperty(path);
                    if (isolatedProperty == null) return false;
                    if (!IsTargetPropertyValid(
                            property,
                            isolatedProperty,
                            createValidator(target),
                            hasPendingEdits))
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

        public static bool IsPropertyValid(SerializedProperty property, Func<UnityEngine.Object, bool> isValid)
        {
            if (property == null || isValid == null) return false;

            try
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    return isValid(property.objectReferenceValue);
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

                        if (!isValid(element.objectReferenceValue))
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

        static bool IsTargetPropertyValid(
            SerializedProperty sharedProperty,
            SerializedProperty isolatedProperty,
            Func<UnityEngine.Object, bool> isValid,
            bool hasPendingEdits)
        {
            if (!hasPendingEdits)
            {
                return IsPropertyValid(isolatedProperty, isValid);
            }

            try
            {
                if (sharedProperty.propertyType == SerializedPropertyType.ObjectReference)
                {
                    var value = sharedProperty.hasMultipleDifferentValues
                        ? isolatedProperty.objectReferenceValue
                        : sharedProperty.objectReferenceValue;
                    return isValid(value);
                }

                if (sharedProperty.isArray && sharedProperty.propertyType != SerializedPropertyType.String)
                {
                    if (!sharedProperty.hasMultipleDifferentValues)
                    {
                        return IsPropertyValid(sharedProperty, isValid);
                    }

                    return IsPendingArrayValid(sharedProperty, isolatedProperty, isValid);
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        static bool IsPendingArrayValid(
            SerializedProperty sharedProperty,
            SerializedProperty isolatedProperty,
            Func<UnityEngine.Object, bool> isValid)
        {
            var sharedSize = sharedProperty.arraySize;
            var isolatedSize = isolatedProperty.arraySize;
            var sizesDiffer = ArraySizesDiffer(sharedProperty);
            var length = sizesDiffer ? isolatedSize : sharedSize;

            for (var i = 0; i < length; i++)
            {
                if (!TryGetPendingArrayElement(
                        sharedProperty,
                        isolatedProperty,
                        i,
                        sharedSize,
                        isolatedSize,
                        out var value))
                {
                    return false;
                }

                if (!isValid(value))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ArraySizesDiffer(SerializedProperty arrayProperty)
        {
            var sizeProperty = arrayProperty.FindPropertyRelative("Array.size");
            if (sizeProperty == null)
            {
                sizeProperty = arrayProperty.serializedObject.FindProperty(
                    arrayProperty.propertyPath + ".Array.size");
            }

            return sizeProperty != null && sizeProperty.hasMultipleDifferentValues;
        }

        static bool TryGetPendingArrayElement(
            SerializedProperty sharedProperty,
            SerializedProperty isolatedProperty,
            int index,
            int sharedSize,
            int isolatedSize,
            out UnityEngine.Object value)
        {
            value = null;
            if (index < sharedSize)
            {
                var sharedElement = sharedProperty.GetArrayElementAtIndex(index);
                if (sharedElement == null ||
                    sharedElement.propertyType != SerializedPropertyType.ObjectReference)
                {
                    return false;
                }

                if (!sharedElement.hasMultipleDifferentValues)
                {
                    value = sharedElement.objectReferenceValue;
                    return true;
                }

                if (index < isolatedSize)
                {
                    return TryGetObjectReference(isolatedProperty, index, out value);
                }

                if (isolatedSize > 0)
                {
                    return TryGetObjectReference(isolatedProperty, isolatedSize - 1, out value);
                }

                return true;
            }

            return index < isolatedSize && TryGetObjectReference(isolatedProperty, index, out value);
        }

        static bool TryGetObjectReference(SerializedProperty arrayProperty, int index, out UnityEngine.Object value)
        {
            value = null;
            var element = arrayProperty.GetArrayElementAtIndex(index);
            if (element == null || element.propertyType != SerializedPropertyType.ObjectReference)
            {
                return false;
            }

            value = element.objectReferenceValue;
            return true;
        }
    }
}
