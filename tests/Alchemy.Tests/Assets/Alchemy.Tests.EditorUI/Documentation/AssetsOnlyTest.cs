using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class AssetsOnlyTest : MonoBehaviour
    {
        [AssetsOnly]
        public Object asset1;

        [AssetsOnly]
        public GameObject asset2;
    }
}
