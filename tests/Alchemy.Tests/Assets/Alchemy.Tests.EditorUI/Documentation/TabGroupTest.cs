using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class TabGroupTest : MonoBehaviour
    {
        [TabGroup("Group1", "Tab1")]
        public float foo;

        [TabGroup("Group1", "Tab2")]
        public Vector3 bar;

        [TabGroup("Group1", "Tab3")]
        public GameObject baz;

        [TabGroup("Group1", "Tab1")]
        public float alpha;

        [TabGroup("Group1", "Tab2")]
        public Vector3 beta;

        [TabGroup("Group1", "Tab3")]
        public GameObject gamma;
    }
}
