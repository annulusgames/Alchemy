using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class ReadOnlyTest : MonoBehaviour
    {
        [ReadOnly]
        public float field = 2.5f;

        [ReadOnly]
        public int[] array = new int[5];

        [ReadOnly]
        public DocumentationSampleClass classField;

        [ReadOnly]
        public DocumentationSampleClass[] classArray = new DocumentationSampleClass[3];
    }
}
