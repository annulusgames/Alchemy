using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class OrderTest : MonoBehaviour
    {
        [Order(2)]
        public float foo;

        [Order(1)]
        public Vector3 bar;

        [Order(0)]
        public GameObject baz;
    }
}
