using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class HideIfTest : MonoBehaviour
    {
        public bool hide;

        public bool Hide => hide;
        public bool IsHideTrue() => hide;

        [HideIf("hide")]
        public int hideIfField;

        [HideIf("Hide")]
        public int hideIfProperty;

        [HideIf("IsHideTrue")]
        public int hideIfMethod;
    }
}
