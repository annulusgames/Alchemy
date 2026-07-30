#if ALCHEMY_SUPPORT_SERIALIZATION
using System;
using System.Collections.Generic;
using Alchemy.Serialization;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Alchemy.Tests.PlayMode.Serialization
{
    [Serializable]
    class GeneratedClassData
    {
        public int id;
        public string name;
        public GeneratedStructData nested;
        public List<string> values;
    }

    [Serializable]
    struct GeneratedStructData
    {
        public bool enabled;
        public Vector3 position;
        public (int Min, int Max) range;
        public double ratio;
    }

    [AlchemySerialize]
    partial class GeneratedSerializationTarget : IAlchemySerializationCallbackReceiver
    {
        [AlchemySerializeField, NonSerialized] public int primitive;
        [AlchemySerializeField, NonSerialized] public Texture2D unityObject;
        [AlchemySerializeField, NonSerialized] public AnimationCurve animationCurve;
        [AlchemySerializeField, NonSerialized] public Gradient gradient;
        [AlchemySerializeField, NonSerialized] public int[] array;
        [AlchemySerializeField, NonSerialized] public List<string> list;
        [AlchemySerializeField, NonSerialized] public HashSet<int> hashSet;
        [AlchemySerializeField, NonSerialized] public Dictionary<string, Texture2D> dictionary;
        [AlchemySerializeField, NonSerialized] public (int Id, string Label) valueTuple;
        [AlchemySerializeField, NonSerialized] public Vector3? nullable;
        [AlchemySerializeField, NonSerialized] public GeneratedClassData classData;
        [AlchemySerializeField, NonSerialized] public GeneratedStructData structData;

        public int beforeSerializeCallbackCount;
        public int afterDeserializeCallbackCount;
        public int primitiveObservedAfterDeserialize;

        void IAlchemySerializationCallbackReceiver.OnBeforeSerialize()
        {
            beforeSerializeCallbackCount++;
        }

        void IAlchemySerializationCallbackReceiver.OnAfterDeserialize()
        {
            afterDeserializeCallbackCount++;
            primitiveObservedAfterDeserialize = primitive;
        }
    }

    public class GeneratedTest
    {
        [Test]
        public void Test_GeneratedCallbacks_RoundTrip_AllSupportedTypeCategories()
        {
            var texture = new Texture2D(1, 1);
            var target = new GeneratedSerializationTarget
            {
                primitive = 42,
                unityObject = texture,
                animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                gradient = new Gradient
                {
                    colorKeys = new[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.blue, 1f) },
                    alphaKeys = new[] { new GradientAlphaKey(0.25f, 0f), new GradientAlphaKey(0.75f, 1f) },
                    mode = GradientMode.Fixed,
                },
                array = new[] { 1, 2, 3 },
                list = new List<string> { "one", "two" },
                hashSet = new HashSet<int> { 3, 5, 8 },
                dictionary = new Dictionary<string, Texture2D> { ["texture"] = texture },
                valueTuple = (42, "answer"),
                nullable = new Vector3(1.25f, -2.5f, 5f),
                classData = CreateClassData(),
                structData = CreateStructData(),
            };

            try
            {
                var callback = (ISerializationCallbackReceiver)target;
                callback.OnBeforeSerialize();

                target.primitive = default;
                target.unityObject = null;
                target.animationCurve = null;
                target.gradient = null;
                target.array = null;
                target.list = null;
                target.hashSet = null;
                target.dictionary = null;
                target.valueTuple = default;
                target.nullable = null;
                target.classData = null;
                target.structData = default;

                callback.OnAfterDeserialize();

                Assert.That(target.beforeSerializeCallbackCount, Is.EqualTo(1));
                Assert.That(target.afterDeserializeCallbackCount, Is.EqualTo(1));
                Assert.That(target.primitiveObservedAfterDeserialize, Is.EqualTo(42));
                Assert.That(target.primitive, Is.EqualTo(42));
                Assert.That(target.unityObject, Is.SameAs(texture));
                Assert.That(target.animationCurve, Is.EqualTo(AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)));
                Assert.That(target.gradient.mode, Is.EqualTo(GradientMode.Fixed));
                CollectionAssert.AreEqual(new[] { 1, 2, 3 }, target.array);
                CollectionAssert.AreEqual(new[] { "one", "two" }, target.list);
                CollectionAssert.AreEquivalent(new[] { 3, 5, 8 }, target.hashSet);
                Assert.That(target.dictionary["texture"], Is.SameAs(texture));
                Assert.That(target.valueTuple, Is.EqualTo((42, "answer")));
                Assert.That(target.nullable, Is.EqualTo(new Vector3(1.25f, -2.5f, 5f)));
                AssertClassData(target.classData, CreateClassData());
                Assert.That(target.structData.enabled, Is.True);
                Assert.That(target.structData.position, Is.EqualTo(new Vector3(1.25f, -2.5f, 5f)));
                Assert.That(target.structData.range, Is.EqualTo((-10, 20)));
                Assert.That(target.structData.ratio, Is.EqualTo(-98765.5d));
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        static GeneratedClassData CreateClassData()
        {
            return new GeneratedClassData
            {
                id = 42,
                name = "generated",
                nested = CreateStructData(),
                values = new List<string> { "one", "two" },
            };
        }

        static GeneratedStructData CreateStructData()
        {
            return new GeneratedStructData
            {
                enabled = true,
                position = new Vector3(1.25f, -2.5f, 5f),
                range = (-10, 20),
                ratio = -98765.5d,
            };
        }

        static void AssertClassData(GeneratedClassData actual, GeneratedClassData expected)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.id, Is.EqualTo(expected.id));
            Assert.That(actual.name, Is.EqualTo(expected.name));
            Assert.That(actual.nested.enabled, Is.EqualTo(expected.nested.enabled));
            Assert.That(actual.nested.position, Is.EqualTo(expected.nested.position));
            Assert.That(actual.nested.range, Is.EqualTo(expected.nested.range));
            Assert.That(actual.nested.ratio, Is.EqualTo(expected.nested.ratio));
            CollectionAssert.AreEqual(expected.values, actual.values);
        }
    }
}
#endif
