using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class InlineGroupTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [InlineGroup("Group1")]
        public float foo;

        [InlineGroup("Group1")]
        public Vector3 bar;

        [InlineGroup("Group1")]
        public GameObject baz;
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
