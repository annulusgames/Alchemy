using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample(Capture = false)]
    public class PreviewTest : MonoBehaviour
    {
        #region document
        [Preview(64, Align.FlexStart)]
        public Sprite foo;

        [Preview(64, Align.Center)]
        public Texture bar;

        [Preview]
        public Material baz;
        #endregion
    }
}
