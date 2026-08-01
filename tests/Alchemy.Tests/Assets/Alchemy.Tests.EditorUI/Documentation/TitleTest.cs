using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class TitleTest : MonoBehaviour
    {
        [Title("Title1")]
        public float foo;
        public Vector3 bar;
        public GameObject baz;

        [Title("Title2", "Subtitle")]
        public float alpha;
        public Vector3 beta;
        public GameObject gamma;
    }
}
