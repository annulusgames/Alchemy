using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample(Capture = false)]
    public class DisableInEditModeTest : MonoBehaviour
    {
        #region document
        [DisableInEditMode]
        public float foo;
        #endregion
    }
}
