using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class GroupAttributesTest : MonoBehaviour
    {
        [FoldoutGroup("Foldout")]
        public int a;

        [FoldoutGroup("Foldout")]
        public int b;

        [FoldoutGroup("Foldout")]
        public int c;

        [TabGroup("Tab", "Tab1")]
        public int x;

        [TabGroup("Tab", "Tab2")]
        public string y;

        [TabGroup("Tab", "Tab3")]
        public Vector3 z;
    }
}
