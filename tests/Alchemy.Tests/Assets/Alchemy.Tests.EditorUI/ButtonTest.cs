using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    public class ButtonTest : MonoBehaviour
    {
        [Button]
        public void Foo() { }

        [Button]
        [LabelText("Bar!!!")]
        public void Bar() { }
    }
}
