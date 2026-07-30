using System.Collections.Generic;
using Alchemy.Serialization.Internal;
using UnityEngine;

namespace Alchemy.Tests
{
    public static class TestUtility
    {
#if ALCHEMY_SUPPORT_SERIALIZATION
        public static T RoundTrip<T>(T value, IList<Object> objectReferences = null)
        {
            objectReferences ??= new List<Object>();
            var json = SerializationHelper.ToJson(value, objectReferences);
            return SerializationHelper.FromJson<T>(json, objectReferences);
        }
#endif
    }
}
