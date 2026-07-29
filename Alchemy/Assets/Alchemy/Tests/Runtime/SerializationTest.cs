#if ALCHEMY_SUPPORT_SERIALIZATION
using System.Collections.Generic;
using Alchemy.Serialization.Internal;
using NUnit.Framework;
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
    }
}
#endif
