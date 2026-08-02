#if ALCHEMY_SUPPORT_SERIALIZATION
using NUnit.Framework;
using UnityEngine;

namespace Alchemy.Tests.Serialization.EditMode
{
    public class GradientTest
    {
        [Test]
        public void Test_RoundTrip_Gradient()
        {
            var before = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.blue, 1f) },
                alphaKeys = new[] { new GradientAlphaKey(0.25f, 0f), new GradientAlphaKey(0.75f, 1f) },
                mode = GradientMode.Fixed,
            };

            var after = TestUtility.RoundTrip(before);

            Assert.AreEqual(before, after);
        }

        [Test]
        public void Test_RoundTrip_GradientColorKey()
        {
            var before = new GradientColorKey { color = Color.black, time = 1f };
            var after = TestUtility.RoundTrip(before);

            Assert.AreEqual(before, after);
        }

        [Test]
        public void Test_RoundTrip_GradientAlphaKey()
        {
            var before = new GradientAlphaKey { alpha = 0.5f, time = 1f };
            var after = TestUtility.RoundTrip(before);

            Assert.AreEqual(before, after);
        }

#if UNITY_2022_2_OR_NEWER
        [Test]
        public void Test_RoundTrip_PreservesColorSpace()
        {
            var before = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.white, 0f) },
                alphaKeys = new[] { new GradientAlphaKey(1f, 0f) },
                colorSpace = ColorSpace.Linear,
            };

            var after = TestUtility.RoundTrip(before);

            Assert.That(after.colorSpace, Is.EqualTo(ColorSpace.Linear));
        }
#endif

        [Test]
        public void Test_RoundTrip_NullGradient()
        {
            Assert.That(TestUtility.RoundTrip<Gradient>(null), Is.Null);
        }
    }
}
#endif
