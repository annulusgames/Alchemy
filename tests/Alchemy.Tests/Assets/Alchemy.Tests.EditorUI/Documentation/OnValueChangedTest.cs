using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample(Capture = false)]
    public class OnValueChangedTest : MonoBehaviour
    {
        #region document
        [OnValueChanged("OnValueChanged")]
        public int foo;

        void OnValueChanged(int value)
        {
            Debug.Log(value);
        }
        #endregion
    }
}
