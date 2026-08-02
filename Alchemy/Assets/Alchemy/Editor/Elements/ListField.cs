using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Alchemy.Editor.Elements
{
    /// <summary>
    /// Visual Element that draws an IList
    /// </summary>
    public sealed class ListField : VisualElement
    {
        sealed class Item : VisualElement
        {
            public int index;
        }

        const string ItemClassName = "unity-list-view__item";

        public ListField(IList target, string label)
        {
            Assert.IsNotNull(target);
            if (target is Array array)
            {
                this.array = array;
                arrayElementType = array.GetType().GetElementType();
                var listType = typeof(List<>).MakeGenericType(arrayElementType);
                list = (IList)Activator.CreateInstance(listType);
                foreach (var item in array) list.Add(item);
            }
            else
            {
                list = target;
            }

            listView = GUIHelper.CreateDefaultListView(label);
            listView.makeItem = () => new Item();
            listView.bindItem = (element, index) =>
            {
                ((Item)element).index = index;

                var value = list[index];
                var listType = list.GetType();
                var valueType = value != null ? value.GetType() : listType.IsGenericType ? listType.GenericTypeArguments[0] : typeof(object);
                var fieldElement = new GenericField(value, valueType, label);
                element.Add(fieldElement);
                var labelElement = fieldElement.Q<Label>();
                if (labelElement != null) labelElement.text = "Element " + index;

                fieldElement.OnValueChanged += x =>
                {
                    list[((Item)element).index] = x;
                    NotifyOnValueChanged();
                };
            };
            listView.unbindItem = (element, index) =>
            {
                element.Clear();
            };
            listView.itemsSource = list;
            listView.itemIndexChanged += (prevIndex, index) =>
            {
                var item = listView.Query<VisualElement>(className: ItemClassName).AtIndex(index).Q<Item>();
                item.index = index;
                NotifyOnValueChanged();
            };
            listView.itemsAdded += indexes => NotifyOnValueChanged();
            listView.itemsRemoved += indexes =>
            {
                if (arrayElementType == null) listView.schedule.Execute(NotifyOnValueChanged);
                else NotifyOnItemsRemoved(indexes);
            };

            listView.Q<Label>().style.unityFontStyleAndWeight = FontStyle.Bold;
            Add(listView);
        }

        readonly IList list;
        readonly ListView listView;
        readonly Type arrayElementType;
        Array array;

        public event Action<object> OnValueChanged;

        void NotifyOnValueChanged()
        {
            if (arrayElementType == null)
            {
                OnValueChanged?.Invoke(list);
                return;
            }

            if (array.Length != list.Count)
            {
                array = Array.CreateInstance(arrayElementType, list.Count);
            }
            list.CopyTo(array, 0);
            OnValueChanged?.Invoke(array);
        }

        void NotifyOnItemsRemoved(IEnumerable<int> indexes)
        {
            var removedIndexes = new HashSet<int>(indexes);
            array = Array.CreateInstance(arrayElementType, list.Count - removedIndexes.Count);
            var destinationIndex = 0;
            for (var i = 0; i < list.Count; i++)
            {
                if (removedIndexes.Contains(i)) continue;
                array.SetValue(list[i], destinationIndex++);
            }
            OnValueChanged?.Invoke(array);
        }
    }
}
