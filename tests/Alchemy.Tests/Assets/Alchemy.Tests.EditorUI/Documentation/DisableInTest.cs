using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class DisableInTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [DisableIn(PrefabKind.None)]
        [SerializeField] GameObject none;

        [DisableIn(PrefabKind.InstanceInPrefab)]
        [SerializeField] GameObject instanceInPrefab;

        [DisableIn(PrefabKind.InstanceInScene)]
        [SerializeField] GameObject instanceInScene;

        [DisableIn(PrefabKind.Regular)]
        [SerializeField] GameObject regular;

        [DisableIn(PrefabKind.Variant)]
        [SerializeField] GameObject variant;

        [DisableIn(PrefabKind.NonPrefabInstance)]
        [SerializeField] GameObject nonPrefabInstance;

        [DisableIn(PrefabKind.PrefabInstance)]
        [SerializeField] GameObject prefabInstance;

        [DisableIn(PrefabKind.PrefabAsset)]
        [SerializeField] GameObject prefabAsset;

        [DisableIn(PrefabKind.PrefabInstanceAndNonPrefabInstance)]
        [SerializeField] GameObject prefabInstanceAndNonPrefabInstance;

        [DisableIn(PrefabKind.All)]
        [SerializeField] GameObject all;

        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
