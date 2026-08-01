using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class AttributesTest : MonoBehaviour
    {
        [LabelText("Custom Label")]
        public float foo;

        [HideLabel]
        public Vector3 bar;

        [AssetsOnly]
        public GameObject baz;

        [Title("Title")]
        [HelpBox("HelpBox", HelpBoxMessageType.Info)]
        [ReadOnly]
        public string message = "Read Only";
    }
}
