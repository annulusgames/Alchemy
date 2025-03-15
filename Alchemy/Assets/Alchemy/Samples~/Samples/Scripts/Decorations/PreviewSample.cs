using UnityEngine;
using Alchemy.Inspector;
using UnityEngine.UIElements;

namespace Alchemy.Samples
{
    public class PreviewSample : MonoBehaviour
    {
        [Preview(64, Align.FlexStart)] public Sprite foo;
        [Preview(64, Align.Center)] public Texture bar;
        [Preview] public Material baz;
        [Preview] public GameObject qux;
    }
}
