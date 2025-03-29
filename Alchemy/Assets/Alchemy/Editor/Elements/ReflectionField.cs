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
        public ReflectionField(IObjectAccessor accessor, MemberInfo memberInfo)
        {
            Rebuild(accessor, memberInfo);
        }

        public void Rebuild(IObjectAccessor accessor, MemberInfo memberInfo)
        {
            Clear();

            if (memberInfo is MethodInfo methodInfo)
            {
                if (methodInfo.HasCustomAttribute<ButtonAttribute>())
                {
                    var button = new MethodButton(accessor, methodInfo);
                    Add(button);
                }
                return;
            }
            GenericField element;
            switch (memberInfo)
            {
                default: return;
                case FieldInfo fieldInfo:
                    var fieldAccessor = accessor.Create(fieldInfo);
                    var target = accessor.Target;
                    var fieldType = target == null ? fieldInfo.FieldType : fieldInfo.GetValue(target)?.GetType() ?? fieldInfo.FieldType;
                    element = new GenericField(fieldAccessor, fieldType, ObjectNames.NicifyVariableName(memberInfo.Name), false);
                    element.OnValueChanged += x =>
                    {
                        var currentTarget = accessor.Target;
                        OnBeforeValueChange?.Invoke(currentTarget);
                        fieldInfo.SetValue(currentTarget, x);

                        // Force serialization
                        if (currentTarget is ISerializationCallbackReceiver receiver)
                        {
                            receiver.OnBeforeSerialize();
                        }

                        OnValueChanged?.Invoke(currentTarget);
                        accessor.Target= currentTarget;
                    };
                    break;
                case PropertyInfo propertyInfo:
                    if (!propertyInfo.HasCustomAttribute<ShowInInspectorAttribute>()) return;
                    if (!propertyInfo.CanRead) return;
                    var propertyAccessor = accessor.Create(propertyInfo);
                     target = accessor.Target;
                    var propertyType = target == null ? propertyInfo.PropertyType : propertyInfo.GetValue(target)?.GetType() ?? propertyInfo.PropertyType;
                    element = new GenericField(propertyAccessor, propertyType, ObjectNames.NicifyVariableName(memberInfo.Name), false);
                    element.OnValueChanged += x =>
                    {
                        var currentTarget = accessor.Target;
                        OnBeforeValueChange?.Invoke(currentTarget);
                        if (propertyInfo.CanWrite)
                        {
                            propertyInfo.SetValue(currentTarget, x);
                        }

                        // Force serialization
                        if (currentTarget is ISerializationCallbackReceiver receiver)
                        {
                            receiver.OnBeforeSerialize();
                        }

                        OnValueChanged?.Invoke(currentTarget);
                        accessor.Target= currentTarget;
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