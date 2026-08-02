#if ALCHEMY_SUPPORT_SERIALIZATION
using System;
using System.Collections.Generic;
using Alchemy.Serialization.Internal;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Alchemy.Tests.Serialization.EditMode
{
    [Serializable]
    class UnityObjectContainer
    {
        public Texture2D typedReference;
        public Object baseReference;
        public List<Object> references;
    }

    public class UnityObjectTest
    {
        [Test]
        public void Test_RoundTrip_UnityObject()
        {
            var texture = new Texture2D(1, 1);
            var objectReferences = new List<Object>();

            try
            {
                var after = TestUtility.RoundTrip(texture, objectReferences);

                Assert.That(after, Is.SameAs(texture));
                Assert.That(objectReferences, Has.Count.EqualTo(1));
                Assert.That(objectReferences[0], Is.SameAs(texture));
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void Test_RoundTrip_NestedUnityObjects_DeduplicatesReferenceList()
        {
            var texture = new Texture2D(1, 1);
            var objectReferences = new List<Object>();
            var value = new UnityObjectContainer
            {
                typedReference = texture,
                baseReference = texture,
                references = new List<Object> { texture, null, texture },
            };

            try
            {
                var after = TestUtility.RoundTrip(value, objectReferences);

                Assert.That(after.typedReference, Is.SameAs(texture));
                Assert.That(after.baseReference, Is.SameAs(texture));
                Assert.That(after.references[0], Is.SameAs(texture));
                Assert.That(after.references[1], Is.Null);
                Assert.That(after.references[2], Is.SameAs(texture));
                Assert.That(objectReferences, Has.Count.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void Test_DeserializeUnityObject_InvalidReferenceIndex_ReturnsNull()
        {
            var after = SerializationHelper.FromJson<Texture2D>("100", new List<Object>());

            Assert.That(after, Is.Null);
        }
    }
}
#endif
