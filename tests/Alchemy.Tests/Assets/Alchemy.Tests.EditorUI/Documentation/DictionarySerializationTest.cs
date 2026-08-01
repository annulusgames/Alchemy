using System;
using System.Collections.Generic;
using Alchemy.Inspector;
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
    public partial class DictionarySerializationTest : MonoBehaviour
    {
#if ALCHEMY_SUPPORT_SERIALIZATION
        [HorizontalGroup("8-bit")]
        [AlchemySerializeField, NonSerialized]
        public Dictionary<sbyte, int> sbyteKeys = new() { [(sbyte)-101] = 1 };

        [HorizontalGroup("8-bit")]
        [AlchemySerializeField, NonSerialized]
        public Dictionary<byte, int> byteKeys = new() { [(byte)251] = 2 };

        [HorizontalGroup("16-bit")]
        [AlchemySerializeField, NonSerialized]
        public Dictionary<short, int> shortKeys = new() { [(short)-30001] = 3 };

        [HorizontalGroup("16-bit")]
        [AlchemySerializeField, NonSerialized]
        public Dictionary<ushort, int> ushortKeys = new() { [(ushort)60001] = 4 };

        [HorizontalGroup("32-bit")]
        [AlchemySerializeField, NonSerialized]
        public Dictionary<int, int> intKeys = new() { [-2000000001] = 5 };

        [HorizontalGroup("32-bit")]
        [AlchemySerializeField, NonSerialized]
        public Dictionary<uint, int> uintKeys = new() { [4000000001u] = 6 };

        [HorizontalGroup("64-bit")]
        [AlchemySerializeField, NonSerialized]
        public Dictionary<long, int> longKeys = new() { [-900000000000000001L] = 7 };

        [HorizontalGroup("64-bit")]
        [AlchemySerializeField, NonSerialized]
        public Dictionary<ulong, int> ulongKeys = new() { [18000000000000000001UL] = 8 };

        [HorizontalGroup("Floating Point")]
        [AlchemySerializeField, NonSerialized]
        public Dictionary<float, int> floatKeys = new() { [123.625f] = 9 };

        [HorizontalGroup("Floating Point")]
        [AlchemySerializeField, NonSerialized]
        public Dictionary<double, int> doubleKeys = new() { [-98765.5d] = 10 };

        [HorizontalGroup("Other")]
        [AlchemySerializeField, NonSerialized]
        public Dictionary<bool, int> boolKeys = new() { [true] = 11 };

        [HorizontalGroup("Other")]
        [AlchemySerializeField, NonSerialized]
        public Dictionary<string, int> stringKeys = new() { ["Alchemy"] = 12 };
#endif
    }
}
