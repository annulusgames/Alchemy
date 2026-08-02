#if ALCHEMY_SUPPORT_SERIALIZATION
using NUnit.Framework;
using UnityEngine;

namespace Alchemy.Tests.Serialization.EditMode
{
    public class UnityValueTypeTest
    {
        [Test]
        public void Test_RoundTrip_InspectorSupportedUnityValueTypes()
        {
            Assert.That(TestUtility.RoundTrip(new Vector2(1.25f, -2.5f)), Is.EqualTo(new Vector2(1.25f, -2.5f)));
            Assert.That(TestUtility.RoundTrip(new Vector2Int(12, -25)), Is.EqualTo(new Vector2Int(12, -25)));
            Assert.That(TestUtility.RoundTrip(new Vector3(1.25f, -2.5f, 5f)), Is.EqualTo(new Vector3(1.25f, -2.5f, 5f)));
            Assert.That(TestUtility.RoundTrip(new Vector3Int(12, -25, 50)), Is.EqualTo(new Vector3Int(12, -25, 50)));
            Assert.That(TestUtility.RoundTrip(new Vector4(1.25f, -2.5f, 5f, 10f)), Is.EqualTo(new Vector4(1.25f, -2.5f, 5f, 10f)));
            Assert.That(TestUtility.RoundTrip(new Rect(1.25f, -2.5f, 5f, 10f)), Is.EqualTo(new Rect(1.25f, -2.5f, 5f, 10f)));
            Assert.That(TestUtility.RoundTrip(new RectInt(12, -25, 50, 100)), Is.EqualTo(new RectInt(12, -25, 50, 100)));
            Assert.That(
                TestUtility.RoundTrip(new Bounds(new Vector3(1f, 2f, 3f), new Vector3(4f, 5f, 6f))),
                Is.EqualTo(new Bounds(new Vector3(1f, 2f, 3f), new Vector3(4f, 5f, 6f))));
            Assert.That(
                TestUtility.RoundTrip(new BoundsInt(new Vector3Int(1, 2, 3), new Vector3Int(4, 5, 6))),
                Is.EqualTo(new BoundsInt(new Vector3Int(1, 2, 3), new Vector3Int(4, 5, 6))));
            Assert.That(TestUtility.RoundTrip(new Color(0.125f, 0.25f, 0.5f, 0.75f)), Is.EqualTo(new Color(0.125f, 0.25f, 0.5f, 0.75f)));
        }
    }
}
#endif
