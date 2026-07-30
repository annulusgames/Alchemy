using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class HorizontalLineTest : MonoBehaviour
    {
        [HorizontalLine]
        public float foo;

        [HorizontalLine(1f, 0f, 0f)]
        public Vector2 bar;

        [HorizontalLine(1f, 0.5f, 0f, 0.5f)]
        public GameObject baz;
    }
}
