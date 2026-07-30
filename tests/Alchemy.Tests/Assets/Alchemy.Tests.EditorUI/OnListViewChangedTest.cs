using System.Collections.Generic;
using UnityEngine;
using Alchemy.Inspector;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class OnListViewChangedTest : MonoBehaviour
    {
        [OnListViewChanged(
            OnItemChanged = nameof(OnItemChanged),
            OnItemsAdded = nameof(OnItemsAdded),
            OnItemsRemoved = nameof(OnItemsRemoved),
            OnItemsChosen = nameof(OnItemChosen),
            OnItemsSourceChanged = nameof(OnItemsSourceChanged),
            OnSelectionChanged = nameof(OnSelectionChanged),
            OnSelectedIndicesChanged = nameof(OnSelectedIndicesChanged),
            OnItemIndexChanged = nameof(OnItemIndexChanged))
        ]
        public int[] array;

        void OnItemChanged(int index, int item)
        {
            Debug.Log($"Changed: [{index}] -> {item}");
        }

        void OnItemsAdded(IEnumerable<int> indices)
        {
            Debug.Log($"Added: [{string.Join(',', indices)}]");
        }

        void OnItemsRemoved(IEnumerable<int> indices)
        {
            Debug.Log($"Removed: [{string.Join(',', indices)}]");
        }

        void OnItemsSourceChanged()
        {
            Debug.Log("Items Source Changed");
        }

        void OnItemChosen(IEnumerable<object> items)
        {
            Debug.Log($"Chosen: [{string.Join(',', items)}]");
        }

        void OnSelectionChanged(IEnumerable<object> items)
        {
            Debug.Log($"Selection Changed: [{string.Join(',', items)}]");
        }

        void OnSelectedIndicesChanged(IEnumerable<int> indices)
        {
            Debug.Log($"Selected: [{string.Join(',', indices)}]");
        }

        void OnItemIndexChanged(int before, int after)
        {
            Debug.Log($"Index Changed: [{before} -> {after}]");
        }
    }
}
