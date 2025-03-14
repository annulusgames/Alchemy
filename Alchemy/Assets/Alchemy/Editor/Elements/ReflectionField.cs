using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Alchemy.Inspector;

namespace Alchemy.Editor.Elements
{
    public sealed class ReflectionField : VisualElement
    {
        public ReflectionField(object target, MemberInfo memberInfo)
        {
            Rebuild(target, memberInfo);
        }

        public void Rebuild(object target, MemberInfo memberInfo)
        {
            Clear();

            if (memberInfo is MethodInfo methodInfo)
            {
                if (methodInfo.HasCustomAttribute<ButtonAttribute>())
                {
                    var button = new MethodButton(target, methodInfo);
                    if (methodInfo.TryGetCustomAttribute(out LabelTextAttribute labelText)) {
                        button.SetLableText(labelText.Text);
                    }
                    Add(button);
                }
                return;
            }

            object value;
            GenericField element;
            switch (memberInfo)
            {
                default: return;
                case FieldInfo fieldInfo:
                    value = fieldInfo.IsStatic ? fieldInfo.GetValue(null) : target == null ? TypeHelper.GetDefaultValue(fieldInfo.FieldType) : fieldInfo.GetValue(target);
                    var fieldType = target == null ? fieldInfo.FieldType : fieldInfo.GetValue(target)?.GetType() ?? fieldInfo.FieldType;
                    element = new GenericField(value, fieldType, ObjectNames.NicifyVariableName(memberInfo.Name), true);
                    element.OnValueChanged += x =>
                    {
                        OnBeforeValueChange?.Invoke(target);
                        fieldInfo.SetValue(target, x);

                        // Force serialization
                        if (target is ISerializationCallbackReceiver receiver)
                        {
                            receiver.OnBeforeSerialize();
                        }

                        OnValueChanged?.Invoke(target);
                    };
                    break;
                case PropertyInfo propertyInfo:
                    if (!propertyInfo.HasCustomAttribute<ShowInInspectorAttribute>()) return;
                    if (!propertyInfo.CanRead) return;

                    value = propertyInfo.GetMethod.IsStatic ? propertyInfo.GetValue(null) : target == null ? TypeHelper.GetDefaultValue(propertyInfo.PropertyType) : propertyInfo.GetValue(target);
                    var propertyType = target == null ? propertyInfo.PropertyType : propertyInfo.GetValue(target)?.GetType() ?? propertyInfo.PropertyType;
                    element = new GenericField(value, propertyType, ObjectNames.NicifyVariableName(memberInfo.Name), true);
                    element.OnValueChanged += x =>
                    {
                        OnBeforeValueChange?.Invoke(target);
                        if (propertyInfo.CanWrite) propertyInfo.SetValue(target, x);

                        // Force serialization
                        if (target is ISerializationCallbackReceiver receiver)
                        {
                            receiver.OnBeforeSerialize();
                        }

                        OnValueChanged?.Invoke(target);
                    };
                    element.SetEnabled(propertyInfo.CanWrite);
                    break;
            }

            Add(element);
        }

        public event Action<object> OnBeforeValueChange;
        public event Action<object> OnValueChanged;
    }
}