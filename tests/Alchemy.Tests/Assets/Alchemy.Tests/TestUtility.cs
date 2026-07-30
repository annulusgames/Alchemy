#if ALCHEMY_SUPPORT_SERIALIZATION
using System.Collections.Generic;
using Alchemy.Serialization.Internal;
using UnityEngine;

namespace Alchemy.Tests
{
    public static class TestUtility
    {
        public static T RoundTrip<T>(T value, IList<Object> objectReferences = null)
        {
            objectReferences ??= new List<Object>();
            var json = SerializationHelper.ToJson(value, objectReferences);
            return SerializationHelper.FromJson<T>(json, objectReferences);
        }
    }
}
#endif
