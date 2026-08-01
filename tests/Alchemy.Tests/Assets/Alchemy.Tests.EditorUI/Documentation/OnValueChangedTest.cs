using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class OnValueChangedTest : MonoBehaviour
    {
        [OnValueChanged("OnValueChanged")]
        public int foo;

        void OnValueChanged(int value)
        {
            Debug.Log(value);
        }
    }
}
