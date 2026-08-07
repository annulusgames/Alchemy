using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample(Capture = false)]
    public class OnInspectorDestroyTest : MonoBehaviour
    {
        #region document
        [OnInspectorDestroy]
        void OnInspectorDestroy()
        {
            Debug.Log("Destroy");
        }
        #endregion
    }
}
