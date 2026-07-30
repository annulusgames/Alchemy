using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class RequiredTest : MonoBehaviour
    {
        [Required]
        public GameObject requiredField1;

        [Required("Custom message")]
        public Material requiredField2;
    }
}
