using System;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    public class ChildObjectsOnlyHost : MonoBehaviour
    {
        [ChildObjectsOnly]
        public GameObject child;

        [ChildObjectsOnly("Must be a child object.")]
        public Transform childTransform;

        [ChildObjectsOnly(IncludeSelf = false)]
        public GameObject descendantOnly;

        [ChildObjectsOnly]
        public GameObject[] children;

        [ChildObjectsOnly]
        public int unsupported;

        [ChildObjectsOnly]
        public Texture2D texture;

        [ChildObjectsOnly]
        public UnityEngine.Object anyObject;

        public Nested nested;

        [Serializable]
        public class Nested
        {
            [ChildObjectsOnly]
            public GameObject child;
        }
    }
}
