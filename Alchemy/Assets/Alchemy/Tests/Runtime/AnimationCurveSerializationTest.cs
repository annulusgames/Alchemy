#if ALCHEMY_SUPPORT_SERIALIZATION
using System.Collections.Generic;
using Alchemy.Serialization.Internal;
using NUnit.Framework;
using UnityEngine;

namespace Alchemy.Tests.Runtime
{
    public class AnimationCurveSerializationTest
    {
        readonly List<Object> objects = new();

        [TearDown]
        public void TearDown()
        {
            objects.Clear();
        }

        [Test]
        public void Test_RoundTrip_AnimationCurve()
        {
            var before = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            var beforeJson = SerializationHelper.ToJson(before, objects);
            Debug.Log(beforeJson);
            var after = SerializationHelper.FromJson<AnimationCurve>(beforeJson, objects);

            Assert.AreEqual(before, after);
        }

        [Test]
        public void Test_RoundTrip_Keyframe()
        {
            var before = new Keyframe(0.25f, 0.75f, -1.5f, 2.5f, 0.2f, 0.8f)
            {
                weightedMode = WeightedMode.Both,
#pragma warning disable 618
                tangentMode = 34,
#pragma warning restore 618
            };

            var json = SerializationHelper.ToJson(before, objects);
            var after = SerializationHelper.FromJson<Keyframe>(json, objects);

            Assert.AreEqual(before.inWeight, after.inWeight);
            Assert.AreEqual(before.outWeight, after.outWeight);
            Assert.AreEqual(before.weightedMode, after.weightedMode);
#pragma warning disable 618
            Assert.AreEqual(before.tangentMode, after.tangentMode);
#pragma warning restore 618
        }
    }
}
#endif
