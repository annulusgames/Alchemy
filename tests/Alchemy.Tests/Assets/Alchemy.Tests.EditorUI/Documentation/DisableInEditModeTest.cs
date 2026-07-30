using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class DisableInEditModeTest : MonoBehaviour
    {
        [DisableInEditMode]
        public float foo;
    }
}
