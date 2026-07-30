#if ALCHEMY_SUPPORT_SERIALIZATION
using System;
using System.Collections.Generic;
using Alchemy.Serialization.Internal;
using NUnit.Framework;
using UnityEngine;

namespace Alchemy.Tests.EditMode.Serialization
{
    [Serializable]
    class SerializableClassData
    {
        public int id;
        public string name;
        public SerializableStructData nested;
        public List<string> values;
    }

    [Serializable]
    struct SerializableStructData
    {
        public bool enabled;
        public Vector3 position;
        public (int Min, int Max) range;
        public double ratio;
    }

    public class CompositeTest
    {
        [Test]
        public void Test_RoundTrip_ClassConsistingOfSupportedTypes()
        {
            var value = CreateClassData();

            var after = TestUtility.RoundTrip(value);

            AssertClassData(after, value);
        }

        [Test]
        public void Test_RoundTrip_StructConsistingOfSupportedTypes()
        {
            var value = CreateStructData();

            var after = TestUtility.RoundTrip(value);

            Assert.That(after.enabled, Is.EqualTo(value.enabled));
            Assert.That(after.position, Is.EqualTo(value.position));
            Assert.That(after.range, Is.EqualTo(value.range));
            Assert.That(after.ratio, Is.EqualTo(value.ratio));
        }

        [Test]
        public void Test_FromJsonOverride_UpdatesExistingClass()
        {
            var objectReferences = new List<UnityEngine.Object>();
            var expected = CreateClassData();
            var json = SerializationHelper.ToJson(expected, objectReferences);
            var actual = new SerializableClassData
            {
                id = -1,
                name = "stale",
                values = new List<string> { "stale" },
            };

            SerializationHelper.FromJsonOverride(json, ref actual, objectReferences);

            AssertClassData(actual, expected);
        }

        static SerializableClassData CreateClassData()
        {
            return new SerializableClassData
            {
                id = 42,
                name = "composite",
                nested = CreateStructData(),
                values = new List<string> { "one", "two" },
            };
        }

        static SerializableStructData CreateStructData()
        {
            return new SerializableStructData
            {
                enabled = true,
                position = new Vector3(1.25f, -2.5f, 5f),
                range = (-10, 20),
                ratio = -98765.5d,
            };
        }

        static void AssertClassData(SerializableClassData actual, SerializableClassData expected)
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
