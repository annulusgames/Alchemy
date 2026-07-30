using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class HideLabelTest : MonoBehaviour
    {
        [HideLabel]
        public float foo;

        [HideLabel]
        public Vector3 bar;

        [HideLabel]
        public GameObject baz;
    }
}
