using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class IndentTest : MonoBehaviour
    {
        [Indent]
        public float foo;

        [Indent(2)]
        public Vector2 bar;

        [Indent(3)]
        public GameObject baz;
    }
}
