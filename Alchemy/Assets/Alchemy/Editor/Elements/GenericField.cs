using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Editor.Elements
{
    /// <summary>
    /// Visual Element that creates a suitable input Field from object type
    /// </summary>
    public sealed class GenericField : VisualElement
    {
        private readonly StyleSheet _styleSheet = Resources.Load<StyleSheet>("Elements/GenericField-Styles");
        
        const string CreateButtonText = "Create...";

        public GenericField(object obj, Type type, string label,bool isDelayed = false)
        {
            styleSheets.Add(_styleSheet);

            Build(obj, type, label, isDelayed);
            GUIHelper.ScheduleAdjustLabelWidth(this);
        }

        void Build(object obj, Type type, string label, bool isDelayed)
        {
            Clear();

            // Add [Create...] button
            if (obj == null && !typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                var nullableArea = new VisualElement();
                nullableArea.AddToClassList("generic-field__nullable-area");

                var nullableLabel = new Label(label + " (Null)");
                nullableArea.AddToClassList("generic-field__nullable-area__label");
                nullableArea.Add(nullableLabel);

                // TODO: support polymorphism
                if (type == typeof(string))
                {
                    nullableArea.Add(new Button(() =>
                    {
                        var instance = "";
                        Build(instance, type, label, isDelayed);
                        OnValueChanged?.Invoke(instance);
                    })
                    { text = CreateButtonText });
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) // nullable
                {
                    nullableArea.Add(new Button(() =>
                    {
                        var instance = Activator.CreateInstance(type, Activator.CreateInstance(type.GenericTypeArguments[0]));
                        Build(instance, type, label, isDelayed);
                        OnValueChanged?.Invoke(instance);
                    })
                    { text = CreateButtonText });
                }
                else if (TypeHelper.HasDefaultConstructor(type))
                {
                    nullableArea.Add(new Button(() =>
                    {
                        var instance = TypeHelper.CreateDefaultInstance(type);
                        Build(instance, type, label, isDelayed);
                        OnValueChanged?.Invoke(instance);
                    })
                    { text = CreateButtonText });
                }

                Add(nullableArea);

                return;
            }

            this.isDelayed = isDelayed;

            if (type == typeof(bool))
            {
                AddField(new Toggle(label), (bool)obj);
            }
            else if (type == typeof(int))
            {
                AddField(new IntegerField(label), (int)obj);
            }
            
            else if (type == typeof(uint))
            {
#if UNITY_2022_1_OR_NEWER
                AddField(new UnsignedIntegerField(label), (uint)obj);
#else
                var value = (uint)obj;
                var control = new LongField(label);
                control.value = value;
                control.RegisterValueChangedCallback(x =>
                {
                    var newValue = (uint)Math.Clamp(control.value, 0, uint.MaxValue);    
                    OnValueChanged?.Invoke(newValue);
                    control.value = newValue;
                });
                
                Add(control);
#endif
                
            }
            else if (type == typeof(long))
            {
                AddField(new LongField(label), (long)obj);
            }
            else if (type == typeof(ulong))
            {
#if UNITY_2022_1_OR_NEWER
                AddField(new UnsignedLongField(label), (ulong)obj);
#else
                var value = (ulong)obj;
                var control = new LongField(label);
                control.value = (long)value;
                control.RegisterValueChangedCallback(x =>
                {
                    var newValue = Math.Clamp(control.value, 0, long.MaxValue); 
                    OnValueChanged?.Invoke((ulong)newValue);
                    control.value = newValue;
                });
                
                Add(control);
#endif
            }
            else if (type == typeof(float))
            {
                AddField(new FloatField(label), (float)obj);
            }
            else if (type == typeof(double) || type == typeof(decimal))
            {
                AddField(new DoubleField(label), (double)obj);
            }
            else if (type == typeof(string))
            {
                AddField(new TextField(label), (string)obj);
            }
            else if (type == typeof(char))
            {
                var charField = new TextField(label, 1, false, false, default) { value = obj.ToString() };
                charField.RegisterValueChangedCallback(x => OnValueChanged?.Invoke(x.newValue[0]));
                Add(charField);
            }
            else if (type.IsEnum)
            {
                var value = (Enum)obj;
                if (value.GetType().HasCustomAttribute<FlagsAttribute>()) AddField(new EnumFlagsField(label, value), value);
                else AddField(new EnumField(label, value), value);
            }
            else if (type == typeof(Vector2))
            {
                AddField(new Vector2Field(label), (Vector2)obj);
            }
            else if (type == typeof(Vector2Int))
            {
                AddField(new Vector2IntField(label), (Vector2Int)obj);
            }
            else if (type == typeof(Vector3))
            {
                AddField(new Vector3Field(label), (Vector3)obj);
            }
            else if (type == typeof(Vector3Int))
            {
                AddField(new Vector3IntField(label), (Vector3Int)obj);
            }
            else if (type == typeof(Vector4))
            {
                AddField(new Vector4Field(label), (Vector4)obj);
            }
            else if (type == typeof(Rect))
            {
                AddField(new RectField(label), (Rect)obj);
            }
            else if (type == typeof(RectInt))
            {
                AddField(new RectIntField(label), (RectInt)obj);
            }
            else if (type == typeof(Bounds))
            {
                AddField(new BoundsField(label), (Bounds)obj);
            }
            else if (type == typeof(BoundsInt))
            {
                AddField(new BoundsIntField(label), (BoundsInt)obj);
            }
            else if (type == typeof(Color))
            {
                AddField(new ColorField(label), (Color)obj);
            }
            else if (type == typeof(Gradient))
            {
                AddField(new GradientField(label), (Gradient)obj);
            }
            else if (type == typeof(Hash128))
            {
                AddField(new Hash128Field(label), (Hash128)obj);
            }
            else if (type == typeof(AnimationCurve))
            {
                AddField(new CurveField(label), (AnimationCurve)obj);
            }
            else if (type == typeof(Gradient))
            {
                AddField(new GradientField(label), (Gradient)obj);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                AddField(new ObjectField(label) { objectType = type }, (UnityEngine.Object)obj);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                var field = new HashSetField(obj, label);
                field.OnValueChanged += x => OnValueChanged?.Invoke(x);

                Add(field);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var field = new DictionaryField(obj, label);
                field.OnValueChanged += x => OnValueChanged?.Invoke(x);
                Add(field);
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                var field = new ListField((IList)obj, label);
                field.OnValueChanged += x => OnValueChanged?.Invoke(x);
                Add(field);
            }
            else
            {
                var field = new ClassField(obj, type, label);
                field.OnValueChanged += x => OnValueChanged?.Invoke(x);
                Add(field);
            }
        }

        public event Action<object> OnValueChanged;
        bool isDelayed;
        bool changed;

        void AddField<T>(BaseField<T> control, T value)
        {
            control.value = value;
            if (isDelayed && control is not ObjectField) // ignore ObjectField
            {
                control.RegisterValueChangedCallback(x => changed = true);
                control.RegisterCallback<FocusOutEvent>(x =>
                {
                    if (changed)
                    {
                        OnValueChanged?.Invoke(control.value);
                        changed = false;
                    }
                });
            }
            else
            {
                control.RegisterValueChangedCallback(x => OnValueChanged?.Invoke(x.newValue));
            }
            Add(control);
        }
    }
}