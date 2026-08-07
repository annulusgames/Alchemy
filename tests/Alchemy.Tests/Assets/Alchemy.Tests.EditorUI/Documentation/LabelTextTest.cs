using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class LabelTextTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [LabelText("FOO!")]
        public float foo;

        [LabelText("BAR!")]
        public Vector3 bar;

        [LabelText("BAZ!")]
        public GameObject baz;

        [Space]
        [LabelText("α")]
        public float alpha;

        [LabelText("β")]
        public Vector3 beta;

        [LabelText("γ")]
        public GameObject gamma;
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
