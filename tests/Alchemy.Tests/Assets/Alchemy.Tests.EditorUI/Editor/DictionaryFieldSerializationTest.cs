#if ALCHEMY_SUPPORT_SERIALIZATION
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Alchemy.Editor.Elements;
using Alchemy.Serialization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.Editor
{
    [AlchemySerialize]
    internal partial class DictionaryCollectionSerializationTarget
    {
        [AlchemySerializeField, NonSerialized]
        public Dictionary<int, string[]> arrayValues = new();

        [AlchemySerializeField, NonSerialized]
        public Dictionary<int, List<string>> listValues = new();
    }

    public class DictionaryFieldSerializationTest
    {
        [TestCase(nameof(DictionaryCollectionSerializationTarget.arrayValues))]
        [TestCase(nameof(DictionaryCollectionSerializationTarget.listValues))]
        public void Test_NestedCollectionEditSurvivesSerializationRoundTrip(string fieldName)
        {
            var target = new DictionaryCollectionSerializationTarget
            {
                arrayValues = new Dictionary<int, string[]> { [1] = new[] { "before" } },
                listValues = new Dictionary<int, List<string>> { [1] = new() { "before" } },
            };
            var callback = (ISerializationCallbackReceiver)target;
            callback.OnBeforeSerialize();

            var fieldInfo = typeof(DictionaryCollectionSerializationTarget).GetField(fieldName);
            Assert.That(fieldInfo, Is.Not.Null);

            var reflectionField = new ReflectionField(target, fieldInfo);
            var nestedListField = reflectionField.Q<ListField>();
            Assert.That(nestedListField, Is.Not.Null);

            var dictionary = (IDictionary)fieldInfo.GetValue(target);
            var nestedCollection = (IList)dictionary[1];
            nestedCollection[0] = "after";

            // Simulate the notification emitted after the nested ListField updates its collection.
            var notifyOnValueChanged = typeof(ListField)
                .GetMethod("NotifyOnValueChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(notifyOnValueChanged, Is.Not.Null);
            notifyOnValueChanged.Invoke(nestedListField, null);

            Assert.That(((IList)dictionary[1])[0], Is.EqualTo("after"));

            callback.OnAfterDeserialize();

            dictionary = (IDictionary)fieldInfo.GetValue(target);
            Assert.That(((IList)dictionary[1])[0], Is.EqualTo("after"));
        }
    }
}
#endif
