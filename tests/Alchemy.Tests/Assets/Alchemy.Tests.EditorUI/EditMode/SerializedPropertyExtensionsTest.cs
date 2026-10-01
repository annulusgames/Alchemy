using System;
using System.Collections.Generic;
using Alchemy.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class SerializedPropertyExtensionsTest
    {
        [Serializable]
        public class NestedItem
        {
            public int value;
        }

        class ListHost : ScriptableObject
        {
            public List<NestedItem> items;
        }

        class ArrayHost : ScriptableObject
        {
            public NestedItem[] items;
        }

        [Test]
        public void GetValue_ListOfClass_ReadsIndexedElement()
        {
            var host = ScriptableObject.CreateInstance<ListHost>();
            try
            {
                host.items = new List<NestedItem>(50);
                for (var i = 0; i < 50; i++)
                {
                    host.items.Add(new NestedItem { value = i });
                }

                var serializedObject = new SerializedObject(host);
                var property = serializedObject.FindProperty("items.Array.data[42].value");

                Assert.That(property, Is.Not.Null);
                Assert.That(property.GetValue<int>(), Is.EqualTo(42));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void GetValue_ArrayOfClass_ReadsIndexedElement()
        {
            var host = ScriptableObject.CreateInstance<ArrayHost>();
            try
            {
                host.items = new NestedItem[20];
                for (var i = 0; i < host.items.Length; i++)
                {
                    host.items[i] = new NestedItem { value = i * 3 };
                }

                var serializedObject = new SerializedObject(host);
                var property = serializedObject.FindProperty("items.Array.data[17].value");

                Assert.That(property, Is.Not.Null);
                Assert.That(property.GetValue<int>(), Is.EqualTo(51));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void GetValue_ListOfClass_ReadsFirstAndLastElements()
        {
            var host = ScriptableObject.CreateInstance<ListHost>();
            try
            {
                host.items = new List<NestedItem>
                {
                    new NestedItem { value = 11 },
                    new NestedItem { value = 22 },
                    new NestedItem { value = 33 },
                };

                var serializedObject = new SerializedObject(host);
                Assert.That(
                    serializedObject.FindProperty("items.Array.data[0].value").GetValue<int>(),
                    Is.EqualTo(11));
                Assert.That(
                    serializedObject.FindProperty("items.Array.data[2].value").GetValue<int>(),
                    Is.EqualTo(33));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Serializable]
        class HiddenItemBase
        {
            [SerializeField] int hidden;

            public void SetHidden(int value) => hidden = value;
        }

        [Serializable]
        class HiddenItem : HiddenItemBase
        {
            public int value;
        }

        class HiddenHost : ScriptableObject
        {
            public HiddenItem item;
            public List<HiddenItem> items;
        }

        [Test]
        public void GetValue_PrivateBaseFieldAndListIndex_ReadsCurrentValue()
        {
            var host = ScriptableObject.CreateInstance<HiddenHost>();
            try
            {
                var direct = new HiddenItem { value = 2 };
                var at0 = new HiddenItem { value = 1 };
                var at1 = new HiddenItem { value = 3 };
                direct.SetHidden(6);
                at0.SetHidden(8);
                at1.SetHidden(5);
                host.item = direct;
                host.items = new List<HiddenItem> { at0, at1 };

                var serializedObject = new SerializedObject(host);
                var hidden = serializedObject.FindProperty("item.hidden");
                var firstHidden = serializedObject.FindProperty("items.Array.data[0].hidden");
                var secondHidden = serializedObject.FindProperty("items.Array.data[1].hidden");

                Assert.That(hidden, Is.Not.Null);
                Assert.That(firstHidden, Is.Not.Null);
                Assert.That(secondHidden, Is.Not.Null);
                Assert.That(hidden.GetValue<int>(), Is.EqualTo(6));
                Assert.That(firstHidden.GetValue<int>(), Is.EqualTo(8));
                Assert.That(secondHidden.GetValue<int>(), Is.EqualTo(5));

                direct.SetHidden(7);
                at0.SetHidden(4);
                Assert.That(hidden.GetValue<int>(), Is.EqualTo(7));
                Assert.That(firstHidden.GetValue<int>(), Is.EqualTo(4));
                Assert.That(secondHidden.GetValue<int>(), Is.EqualTo(5));

                Assert.That(serializedObject.FindProperty("items").GetDeclaredObject(), Is.SameAs(host));
                Assert.That(firstHidden.GetDeclaredObject(), Is.SameAs(at0));
                Assert.That(firstHidden.GetParentObject(), Is.EqualTo(4));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void SetValue_NestedField_WritesPublicFieldOnly()
        {
            var host = ScriptableObject.CreateInstance<HiddenHost>();
            try
            {
                host.item = new HiddenItem { value = 1 };
                host.item.SetHidden(6);

                var serializedObject = new SerializedObject(host);
                var hidden = serializedObject.FindProperty("item.hidden");
                var publicValue = serializedObject.FindProperty("item.value");

                Assert.That(hidden.SetValue(1), Is.False);
                Assert.That(hidden.GetValue<int>(), Is.EqualTo(6));
                Assert.That(hidden.SetValue(1), Is.False);
                Assert.That(hidden.GetValue<int>(), Is.EqualTo(6));

                Assert.That(publicValue.SetValue(9), Is.True);
                Assert.That(host.item.value, Is.EqualTo(9));
                Assert.That(publicValue.GetValue<int>(), Is.EqualTo(9));
                Assert.That(publicValue.SetValue(3), Is.True);
                Assert.That(publicValue.GetValue<int>(), Is.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
