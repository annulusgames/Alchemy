using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class HideInPlayModeTest : MonoBehaviour
    {
        [HideInPlayMode]
        public float foo;
    }
}
