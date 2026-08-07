using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class DisableIfTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        public bool isDisabled;

        public bool IsDisabled => isDisabled;
        public bool IsDisabledMethod() => isDisabled;

        [DisableIf("isDisabled")]
        public int disableIfField;

        [DisableIf("IsDisabled")]
        public int disableIfProperty;

        [DisableIf("IsDisabledMethod")]
        public int disableIfMethod;
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
