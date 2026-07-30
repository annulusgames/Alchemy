using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class LabelTextTest : MonoBehaviour
    {
        [LabelText("FOO!")]
        public float foo;

        [LabelText("BAR!")]
        public Vector3 bar;

        [LabelText("BAZ!")]
        public GameObject baz;

        [Space]
        [LabelText("α")]
        public float alpha;

        [LabelText("β")]
        public Vector3 beta;

        [LabelText("γ")]
        public GameObject gamma;
    }
}
