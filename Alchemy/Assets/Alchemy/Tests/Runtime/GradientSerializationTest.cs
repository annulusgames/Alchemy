#if ALCHEMY_SUPPORT_SERIALIZATION
using System.Collections.Generic;
using Alchemy.Serialization.Internal;
using NUnit.Framework;
using UnityEngine;

namespace Alchemy.Tests.Runtime
{
    public class GradientSerializationTest
    {
        readonly List<Object> objects = new();

        [TearDown]
        public void TearDown()
        {
            objects.Clear();
        }

        [Test]
        public void Test_RoundTrip_Gradient()
        {
            var before = new Gradient
            {
                colorKeys = new GradientColorKey[] { new(Color.white, 0f) },
                alphaKeys = new GradientAlphaKey[] { new(0f, 1f), new(1f, 1f) },
                mode = GradientMode.Blend,
            };
            var beforeJson = SerializationHelper.ToJson(before, objects);
            var after = SerializationHelper.FromJson<Gradient>(beforeJson, objects);

            Assert.AreEqual(before, after);
        }

        [Test]
        public void Test_RoundTrip_GradientColorKey()
        {
            var before = new GradientColorKey { color = Color.black, time = 1f };
            var beforeJson = SerializationHelper.ToJson(before, objects);
            var after = SerializationHelper.FromJson<GradientColorKey>(beforeJson, objects);

            Assert.AreEqual(before, after);
        }

        [Test]
        public void Test_RoundTrip_GradientAlphaKey()
        {
            var before = new GradientAlphaKey { alpha = 0.5f, time = 1f };
            var beforeJson = SerializationHelper.ToJson(before, objects);
            var after = SerializationHelper.FromJson<GradientAlphaKey>(beforeJson, objects);

            Assert.AreEqual(before, after);
        }

#if UNITY_2022_2_OR_NEWER
        [Test]
        public void Test_RoundTrip_PreservesColorSpace()
        {
            var beforeJson = SerializationHelper.ToJson(ColorSpace.Linear, objects);
            var after = SerializationHelper.FromJson<ColorSpace>(beforeJson, objects);

            Assert.AreEqual(ColorSpace.Linear, after);
        }
#endif
    }
}
#endif
