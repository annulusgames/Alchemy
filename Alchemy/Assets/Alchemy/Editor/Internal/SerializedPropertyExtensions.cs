using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    internal static class SerializedPropertyExtensions
    {
        public static bool TryGetAttribute<TAttribute>(this SerializedProperty property, out TAttribute result) where TAttribute : Attribute
        {
            return TryGetAttribute(property, false, out result);
        }

        public static bool TryGetAttribute<TAttribute>(this SerializedProperty property, bool inherit, out TAttribute result) where TAttribute : Attribute
        {
            TAttribute attribute = GetAttribute<TAttribute>(property, inherit);
            result = attribute;
            return attribute != null;
        }

        public static TAttribute GetAttribute<TAttribute>(this SerializedProperty property, bool inherit = false) where TAttribute : Attribute
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            return property.GetFieldInfo().GetCustomAttribute<TAttribute>(inherit);
        }

        public static IEnumerable<TAttribute> GetAttributes<TAttribute>(this SerializedProperty property, bool inherit) where TAttribute : Attribute
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            return property.GetFieldInfo().GetCustomAttributes<TAttribute>(inherit);
        }

        public static float GetHeight(this SerializedProperty property)
        {
            return EditorGUI.GetPropertyHeight(property, true);
        }

        public static float GetHeight(this SerializedProperty property, bool includeChildren)
        {
            return EditorGUI.GetPropertyHeight(property, includeChildren);
        }

        public static float GetHeight(this SerializedProperty property, GUIContent label, bool includeChildren)
        {
            return EditorGUI.GetPropertyHeight(property, label, includeChildren);
        }

        public static T GetValue<T>(this SerializedProperty property)
        {
            return GetNestedObject<T>(property.propertyPath, GetSerializedPropertyRootObject(property), true);
        }

        public static bool SetValue<T>(this SerializedProperty property, T value)
        {
            object obj = GetSerializedPropertyRootObject(property);
            var fieldStructure = property.propertyPath.Split('.');
            for (int i = 0; i < fieldStructure.Length - 1; i++)
            {
                obj = GetFieldOrPropertyValue<object>(fieldStructure[i], obj);
            }
            var fieldName = fieldStructure.Last();

            return SetFieldOrPropertyValue(fieldName, obj, value);
        }

        static readonly Regex IndexerRegex = new(@"[^0-9]+");

        // Misses are cached so a repeated walk does not run GetField/GetProperty or the base-type LINQ query again.
        static readonly Dictionary<(Type type, string name, bool includeAllBases, BindingFlags bindings), CachedMember> cacheMemberAccess = new();

        readonly struct CachedMember
        {
            public CachedMember(FieldInfo field, PropertyInfo property)
            {
                Field = field;
                Property = property;
            }

            public readonly FieldInfo Field;
            public readonly PropertyInfo Property;
        }

        public static FieldInfo GetFieldInfo(this SerializedProperty property)
        {
            object target = property.serializedObject.targetObject;
            if (target == null) return null;
            var splits = property.propertyPath.Split('.');

            // Native properties, such as Behaviour.m_Enabled, have no managed field.
            var fieldInfo = ReflectionHelper.GetField(target.GetType(), splits[0], includingBaseNonPublic: true);
            if (fieldInfo == null) return null;
            target = fieldInfo.GetValue(target);

            for (var i = 1; i < splits.Length; i++)
            {
                if (target == null) return null;

                if (splits[i] == "Array")
                {
                    i++;
                    if (i >= splits.Length) continue;

                    var index = int.Parse(IndexerRegex.Replace(splits[i], string.Empty));
                    var targetType = target.GetType();

                    if (targetType.IsArray) target = (target as Array).GetValue(index);
                    else target = (target as IList)[index];

                    i++;
                    if (i >= splits.Length) continue;

                    targetType = target.GetType();
                    fieldInfo = ReflectionHelper.GetField(targetType, splits[i], includingBaseNonPublic: true);
                }
                else
                {
                    var targetType = target.GetType();
                    fieldInfo = ReflectionHelper.GetField(targetType, splits[i], includingBaseNonPublic: true);
                }

                target = fieldInfo?.GetValue(target);
            }

            return fieldInfo;
        }

        public static Type GetPropertyType(this SerializedProperty property, bool isCollectionType = false)
        {
            var fieldInfo = property.GetFieldInfo();

            if (isCollectionType && property.propertyType != SerializedPropertyType.String)
                return fieldInfo.FieldType.IsArray ?
                    fieldInfo.FieldType.GetElementType() :
                    fieldInfo.FieldType.GetGenericArguments()[0];
            return fieldInfo.FieldType;
        }

        public static object SetManagedReferenceType(this SerializedProperty property, Type type)
        {
            var obj = (type != null) ? Activator.CreateInstance(type) : null;
            property.managedReferenceValue = obj;
            return obj;
        }

        public static string GetManagedReferenceFieldTypeName(this SerializedProperty property)
        {
            var typeName = property.managedReferenceFieldTypename;
            var splitIndex = typeName.IndexOf(' ');
            return typeName[(splitIndex + 1)..];
        }

        // Key is SerializedProperty.managedReferenceFieldTypename ("AssemblyName TypeName").
        static readonly Dictionary<string, Type> managedReferenceFieldTypes = new();

        public static Type GetManagedReferenceFieldType(this SerializedProperty property)
        {
            var typeName = property.managedReferenceFieldTypename;
            if (typeName != null && managedReferenceFieldTypes.TryGetValue(typeName, out var cached)) return cached;

            var splitIndex = typeName.IndexOf(' ');
            var assembly = Assembly.Load(typeName[..splitIndex]);
            var type = assembly.GetType(typeName[(splitIndex + 1)..]);
            if (typeName != null) managedReferenceFieldTypes[typeName] = type;
            return type;
        }

        static UnityEngine.Object GetSerializedPropertyRootObject(SerializedProperty property)
        {
            return property.serializedObject.targetObject;
        }

        static T GetNestedObject<T>(string path, object obj, bool includeAllBases = false)
        {
            var parts = path.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                if (part == "Array")
                {
                    // Same digits as Regex([^0-9]).Replace, without allocating a Regex or a new string.
                    if (!TryParseIndex(parts[i + 1], out var index))
                    {
                        index = -1;
                    }

                    obj = GetElementAtOrDefault(obj, index);

                    i++;
                }
                else
                {
                    obj = GetFieldOrPropertyValue<object>(part, obj, includeAllBases);
                }
            }
            return (T)obj;
        }

        static object GetElementAtOrDefault(object arrayOrListObj, int index)
        {
            if (arrayOrListObj is IList valueList && index >= 0 && index < valueList.Count)
            {
                return valueList[index];
            }

            if (arrayOrListObj is IEnumerable<object> referenceEnumerable)
            {
                return referenceEnumerable.ElementAtOrDefault(index);
            }

            if (arrayOrListObj is IList fallbackList)
            {
                Type listType = fallbackList.GetType();
                Type elementType = listType.IsArray ? listType.GetElementType() : listType.GetGenericArguments()[0];
                return Activator.CreateInstance(elementType);
            }

            throw new ArgumentException($"Can't parse {arrayOrListObj.GetType()} as Array or List");
        }

        public static object GetParentObject(this SerializedProperty property)
        {
            if (property == null) return null;

            var path = property.propertyPath.Replace(".Array.data[", "[");
            object obj = property.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element[..element.IndexOf("[")];
                    var index = Convert.ToInt32(element[element.IndexOf("[")..].Replace("[", "").Replace("]", ""));
                    obj = ReflectionHelper.GetValue(obj, elementName, index);
                }
                else
                {
                    obj = ReflectionHelper.GetValue(obj, element);
                }
            }
            return obj;
        }


        public static object GetDeclaredObject(this SerializedProperty property)
        {
            if (property == null) return null;

            var path = property.propertyPath.Replace(".Array.data[", "[");
            object obj = property.serializedObject.targetObject;
            var elements = path.Split('.');
            for (int i = 0; i < elements.Length - 1; i++)
            {
                var element = elements[i];
                if (element.Contains("["))
                {
                    var elementName = element[..element.IndexOf("[")];
                    var index = Convert.ToInt32(element[element.IndexOf("[")..].Replace("[", "").Replace("]", ""));
                    obj = ReflectionHelper.GetValue(obj, elementName, index);
                }
                else
                {
                    obj = ReflectionHelper.GetValue(obj, element);
                }
            }
            return obj;
        }

        static bool TryParseIndex(string text, out int index)
        {
            var any = false;
            var value = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var digit = text[i] - '0';
                if ((uint)digit > 9) continue;

                any = true;
                if (value > (int.MaxValue - digit) / 10)
                {
                    index = 0;
                    return false;
                }

                value = value * 10 + digit;
            }

            index = value;
            return any;
        }

        static CachedMember GetCachedMember(Type type, string name, bool includeAllBases, BindingFlags bindings)
        {
            var key = (type, name, includeAllBases, bindings);
            if (cacheMemberAccess.TryGetValue(key, out var cached)) return cached;

            var field = type.GetField(name, bindings);
            if (field != null)
            {
                cached = new CachedMember(field, null);
                cacheMemberAccess.Add(key, cached);
                return cached;
            }

            var property = type.GetProperty(name, bindings);
            if (property != null)
            {
                cached = new CachedMember(null, property);
                cacheMemberAccess.Add(key, cached);
                return cached;
            }

            if (includeAllBases)
            {
                foreach (var baseType in TypeHelper.GetBaseClassesAndInterfaces(type))
                {
                    field = baseType.GetField(name, bindings);
                    if (field != null)
                    {
                        cached = new CachedMember(field, null);
                        cacheMemberAccess.Add(key, cached);
                        return cached;
                    }

                    property = baseType.GetProperty(name, bindings);
                    if (property != null)
                    {
                        cached = new CachedMember(null, property);
                        cacheMemberAccess.Add(key, cached);
                        return cached;
                    }
                }
            }

            cacheMemberAccess.Add(key, default);
            return default;
        }

        static T GetFieldOrPropertyValue<T>(string fieldName, object obj, bool includeAllBases = false, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        {
            var member = GetCachedMember(obj.GetType(), fieldName, includeAllBases, bindings);
            if (member.Field != null) return (T)member.Field.GetValue(obj);
            if (member.Property != null) return (T)member.Property.GetValue(obj, null);
            return default;
        }

        static bool SetFieldOrPropertyValue(string fieldName, object obj, object value, bool includeAllBases = false, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        {
            var member = GetCachedMember(obj.GetType(), fieldName, includeAllBases, bindings);
            if (member.Field != null)
            {
                member.Field.SetValue(obj, value);
                return true;
            }

            if (member.Property != null)
            {
                member.Property.SetValue(obj, value, null);
                return true;
            }

            return false;
        }

    }

}
