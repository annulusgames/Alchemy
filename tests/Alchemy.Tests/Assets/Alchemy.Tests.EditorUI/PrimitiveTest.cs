using System;
using UnityEngine;
#if ALCHEMY_SUPPORT_SERIALIZATION
using Alchemy.Serialization;
#endif

namespace Alchemy.Tests.EditorUI
{
#if ALCHEMY_SUPPORT_SERIALIZATION
    [AlchemySerialize]
#endif
    public partial class PrimitiveTest : MonoBehaviour
    {
#if ALCHEMY_SUPPORT_SERIALIZATION
        [AlchemySerializeField, NonSerialized]
        public sbyte sbyteValue = -101;

        [AlchemySerializeField, NonSerialized]
        public byte byteValue = 251;

        [AlchemySerializeField, NonSerialized]
        public short shortValue = -30001;

        [AlchemySerializeField, NonSerialized]
        public ushort ushortValue = 60001;

        [AlchemySerializeField, NonSerialized]
        public int intValue = -2000000001;

        [AlchemySerializeField, NonSerialized]
        public uint uintValue = 4000000001u;

        [AlchemySerializeField, NonSerialized]
        public long longValue = -900000000000000001L;

        [AlchemySerializeField, NonSerialized]
        public ulong ulongValue = 18000000000000000001UL;

        [AlchemySerializeField, NonSerialized]
        public float floatValue = 123.625f;

        [AlchemySerializeField, NonSerialized]
        public double doubleValue = -98765.5d;

        [AlchemySerializeField, NonSerialized]
        public decimal decimalValue = 1234567890.1234567890123456789m;

        [AlchemySerializeField, NonSerialized]
        public bool boolValue = true;

        [AlchemySerializeField, NonSerialized]
        public char charValue = '結';

        [AlchemySerializeField, NonSerialized]
        public string stringValue = "Alchemy \"JSON\" \n 団結";
#endif
    }
}
