using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class ShowIfTest : MonoBehaviour
    {
        public bool show;

        public bool Show => show;
        public bool IsShowTrue() => show;

        [ShowIf("show")]
        public int showIfField;

        [ShowIf("Show")]
        public int showIfProperty;

        [ShowIf("IsShowTrue")]
        public int showIfMethod;
    }
}
