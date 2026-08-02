#if ALCHEMY_SUPPORT_SERIALIZATION
using NUnit.Framework;
using UnityEngine;

namespace Alchemy.Tests.Serialization.EditMode
{
    public class AnimationCurveTest
    {
        [Test]
        public void Test_RoundTrip_AnimationCurve()
        {
            var before = new AnimationCurve(
                new Keyframe(0.25f, 0.75f, -1.5f, 2.5f, 0.2f, 0.8f) { weightedMode = WeightedMode.Both },
                new Keyframe(1.5f, -0.25f, 3.5f, -4.5f, 0.1f, 0.9f) { weightedMode = WeightedMode.Out })
            {
                preWrapMode = WrapMode.PingPong,
                postWrapMode = WrapMode.Loop,
            };

            var after = TestUtility.RoundTrip(before);

            Assert.That(after, Is.Not.Null);
            Assert.That(after.preWrapMode, Is.EqualTo(before.preWrapMode));
            Assert.That(after.postWrapMode, Is.EqualTo(before.postWrapMode));
            Assert.That(after.keys, Has.Length.EqualTo(before.keys.Length));
            for (var i = 0; i < before.keys.Length; i++)
            {
                AssertKeyframe(after.keys[i], before.keys[i]);
            }
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

            var after = TestUtility.RoundTrip(before);

            AssertKeyframe(after, before);
        }

        [Test]
        public void Test_RoundTrip_NullAnimationCurve()
        {
            Assert.That(TestUtility.RoundTrip<AnimationCurve>(null), Is.Null);
        }

        static void AssertKeyframe(Keyframe actual, Keyframe expected)
        {
            Assert.That(actual.time, Is.EqualTo(expected.time));
            Assert.That(actual.value, Is.EqualTo(expected.value));
            Assert.That(actual.inTangent, Is.EqualTo(expected.inTangent));
            Assert.That(actual.outTangent, Is.EqualTo(expected.outTangent));
            Assert.That(actual.inWeight, Is.EqualTo(expected.inWeight));
            Assert.That(actual.outWeight, Is.EqualTo(expected.outWeight));
            Assert.That(actual.weightedMode, Is.EqualTo(expected.weightedMode));
#pragma warning disable 618
            Assert.That(actual.tangentMode, Is.EqualTo(expected.tangentMode));
#pragma warning restore 618
        }
    }
}
#endif
