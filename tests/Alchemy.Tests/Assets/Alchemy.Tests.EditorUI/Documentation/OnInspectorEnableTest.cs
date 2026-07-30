using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class OnInspectorEnableTest : MonoBehaviour
    {
        [OnInspectorEnable]
        void OnInspectorEnable()
        {
            Debug.Log("Enable");
        }
    }
}
