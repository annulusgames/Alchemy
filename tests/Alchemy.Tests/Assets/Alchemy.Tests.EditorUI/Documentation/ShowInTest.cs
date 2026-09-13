using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class ShowInTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [ShowIn(PrefabKind.None)]
        [SerializeField] GameObject none;

        [ShowIn(PrefabKind.InstanceInPrefab)]
        [SerializeField] GameObject instanceInPrefab;

        [ShowIn(PrefabKind.InstanceInScene)]
        [SerializeField] GameObject instanceInScene;

        [ShowIn(PrefabKind.Regular)]
        [SerializeField] GameObject regular;

        [ShowIn(PrefabKind.Variant)]
        [SerializeField] GameObject variant;

        [ShowIn(PrefabKind.NonPrefabInstance)]
        [SerializeField] GameObject nonPrefabInstance;

        [ShowIn(PrefabKind.PrefabInstance)]
        [SerializeField] GameObject prefabInstance;

        [ShowIn(PrefabKind.PrefabAsset)]
        [SerializeField] GameObject prefabAsset;

        [ShowIn(PrefabKind.PrefabInstanceAndNonPrefabInstance)]
        [SerializeField] GameObject prefabInstanceAndNonPrefabInstance;

        [ShowIn(PrefabKind.All)]
        [SerializeField] GameObject all;

        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
