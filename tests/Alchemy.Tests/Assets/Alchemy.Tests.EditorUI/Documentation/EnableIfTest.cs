using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class EnableIfTest : MonoBehaviour
    {
        public bool isEnabled;

        public bool IsEnabled => isEnabled;
        public bool IsEnabledMethod() => isEnabled;

        [EnableIf("isEnabled")]
        public int enableIfField;

        [EnableIf("IsEnabled")]
        public int enableIfProperty;

        [EnableIf("IsEnabledMethod")]
        public int enableIfMethod;
    }
}
