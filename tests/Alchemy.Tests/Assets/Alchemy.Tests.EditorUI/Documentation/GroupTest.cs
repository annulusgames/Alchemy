using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class GroupTest : MonoBehaviour
    {
        [Group("Group1")]
        public float foo;

        [Group("Group1")]
        public Vector3 bar;

        [Group("Group1")]
        public GameObject baz;

        [Group("Group2")]
        public float alpha;

        [Group("Group2")]
        public Vector3 beta;

        [Group("Group2")]
        public GameObject gamma;
    }
}
