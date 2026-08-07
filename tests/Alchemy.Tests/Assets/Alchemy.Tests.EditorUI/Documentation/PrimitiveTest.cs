using System;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class PrimitiveTest : MonoBehaviour
    {
        public sbyte sbyteValue = -101;
        public byte byteValue = 251;
        public short shortValue = -30001;
        public ushort ushortValue = 60001;
        public int intValue = -2000000001;
        public uint uintValue = 4000000001u;
        public long longValue = -900000000000000001L;
        public ulong ulongValue = 18000000000000000001UL;
        public float floatValue = 123.625f;
        public double doubleValue = -98765.5d;

        [NonSerialized, ShowInInspector]
        public decimal decimalValue = 1234567890.1234567890123456789m;

        public bool boolValue = true;
        public char charValue = '結';
        public string stringValue = "Alchemy \"JSON\" \n 団結";
    }
}
