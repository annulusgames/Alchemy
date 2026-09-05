using UnityEngine;
using Alchemy.Inspector;

namespace Alchemy.Samples
{
    public class ChildObjectsOnlySample : MonoBehaviour
    {
        [ChildObjectsOnly]
        public GameObject child;

        [ChildObjectsOnly("Must be a child object.")]
        public Transform childTransform;

        [ChildObjectsOnly(IncludeSelf = false)]
        public GameObject descendantOnly;
    }
}
