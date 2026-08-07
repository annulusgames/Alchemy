using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class ListViewSettingsTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [ListViewSettings(
            ShowAlternatingRowBackgrounds = AlternatingRowBackground.All,
            ShowFoldoutHeader = false)]
        public int[] array1;

        [ListViewSettings(
            Reorderable = false,
            ShowAddRemoveFooter = false,
            ShowBorder = false,
            ShowBoundCollectionSize = false)]
        public Vector3[] array2 = new Vector3[]
        {
            Vector3.zero,
            Vector3.one
        };
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
