using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class EnableIfTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        public bool isEnabled;

        public bool IsEnabled => isEnabled;
        public bool IsEnabledMethod() => isEnabled;

        [EnableIf("isEnabled")]
        public int enableIfField;

        [EnableIf("IsEnabled")]
        public int enableIfProperty;

        [EnableIf("IsEnabledMethod")]
        public int enableIfMethod;
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
