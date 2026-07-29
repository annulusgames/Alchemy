#if ALCHEMY_SUPPORT_SERIALIZATION
using System.Collections.Generic;
using Alchemy.Serialization.Internal;
using NUnit.Framework;
using Unity.Serialization.Json;
using UnityEngine;

namespace Alchemy.Tests.Runtime
{
    public class SerializationTest
    {
        readonly List<Object> objects = new();

        [TearDown]
        public void TearDown()
        {
            objects.Clear();
        }

        [Test]
        public void Test_Serialize_Deserialize_AnimationCurve()
        {
            var before = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            var beforeJson = SerializationHelper.ToJson(before, objects);
            Debug.Log(beforeJson);
            var after = SerializationHelper.FromJson<AnimationCurve>(beforeJson, objects);

            Assert.AreEqual(before, after);
        }

        [Test]
        public void Test_Serialize_Deserialize_Gradient()
        {
            var before = new Gradient()
            {
                colorKeys = new GradientColorKey[] { new(Color.white, 0f) },
                alphaKeys = new GradientAlphaKey[] { new(0f, 1f), new(1f, 1f) },
                mode = GradientMode.Blend,
            };
            var beforeJson = SerializationHelper.ToJson(before, objects);
            Debug.Log(beforeJson);
            var after = SerializationHelper.FromJson<Gradient>(beforeJson, objects);

            Assert.AreEqual(before, after);
        }

        [Test]
        public void Test_Serialize_Deserialize_GradientColorKey()
        {
            var before = new GradientColorKey() { color = Color.black, time = 1f };
            var beforeJson = SerializationHelper.ToJson(before, objects);
            Debug.Log(beforeJson);
            var after = SerializationHelper.FromJson<GradientColorKey>(beforeJson, objects);

            Assert.AreEqual(before, after);
        }

        [Test]
        public void Test_Serialize_Deserialize_GradientAlphaKey()
        {
            var before = new GradientAlphaKey() { alpha = 0.5f, time = 1f };
            var beforeJson = SerializationHelper.ToJson(before, objects);
            Debug.Log(beforeJson);
            var after = SerializationHelper.FromJson<GradientAlphaKey>(beforeJson, objects);

            Assert.AreEqual(before, after);
        }
    }
}
#endif
