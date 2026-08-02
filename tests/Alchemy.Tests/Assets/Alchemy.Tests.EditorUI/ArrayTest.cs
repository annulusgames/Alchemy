using System;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    public sealed class ArrayTest : MonoBehaviour
    {
        [NonSerialized, ShowInInspector]
        public int[] array = { 10, 20 };

        [NonSerialized, ShowInInspector]
        public string[] nullArray;
    }
}
