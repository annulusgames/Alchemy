using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class HideInTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [HideIn(PrefabKind.None)]
        [SerializeField] GameObject none;

        [HideIn(PrefabKind.InstanceInPrefab)]
        [SerializeField] GameObject instanceInPrefab;

        [HideIn(PrefabKind.InstanceInScene)]
        [SerializeField] GameObject instanceInScene;

        [HideIn(PrefabKind.Regular)]
        [SerializeField] GameObject regular;

        [HideIn(PrefabKind.Variant)]
        [SerializeField] GameObject variant;

        [HideIn(PrefabKind.NonPrefabInstance)]
        [SerializeField] GameObject nonPrefabInstance;

        [HideIn(PrefabKind.PrefabInstance)]
        [SerializeField] GameObject prefabInstance;

        [HideIn(PrefabKind.PrefabAsset)]
        [SerializeField] GameObject prefabAsset;

        [HideIn(PrefabKind.PrefabInstanceAndNonPrefabInstance)]
        [SerializeField] GameObject prefabInstanceAndNonPrefabInstance;

        [HideIn(PrefabKind.All)]
        [SerializeField] GameObject all;

        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
