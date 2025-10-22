using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace Alchemy.Editor
{
    public interface IAlchemyAttributeDrawer
    {
        void SetContext(SerializedObject serializedObject, SerializedProperty serializedProperty, object target, MemberInfo memberInfo, Attribute attribute, VisualElement targetElement);
        void OnCreateElement();
    }

    /// <summary>
    /// Base class for extending drawing processing for fields with Alchemy attributes.
    /// </summary>
    public abstract class AlchemyAttributeDrawer<T> : IAlchemyAttributeDrawer where T : Attribute
    {
        private SerializedObject serializedObject;
        private SerializedProperty serializedProperty;
        private object target;
        private MemberInfo memberInfo;
        private T attribute;
        private VisualElement targetElement;

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
        public T Attribute => attribute;

        /// <summary>
        /// Target visual element.
        /// </summary>
        public VisualElement TargetElement => targetElement;

        /// <summary>
        /// Called when the target visual element is created.
        /// </summary>
        public abstract void OnCreateElement();

        void IAlchemyAttributeDrawer.SetContext(SerializedObject serializedObject, SerializedProperty serializedProperty, object target, MemberInfo memberInfo, Attribute attribute, VisualElement targetElement)
        {
            this.serializedObject = serializedObject;
            this.serializedProperty = serializedProperty;
            this.target = target;
            this.memberInfo = memberInfo;
            this.attribute = attribute as T;
            this.targetElement = targetElement;
        }
    }
}