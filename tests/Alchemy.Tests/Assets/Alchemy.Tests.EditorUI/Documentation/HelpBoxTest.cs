using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class HelpBoxTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [HelpBox("Custom Info")]
        public float foo;

        [HelpBox("Custom Warning", HelpBoxMessageType.Warning)]
        public Vector2 bar;

        [HelpBox("Custom Error", HelpBoxMessageType.Error)]
        public GameObject baz;
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
