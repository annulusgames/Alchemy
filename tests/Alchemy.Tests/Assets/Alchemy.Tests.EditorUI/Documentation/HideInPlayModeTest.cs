using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample(Capture = false)]
    public class HideInPlayModeTest : MonoBehaviour
    {
        #region document
        [HideInPlayMode]
        public float foo;
        #endregion
    }
}
