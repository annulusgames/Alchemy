using System;
using System.Linq;
using System.Reflection;
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
            var processorTypes = TypeCache.GetTypesWithAttribute(typeof(CustomAttributeDrawerAttribute));
            foreach (var attribute in attributes)
            {
                var processorType = processorTypes.FirstOrDefault(x => x.IsSubclassOf(typeof(AlchemyAttributeDrawer)) && x.GetCustomAttribute<CustomAttributeDrawerAttribute>().targetAttributeType == attribute.GetType());
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
    }
}
