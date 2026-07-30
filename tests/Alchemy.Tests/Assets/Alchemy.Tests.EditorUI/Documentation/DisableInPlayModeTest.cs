using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class DisableInPlayModeTest : MonoBehaviour
    {
        [DisableInPlayMode]
        public float foo;
    }
}
