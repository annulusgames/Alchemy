using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample(Capture = false)]
    public class OnInspectorEnableTest : MonoBehaviour
    {
        #region document
        [OnInspectorEnable]
        void OnInspectorEnable()
        {
            Debug.Log("Enable");
        }
        #endregion
    }
}
