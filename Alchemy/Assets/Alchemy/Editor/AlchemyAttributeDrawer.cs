using System;
using System.Collections.Generic;
using System.Reflection;
using Alchemy.Inspector;
using UnityEditor;
using UnityEngine.UIElements;

namespace Alchemy.Editor
{
    /// <summary>
    /// Base class for extending drawing processing for fields with Alchemy attributes.
    /// </summary>
    public abstract class AlchemyAttributeDrawer
    {
        SerializedObject serializedObject;
        SerializedProperty serializedProperty;
        object target;
        MemberInfo memberInfo;
        Attribute attribute;
        VisualElement targetElement;

        /// <summary>
        /// Target serialized object.
        /// </summary>
        public SerializedObject SerializedObject => serializedObject;

        /// <summary>
        /// Target serialized property.
        /// </summary>
        public SerializedProperty SerializedProperty => serializedProperty;

        /// <summary>
        /// Target object.
        /// </summary>
        public object Target => target;

        /// <summary>
        /// MemberInfo of the target member.
        /// </summary>
        public MemberInfo MemberInfo => memberInfo;

        /// <summary>
        /// Target attribute.
        /// </summary>
        public Attribute Attribute => attribute;

        /// <summary>
        /// Target visual element.
        /// </summary>
        public VisualElement TargetElement => targetElement;

        /// <summary>
        /// Called when the target visual element is created.
        /// </summary>
        public abstract void OnCreateElement();

        internal static void ExecutePropertyDrawers(SerializedObject serializedObject, SerializedProperty property, object target, MemberInfo memberInfo, VisualElement memberElement)
        {
            var attributes = memberInfo.GetCustomAttributes();
            Elements.PrefabConditionalElement.Wrap(serializedObject, target, attributes, memberElement);
            var hasValueDropdown = ValueDropdownSource.GetAttribute(memberInfo) != null;
            foreach (var attribute in attributes)
            {
                // Dropdowns create their value control before decoration. Reflected callbacks are
                // dispatched by that control rather than the SerializedProperty-only drawer.
                if (hasValueDropdown && property == null && attribute is OnValueChangedAttribute) continue;
                // Dropdown controls apply LabelWidth to the labels they create.
                if (hasValueDropdown && attribute is LabelWidthAttribute) continue;
                var processorType = GetAttributeDrawerType(attribute.GetType());
                if (processorType == null) continue;

                var processor = (AlchemyAttributeDrawer)Activator.CreateInstance(processorType);
                processor.serializedObject = serializedObject;
                processor.serializedProperty = property;
                processor.target = target;
                processor.memberInfo = memberInfo;
                processor.attribute = attribute;
                processor.targetElement = memberElement;

                processor.OnCreateElement();
            }
        }

        static Dictionary<Type, Type> attributeDrawerTypes;

        static Type GetAttributeDrawerType(Type attributeType)
        {
            attributeDrawerTypes ??= CreateAttributeDrawerTypes();
            attributeDrawerTypes.TryGetValue(attributeType, out var drawerType);
            return drawerType;
        }

        static Dictionary<Type, Type> CreateAttributeDrawerTypes()
        {
            var drawerTypes = new Dictionary<Type, Type>();
            foreach (var drawerType in TypeCache.GetTypesWithAttribute(typeof(CustomAttributeDrawerAttribute)))
            {
                if (!drawerType.IsSubclassOf(typeof(AlchemyAttributeDrawer))) continue;

                var targetAttributeType = drawerType.GetCustomAttribute<CustomAttributeDrawerAttribute>().targetAttributeType;
                // TypeCache order matches the previous FirstOrDefault scan; the first drawer wins.
                if (targetAttributeType == null || drawerTypes.ContainsKey(targetAttributeType)) continue;
                drawerTypes.Add(targetAttributeType, drawerType);
            }

            return drawerTypes;
        }
    }
}
