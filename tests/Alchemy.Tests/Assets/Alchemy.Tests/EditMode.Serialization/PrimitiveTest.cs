#if ALCHEMY_SUPPORT_SERIALIZATION
using NUnit.Framework;

namespace Alchemy.Tests.EditMode.Serialization
{
    public class PrimitiveTest
    {
        enum SignedEnum : long
        {
            Negative = -9876543210L,
        }

        [System.Flags]
        enum FlagsEnum : uint
        {
            First = 1u << 3,
            Second = 1u << 29,
        }

        [Test]
        public void Test_RoundTrip_AllPrimitiveTypes()
        {
            Assert.That(TestUtility.RoundTrip((sbyte)-101), Is.EqualTo((sbyte)-101));
            Assert.That(TestUtility.RoundTrip((byte)251), Is.EqualTo((byte)251));
            Assert.That(TestUtility.RoundTrip((short)-30001), Is.EqualTo((short)-30001));
            Assert.That(TestUtility.RoundTrip((ushort)60001), Is.EqualTo((ushort)60001));
            Assert.That(TestUtility.RoundTrip(-2000000001), Is.EqualTo(-2000000001));
            Assert.That(TestUtility.RoundTrip(4000000001u), Is.EqualTo(4000000001u));
            Assert.That(TestUtility.RoundTrip(-900000000000000001L), Is.EqualTo(-900000000000000001L));
            Assert.That(TestUtility.RoundTrip(18000000000000000001UL), Is.EqualTo(18000000000000000001UL));
            Assert.That(TestUtility.RoundTrip(123.625f), Is.EqualTo(123.625f));
            Assert.That(TestUtility.RoundTrip(-98765.5d), Is.EqualTo(-98765.5d));
            Assert.That(TestUtility.RoundTrip(true), Is.True);
            Assert.That(TestUtility.RoundTrip("Alchemy \"JSON\" \n 団結"), Is.EqualTo("Alchemy \"JSON\" \n 団結"));
            Assert.That(TestUtility.RoundTrip<string>(null), Is.Null);
        }

        [Test]
        public void Test_RoundTrip_EnumTypes()
        {
            Assert.That(TestUtility.RoundTrip(SignedEnum.Negative), Is.EqualTo(SignedEnum.Negative));
            Assert.That(
                TestUtility.RoundTrip(FlagsEnum.First | FlagsEnum.Second),
                Is.EqualTo(FlagsEnum.First | FlagsEnum.Second));
        }
    }
}
#endif
