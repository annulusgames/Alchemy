using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class BoxGroupTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [BoxGroup("Group1")]
        public float foo;

        [BoxGroup("Group1")]
        public Vector3 bar;

        [BoxGroup("Group1")]
        public GameObject baz;

        [BoxGroup("Group1/Group2")]
        public float alpha;

        [BoxGroup("Group1/Group2")]
        public Vector3 beta;

        [BoxGroup("Group1/Group2")]
        public GameObject gamma;
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
