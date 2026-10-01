using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    public class OnValueChangedDrawerHost : MonoBehaviour
    {
        public readonly List<int> calls = new List<int>();

        [OnValueChanged(nameof(Changed))]
        public int value;

        void Changed(int newValue) => calls.Add(newValue);
    }
}
