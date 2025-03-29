using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Editor.Elements
{
    public sealed class DictionaryField : HashMapFieldBase
    {
        public DictionaryField(object collection, string label) : base(collection, label)
        {
            if (collection != null)
            {
                keyType = collection.GetType().GenericTypeArguments[0];
                valueType = collection.GetType().GenericTypeArguments[1];
                kvType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
            }
        }

        public override string CollectionTypeName => TypeName;

        const string TypeName = "Dictionary";
        const string KeyName = "Key";
        const string ValueName = "Value";

        readonly Type keyType;
        readonly Type valueType;
        readonly Type kvType;

        public override bool CheckElement(object element)
        {
            var keyObj = ReflectionHelper.GetPropertyValue(element, element.GetType(), KeyName);
            if (keyObj is string str && string.IsNullOrEmpty(str)) return true;
            if (keyObj == null) return true;
            return (bool)ReflectionHelper.Invoke(Collection, "ContainsKey", keyObj);
        }

        public override object CreateElement()
        {
            var keyObj = TypeHelper.CreateDefaultInstance(keyType);
            var valueObj = TypeHelper.CreateDefaultInstance(valueType);
            return Activator.CreateInstance(kvType, keyObj, valueObj);
        }

        public override void AddElement(object element)
        {
            var keyObj = ReflectionHelper.GetPropertyValue(element, kvType, KeyName);
            var valueObj = ReflectionHelper.GetPropertyValue(element, kvType, ValueName);
            ReflectionHelper.Invoke(Collection, "Add", keyObj, valueObj);
        }

        public override bool RemoveElement(object element)
        {
            return (bool)ReflectionHelper.Invoke(Collection, "Remove", ReflectionHelper.GetPropertyValue(element, kvType, KeyName));
        }

        public override void ClearElements()
        {
            ReflectionHelper.Invoke(Collection, "Clear");
        }

        public override HashMapItemBase CreateItem(object collection, object elementObj, string label)
        {
            return new Item(collection, elementObj);
        }

        public sealed class Item : HashMapItemBase
        {
            public Item(object collection, object keyValuePair)
            {
                var box = new Box()
                {
                    style = {
                        marginBottom = 3.5f,
                        marginRight = -2f,
                        flexDirection = FlexDirection.Row
                    }
                };

                kvType = keyValuePair.GetType();
                var keyType = kvType.GenericTypeArguments[0];
                var valueType = kvType.GenericTypeArguments[1];

                key = ReflectionHelper.GetPropertyValue(keyValuePair, kvType, KeyName);
                value = ReflectionHelper.GetPropertyValue(keyValuePair, kvType, ValueName);

                this.collection = collection;
                this.keyValuePair = keyValuePair;

                var keyValueElement = new VisualElement()
                {
                    style = {
                        flexDirection = FlexDirection.Column,
                        flexGrow = 1f
                    }
                };
                box.Add(keyValueElement);

                keyField = new GenericField(new IdentityAccessor(key), keyType, KeyName)
                {
                    style = { flexGrow = 1f }
                };
                keyField.OnValueChanged += SetKey;
                keyValueElement.Add(keyField);
                
                valueField = new GenericField(new DelegateAccessor(()=>value,null), valueType, ValueName)
                {
                    style = { flexGrow = 1f }
                };
                valueField.OnValueChanged += SetValue;
                keyValueElement.Add(valueField);

                var closeButton = new Button(() => OnClose?.Invoke())
                {
                    style = {
                        width = EditorGUIUtility.singleLineHeight,
                        height = EditorGUIUtility.singleLineHeight,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        fontSize = 10f
                    },
                    text = "X",
                };
                box.Add(closeButton);
                Add(box);
            }

            readonly GenericField keyField;
            readonly GenericField valueField;

            readonly object collection;
            public override object Value => keyValuePair;

            object key;
            object value;
            object keyValuePair;
            readonly Type kvType;

            public override void Lock()
            {
                keyField.SetEnabled(false);
                valueField.OnValueChanged -= SetValue;
                valueField.OnValueChanged += x =>
                {
                    ReflectionHelper.GetProperty(collection.GetType(), "Item").SetValue(collection, x, new object[] { key });
                };
            }

            void SetKey(object obj)
            {
                key = obj;
                keyValuePair = Activator.CreateInstance(kvType, key, value);
                OnValueChanged?.Invoke(keyValuePair);
            }

            void SetValue(object obj)
            {
                value = obj;
                keyValuePair = Activator.CreateInstance(kvType, key, value);
                OnValueChanged?.Invoke(keyValuePair);
            }
        }
    }
}