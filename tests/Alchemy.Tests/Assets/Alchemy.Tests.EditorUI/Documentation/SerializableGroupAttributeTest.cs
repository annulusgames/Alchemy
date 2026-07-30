using System;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class SerializableGroupAttributeTest : MonoBehaviour
    {
        [Serializable]
        [BoxGroup]
        public sealed class Example
        {
            public float foo;
            public Vector3 bar;
            public GameObject baz;
        }

        public Example example;
    }
}
