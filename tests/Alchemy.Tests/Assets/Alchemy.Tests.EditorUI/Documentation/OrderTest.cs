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

        [Group("Group3", 30)]
        public float group3Foo;

        [Group("Group3", 30)]
        public string group3Bar;

        [Group("Group3", 30)]
        public bool group3Baz;

        [Group("Group2", 20)]
        public float group2Foo;

        [Group("Group2", 20)]
        public string group2Bar;

        [Group("Group2", 20)]
        public bool group2Baz;

        [Group("Group1", 10)]
        public float group1Foo;

        [Group("Group1", 10)]
        public string group1Bar;

        [Group("Group1", 10)]
        public bool group1Baz;
    }
}
