using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class FoldoutGroupTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [FoldoutGroup("Group1")]
        public float foo;

        [FoldoutGroup("Group1")]
        public Vector3 bar;

        [FoldoutGroup("Group1")]
        public GameObject baz;

        [FoldoutGroup("Group2")]
        public float alpha;

        [FoldoutGroup("Group2")]
        public Vector3 beta;

        [FoldoutGroup("Group2")]
        public GameObject gamma;
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
