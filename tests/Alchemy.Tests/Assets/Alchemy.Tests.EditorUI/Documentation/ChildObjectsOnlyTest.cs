using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class ChildObjectsOnlyTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [ChildObjectsOnly]
        public GameObject child;

        [ChildObjectsOnly("Must be a child object.")]
        public Transform childTransform;

        [ChildObjectsOnly(IncludeSelf = false)]
        public GameObject descendantOnly;
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
