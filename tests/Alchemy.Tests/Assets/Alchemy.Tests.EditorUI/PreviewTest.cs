using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI
{
    public class PreviewTest : MonoBehaviour
    {
        [Preview(64, Align.FlexStart)] public Sprite foo;
        [Preview(64, Align.Center)] public Texture bar;
        [Preview] public Material baz;
        [Preview] public GameObject qux;
    }
}
