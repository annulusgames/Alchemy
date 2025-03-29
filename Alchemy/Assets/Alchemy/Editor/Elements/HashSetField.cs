using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Editor.Elements
{
    public sealed class HashSetField : HashMapFieldBase
    {
        public HashSetField(object collection, string label) : base(collection, label) { }

        public override string CollectionTypeName => "HashSet";

        public override bool CheckElement(object key)
        {
            return (bool)ReflectionHelper.Invoke(Collection, "Contains", key);
        }

        public override object CreateElement()
        {
            return TypeHelper.CreateDefaultInstance(Collection.GetType().GenericTypeArguments[0]);
        }

        public override void AddElement(object element)
        {
            ReflectionHelper.Invoke(Collection, "Add", element);
        }

        public override bool RemoveElement(object element)
        {
            return (bool)ReflectionHelper.Invoke(Collection, "Remove", element);
        }

        public override void ClearElements()
        {
            ReflectionHelper.Invoke(Collection, "Clear");
        }

        public override HashMapItemBase CreateItem(object collection, object elementObj, string label)
        {
            return new Item(collection, elementObj, label);
        }

        public sealed class Item : HashMapItemBase
        {
            public Item(object collection, object  elementObj, string label)
            {
                var box = new Box()
                {
                    style = {
                        marginBottom = 3.5f,
                        marginRight = -2f,
                        flexDirection = FlexDirection.Row
                    }
                };

                var valueType = elementObj == null ? collection.GetType().GenericTypeArguments[0] : elementObj.GetType();
                var accessor = new IdentityAccessor(elementObj);
                inputField = new GenericField(accessor, valueType, label);
                inputField.style.flexGrow = 1f;
                inputField.OnValueChanged += x =>
                {
                    value = x;
                    OnValueChanged?.Invoke(x);
                    accessor.Target = x;
                };
                box.Add(inputField);

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

            readonly GenericField inputField;

            public override void Lock()
            {
                inputField.SetEnabled(false);
            }

            object value;
            public override object Value => value;
        }
    }
}