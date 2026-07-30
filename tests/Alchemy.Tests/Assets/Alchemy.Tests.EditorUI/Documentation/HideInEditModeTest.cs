using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class HideInEditModeTest : MonoBehaviour
    {
        [HideInEditMode]
        public float foo;
    }
}
