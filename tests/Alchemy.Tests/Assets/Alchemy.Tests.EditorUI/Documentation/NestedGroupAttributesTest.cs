using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class NestedGroupAttributesTest : MonoBehaviour
    {
        [HorizontalGroup("Horizontal"), BoxGroup("Horizontal/Box1")]
        public float foo;

        [HorizontalGroup("Horizontal"), BoxGroup("Horizontal/Box1")]
        public Vector3 bar;

        [HorizontalGroup("Horizontal"), BoxGroup("Horizontal/Box1")]
        public GameObject baz;

        [HorizontalGroup("Horizontal"), BoxGroup("Horizontal/Box2")]
        public float alpha;

        [HorizontalGroup("Horizontal"), BoxGroup("Horizontal/Box2")]
        public Vector3 beta;

        [HorizontalGroup("Horizontal"), BoxGroup("Horizontal/Box2")]
        public GameObject gamma;
    }
}
