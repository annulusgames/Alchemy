using System;
using System.Collections.Generic;
using System.Reflection;
using Alchemy.Editor.Elements;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class ListFieldArrayWriteTest
    {
        [Test]
        public void ElementEdit_WritesOneArraySlotAndKeepsTheInstance()
        {
            var values = new[] { 10, 20, 30 };
            var listField = new ListField(values, "Values");
            object notified = null;
            listField.OnValueChanged += value => notified = value;

            SetElement(listField, 1, 99);

            Assert.That(notified, Is.SameAs(values));
            Assert.That(values, Is.EqualTo(new[] { 10, 99, 30 }));
        }

        [Test]
        public void ElementEdit_ReflectedFieldKeepsTheSameArrayInstance()
        {
            var host = new ReflectedArrayHost();
            var original = host.fieldValues;
            var member = typeof(ReflectedArrayHost).GetField(nameof(ReflectedArrayHost.fieldValues));
            Assert.That(member, Is.Not.Null);

            var reflectionField = new ReflectionField(host, member);
            var listField = reflectionField.Q<ListField>();
            Assert.That(listField, Is.Not.Null);

            SetElement(listField, 0, 7);

            Assert.That(host.fieldValues, Is.SameAs(original));
            Assert.That(host.fieldValues, Is.EqualTo(new[] { 7, 20, 30 }));
        }

        [Test]
        public void ElementEdit_ReflectedPropertyKeepsTheSameArrayInstance()
        {
            var host = new ReflectedArrayHost();
            var original = host.PropertyValues;
            var member = typeof(ReflectedArrayHost).GetProperty(nameof(ReflectedArrayHost.PropertyValues));
            Assert.That(member, Is.Not.Null);

            var reflectionField = new ReflectionField(host, member);
            var listField = reflectionField.Q<ListField>();
            Assert.That(listField, Is.Not.Null);

            SetElement(listField, 2, 8);

            Assert.That(host.PropertyValues, Is.SameAs(original));
            Assert.That(host.PropertyValues, Is.EqualTo(new[] { 1, 2, 8 }));
        }

        [Test]
        public void ElementEdit_ListNotifiesTheSameListInstance()
        {
            var values = new List<int> { 4, 5 };
            var listField = new ListField(values, "Values");
            object notified = null;
            listField.OnValueChanged += value => notified = value;

            SetElement(listField, 1, 6);

            Assert.That(notified, Is.SameAs(values));
            Assert.That(values, Is.EqualTo(new[] { 4, 6 }));
        }

        static void SetElement(ListField listField, int index, int value)
        {
            var listView = listField.Q<ListView>();
            Assert.That(listView, Is.Not.Null);

            var item = listView.makeItem();
            listView.bindItem(item, index);

            var fieldElement = item.Q<GenericField>();
            Assert.That(fieldElement, Is.Not.Null);
            var changed = fieldElement.GetType().GetField(
                nameof(GenericField.OnValueChanged),
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(changed, Is.Not.Null);

            var handler = (Action<object>)changed.GetValue(fieldElement);
            Assert.That(handler, Is.Not.Null);
            handler.Invoke(value);
        }

        sealed class ReflectedArrayHost
        {
            public int[] fieldValues = { 10, 20, 30 };

            [ShowInInspector]
            public int[] PropertyValues { get; set; } = new int[] { 1, 2, 3 };
        }
    }
}
