using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class EnableInTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [EnableIn(PrefabKind.None)]
        [SerializeField] GameObject none;

        [EnableIn(PrefabKind.InstanceInPrefab)]
        [SerializeField] GameObject instanceInPrefab;

        [EnableIn(PrefabKind.InstanceInScene)]
        [SerializeField] GameObject instanceInScene;

        [EnableIn(PrefabKind.Regular)]
        [SerializeField] GameObject regular;

        [EnableIn(PrefabKind.Variant)]
        [SerializeField] GameObject variant;

        [EnableIn(PrefabKind.NonPrefabInstance)]
        [SerializeField] GameObject nonPrefabInstance;

        [EnableIn(PrefabKind.PrefabInstance)]
        [SerializeField] GameObject prefabInstance;

        [EnableIn(PrefabKind.PrefabAsset)]
        [SerializeField] GameObject prefabAsset;

        [EnableIn(PrefabKind.PrefabInstanceAndNonPrefabInstance)]
        [SerializeField] GameObject prefabInstanceAndNonPrefabInstance;

        [EnableIn(PrefabKind.All)]
        [SerializeField] GameObject all;

        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
