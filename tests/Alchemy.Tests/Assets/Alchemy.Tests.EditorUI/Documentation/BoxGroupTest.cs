using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class BoxGroupTest : MonoBehaviour
    {
        [BoxGroup("Group1")]
        public float foo;

        [BoxGroup("Group1")]
        public Vector3 bar;

        [BoxGroup("Group1")]
        public GameObject baz;

        [BoxGroup("Group1/Group2")]
        public float alpha;

        [BoxGroup("Group1/Group2")]
        public Vector3 beta;

        [BoxGroup("Group1/Group2")]
        public GameObject gamma;
    }
}
