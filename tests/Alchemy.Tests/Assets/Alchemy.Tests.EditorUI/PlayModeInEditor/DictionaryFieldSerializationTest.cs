#if ALCHEMY_SUPPORT_SERIALIZATION
using System;
using System.Collections;
using System.Collections.Generic;
using Alchemy.Editor.Elements;
using Alchemy.Serialization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.PlayModeInEditor
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
        [UnityTest]
        public IEnumerator Test_ArrayValueEditSurvivesSerializationRoundTrip()
        {
            return Test_NestedCollectionEditSurvivesSerializationRoundTrip(
                nameof(DictionaryCollectionSerializationTarget.arrayValues));
        }

        [UnityTest]
        public IEnumerator Test_ListValueEditSurvivesSerializationRoundTrip()
        {
            return Test_NestedCollectionEditSurvivesSerializationRoundTrip(
                nameof(DictionaryCollectionSerializationTarget.listValues));
        }

        static IEnumerator Test_NestedCollectionEditSurvivesSerializationRoundTrip(string fieldName)
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
            var foldout = EditorTestUtility.QueryRequired<Foldout>(reflectionField);
            foldout.value = true;
            var window = EditorTestUtility.ShowInWindow(reflectionField);
            try
            {
                yield return null;

                var dictionaryItem = EditorTestUtility.QueryRequired<HashMapFieldBase.HashMapItemBase>(reflectionField);
                var listField = EditorTestUtility.QueryRequired<ListField>(dictionaryItem);
                var listView = EditorTestUtility.QueryRequired<ListView>(listField);
                listView.Rebuild();
                yield return null;

                var textField = EditorTestUtility.QueryRequired<TextField>(
                    listView,
                    field => field.label == "Element 0");
                textField.value = "after";

                var dictionary = (IDictionary)fieldInfo.GetValue(target);
                Assert.That(((IList)dictionary[1])[0], Is.EqualTo("after"));

                callback.OnAfterDeserialize();

                dictionary = (IDictionary)fieldInfo.GetValue(target);
                Assert.That(((IList)dictionary[1])[0], Is.EqualTo("after"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }
    }
}
#endif
