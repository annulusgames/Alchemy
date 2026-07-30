using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class HelpBoxTest : MonoBehaviour
    {
        [HelpBox("Custom Info")]
        public float foo;

        [HelpBox("Custom Warning", HelpBoxMessageType.Warning)]
        public Vector2 bar;

        [HelpBox("Custom Error", HelpBoxMessageType.Error)]
        public GameObject baz;
    }
}
