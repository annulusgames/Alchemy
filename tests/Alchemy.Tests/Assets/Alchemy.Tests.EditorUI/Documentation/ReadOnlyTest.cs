using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class ReadOnlyTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [ReadOnly]
        public float field = 2.5f;

        [ReadOnly]
        public int[] array = new int[5];

        [ReadOnly]
        public DocumentationSampleClass classField;

        [ReadOnly]
        public DocumentationSampleClass[] classArray = new DocumentationSampleClass[3];
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
