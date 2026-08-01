using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class DisableIfTest : MonoBehaviour
    {
        public bool isDisabled;

        public bool IsDisabled => isDisabled;
        public bool IsDisabledMethod() => isDisabled;

        [DisableIf("isDisabled")]
        public int disableIfField;

        [DisableIf("IsDisabled")]
        public int disableIfProperty;

        [DisableIf("IsDisabledMethod")]
        public int disableIfMethod;
    }
}
