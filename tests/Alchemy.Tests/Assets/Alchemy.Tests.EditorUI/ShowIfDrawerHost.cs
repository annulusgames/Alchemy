using System;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    public class ShowIfDrawerHost : MonoBehaviour
    {
        public bool show;

        [ShowIf(nameof(show))]
        public int value;

        public Nested nested = new Nested();

        [Serializable]
        public class Nested
        {
            public bool show;

            [ShowIf(nameof(show))]
            public int value;
        }
    }
}
