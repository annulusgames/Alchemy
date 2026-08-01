using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class InlineEditorTest : MonoBehaviour
    {
        [InlineEditor]
        public DocumentationSampleScriptableObject sample;
    }
}
