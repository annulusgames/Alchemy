using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample(Capture = false)]
    public class OnInspectorDisableTest : MonoBehaviour
    {
        #region document
        [OnInspectorDisable]
        void OnInspectorDisable()
        {
            Debug.Log("Disable");
        }
        #endregion
    }
}
