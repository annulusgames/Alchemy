#if ALCHEMY_SUPPORT_SERIALIZATION
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Alchemy.Tests.EditMode.Serialization
{
    public class CollectionTest
    {
        [Test]
        public void Test_RoundTrip_Array()
        {
            var values = new[] { -1, 0, 42 };

            CollectionAssert.AreEqual(values, TestUtility.RoundTrip(values));
            CollectionAssert.IsEmpty(TestUtility.RoundTrip(System.Array.Empty<int>()));
            Assert.That(TestUtility.RoundTrip<int[]>(null), Is.Null);
        }

        [Test]
        public void Test_RoundTrip_List()
        {
            var values = new List<string> { "first", null, "third" };

            CollectionAssert.AreEqual(values, TestUtility.RoundTrip(values));
            CollectionAssert.IsEmpty(TestUtility.RoundTrip(new List<string>()));
            Assert.That(TestUtility.RoundTrip<List<string>>(null), Is.Null);
        }

        [Test]
        public void Test_RoundTrip_HashSet()
        {
            var values = new HashSet<int> { -1, 3, 8 };

            CollectionAssert.AreEquivalent(values, TestUtility.RoundTrip(values));
            CollectionAssert.IsEmpty(TestUtility.RoundTrip(new HashSet<int>()));
            Assert.That(TestUtility.RoundTrip<HashSet<int>>(null), Is.Null);
        }

        [Test]
        public void Test_RoundTrip_Dictionary()
        {
            var values = new Dictionary<string, int>
            {
                ["negative"] = -1,
                ["positive"] = 42,
            };

            CollectionAssert.AreEquivalent(values, TestUtility.RoundTrip(values));
            CollectionAssert.IsEmpty(TestUtility.RoundTrip(new Dictionary<string, int>()));
            Assert.That(TestUtility.RoundTrip<Dictionary<string, int>>(null), Is.Null);
        }

        [Test]
        public void Test_RoundTrip_ValueTuple()
        {
            var value = (Id: 42, Label: "answer", Position: new Vector3(1.25f, -2.5f, 5f));

            Assert.That(TestUtility.RoundTrip(value), Is.EqualTo(value));
        }

        [Test]
        public void Test_RoundTrip_NestedCollections()
        {
            var value = new Dictionary<string, List<(int Id, bool Enabled)>>
            {
                ["first"] = new List<(int, bool)> { (1, true), (2, false) },
                ["empty"] = new List<(int, bool)>(),
            };

            var after = TestUtility.RoundTrip(value);

            Assert.That(after.Keys, Is.EquivalentTo(value.Keys));
            Assert.That(after["first"], Is.EqualTo(value["first"]));
            CollectionAssert.IsEmpty(after["empty"]);
        }
    }
}
#endif
