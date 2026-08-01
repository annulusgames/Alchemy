using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class OnInspectorDestroyTest : MonoBehaviour
    {
        [OnInspectorDestroy]
        void OnInspectorDestroy()
        {
            Debug.Log("Destroy");
        }
    }
}
