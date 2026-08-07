using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class ValidateInputTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [ValidateInput("IsNotNull")]
        public GameObject obj;

        [ValidateInput("IsZeroOrGreater", "foo must be 0 or greater.")]
        public int foo = -1;

        static bool IsNotNull(GameObject go)
        {
            return go != null;
        }

        static bool IsZeroOrGreater(int a)
        {
            return a >= 0;
        }
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
