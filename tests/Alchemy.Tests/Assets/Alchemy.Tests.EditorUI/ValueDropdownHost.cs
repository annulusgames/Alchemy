using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    public class ValueDropdownHost : MonoBehaviour
    {
        [ValueDropdown(nameof(Numbers))] public List<int> numbers = new List<int> { 1, 2, 3 };
        [LabelWidth(200f), ValueDropdown(nameof(Numbers), Mode = ValueDropdownMode.Append)] public int appended = 1;

        static int[] Numbers => new[] { 1, 2, 3, 4 };
    }
}
