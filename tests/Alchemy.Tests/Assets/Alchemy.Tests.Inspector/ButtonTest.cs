using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.Inspector
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
