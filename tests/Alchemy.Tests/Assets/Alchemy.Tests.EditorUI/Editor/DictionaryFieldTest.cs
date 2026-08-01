using System.Collections;
using System.Collections.Generic;
using Alchemy.Editor.Elements;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.Editor
{
    public class DictionaryFieldTest
    {
        sealed class SmallIntegerFieldTarget<T>
        {
            public SmallIntegerFieldTarget(T value)
            {
                this.value = value;
            }

            public T value;
        }

        sealed class SmallIntegerCase
        {
            public SmallIntegerCase(IDictionary dictionary, object key)
            {
                Dictionary = dictionary;
                Key = key;
            }

            public IDictionary Dictionary { get; }
            public object Key { get; }
        }

        sealed class SmallIntegerFieldCase
        {
            public SmallIntegerFieldCase(object target, int input, object initialValue, object expectedValue)
            {
                Target = target;
                Input = input;
                InitialValue = initialValue;
                ExpectedValue = expectedValue;
            }

            public object Target { get; }
            public int Input { get; }
            public object InitialValue { get; }
            public object ExpectedValue { get; }
        }

        [UnityTest]
        public IEnumerator Test_SmallIntegerKeyInputCanBeEdited()
        {
            var cases = new[]
            {
                new SmallIntegerCase(new Dictionary<sbyte, int>(), (sbyte)-7),
                new SmallIntegerCase(new Dictionary<byte, int>(), (byte)7),
                new SmallIntegerCase(new Dictionary<short, int>(), (short)-300),
                new SmallIntegerCase(new Dictionary<ushort, int>(), (ushort)600),
            };

            foreach (var testCase in cases)
            {
                var field = new DictionaryField(testCase.Dictionary, "Dictionary");
                var window = EditorTestUtility.ShowInWindow(field);
                try
                {
                    yield return null;

                    var addButton = EditorTestUtility.QueryRequired<Button>(
                        field,
                        button => button.text == "+ Add");
                    EditorTestUtility.Click(addButton);

                    var item = EditorTestUtility.QueryRequired<HashMapFieldBase.HashMapItemBase>(field);
                    var keyField = EditorTestUtility.QueryRequired<IntegerField>(
                        item,
                        integerField => integerField.label == "Key");
                    keyField.value = System.Convert.ToInt32(testCase.Key);

                    Assert.That(addButton.text, Is.EqualTo("Done"));
                    EditorTestUtility.Click(addButton);

                    Assert.That(testCase.Dictionary.Count, Is.EqualTo(1));
                    Assert.That(testCase.Dictionary.Contains(testCase.Key), Is.True);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(window);
                }
            }
        }

        [UnityTest]
        public IEnumerator Test_SmallIntegerFieldsDeferClampedValueUntilFocusOut()
        {
            var cases = new[]
            {
                new SmallIntegerFieldCase(
                    new SmallIntegerFieldTarget<sbyte>(1),
                    sbyte.MinValue - 1,
                    (sbyte)1,
                    sbyte.MinValue),
                new SmallIntegerFieldCase(
                    new SmallIntegerFieldTarget<byte>(1),
                    byte.MaxValue + 1,
                    (byte)1,
                    byte.MaxValue),
                new SmallIntegerFieldCase(
                    new SmallIntegerFieldTarget<short>(1),
                    short.MinValue - 1,
                    (short)1,
                    short.MinValue),
                new SmallIntegerFieldCase(
                    new SmallIntegerFieldTarget<ushort>(1),
                    ushort.MaxValue + 1,
                    (ushort)1,
                    ushort.MaxValue),
            };

            foreach (var testCase in cases)
            {
                var fieldInfo = testCase.Target.GetType().GetField("value");
                Assert.That(fieldInfo, Is.Not.Null);

                var reflectionField = new ReflectionField(testCase.Target, fieldInfo);
                var window = EditorTestUtility.ShowInWindow(reflectionField);
                try
                {
                    yield return null;

                    var integerField = EditorTestUtility.QueryRequired<IntegerField>(
                        reflectionField,
                        field => field.label == "Value");
                    integerField.Focus();
                    yield return null;

                    integerField.value = testCase.Input;

                    Assert.That(integerField.value, Is.EqualTo(System.Convert.ToInt32(testCase.ExpectedValue)));
                    Assert.That(fieldInfo.GetValue(testCase.Target), Is.EqualTo(testCase.InitialValue));

                    integerField.Blur();
                    yield return null;

                    Assert.That(fieldInfo.GetValue(testCase.Target), Is.EqualTo(testCase.ExpectedValue));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(window);
                }
            }
        }
    }
}
