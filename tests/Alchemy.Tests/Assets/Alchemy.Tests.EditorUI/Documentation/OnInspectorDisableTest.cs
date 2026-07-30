using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class OnInspectorDisableTest : MonoBehaviour
    {
        [OnInspectorDisable]
        void OnInspectorDisable()
        {
            Debug.Log("Disable");
        }
    }
}
