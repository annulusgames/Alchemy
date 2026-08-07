using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample(Capture = false)]
    public class DisableInPlayModeTest : MonoBehaviour
    {
        #region document
        [DisableInPlayMode]
        public float foo;
        #endregion
    }
}
