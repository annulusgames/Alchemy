using System;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class ShowInInspectorTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [NonSerialized, ShowInInspector]
        public int field;

        [NonSerialized, ShowInInspector]
        public DocumentationSampleClass classField = new();

        [ShowInInspector]
        public int Getter => 10;

        [field: NonSerialized, ShowInInspector]
        public string Property { get; set; } = string.Empty;
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
