using System;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class ShowInInspectorTest : MonoBehaviour
    {
        [NonSerialized, ShowInInspector]
        public int field;

        [NonSerialized, ShowInInspector]
        public DocumentationSampleClass classField = new();

        [ShowInInspector]
        public int Getter => 10;

        [field: NonSerialized, ShowInInspector]
        public string Property { get; set; } = string.Empty;
    }
}
