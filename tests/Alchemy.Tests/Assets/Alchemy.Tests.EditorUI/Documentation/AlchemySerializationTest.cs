using System;
using System.Collections.Generic;
using UnityEngine;
#if ALCHEMY_SUPPORT_SERIALIZATION
using Alchemy.Serialization;
#endif

namespace Alchemy.Tests.EditorUI
{
#if ALCHEMY_SUPPORT_SERIALIZATION
    [AlchemySerialize]
#endif
    [DocumentationSample]
    public partial class AlchemySerializationTest : MonoBehaviour
    {
#if ALCHEMY_SUPPORT_SERIALIZATION
        [AlchemySerializeField, NonSerialized]
        public HashSet<GameObject> hashSet = new();

        [AlchemySerializeField, NonSerialized]
        public Dictionary<string, GameObject> dictionary = new();

        [AlchemySerializeField, NonSerialized]
        public (int, int) tuple;

        [AlchemySerializeField, NonSerialized]
        public Vector3? nullable;
#endif
    }
}
