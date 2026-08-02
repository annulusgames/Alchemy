#if ALCHEMY_SUPPORT_SERIALIZATION
using NUnit.Framework;
using UnityEngine;

namespace Alchemy.Tests.Serialization.EditMode
{
    public class NullableTest
    {
        [Test]
        public void Test_RoundTrip_Nullable()
        {
            Vector3? value = new Vector3(1.25f, -2.5f, 5f);

            Assert.That(TestUtility.RoundTrip(value), Is.EqualTo(value));
            Assert.That(TestUtility.RoundTrip<Vector3?>(null), Is.Null);
        }
    }
}
#endif
