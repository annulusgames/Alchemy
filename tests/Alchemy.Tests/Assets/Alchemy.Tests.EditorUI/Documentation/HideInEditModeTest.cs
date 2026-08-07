using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample(Capture = false)]
    public class HideInEditModeTest : MonoBehaviour
    {
        #region document
        [HideInEditMode]
        public float foo;
        #endregion
    }
}
