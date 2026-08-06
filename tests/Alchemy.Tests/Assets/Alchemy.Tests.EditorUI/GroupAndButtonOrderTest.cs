using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    // Visual acceptance fixture for unified member/group/button order.
    // Expected Inspector sibling order (Order, then declaration order; missing Order => 0).
    // Reflection cannot interleave member kinds: fields before methods within the same Order.
    //   Foldout Group A1 > fieldA1 > Tab Group A1 > Foldout Group A2 > ButtonA1 > ButtonA2 > ButtonA3
    //   > fieldB1 > Foldout Group B1 > ButtonB1 > ButtonB2 > Foldout Group B2 > ButtonB3 > Tab Group B1
    public class GroupAndButtonOrderTest : MonoBehaviour
    {
        // Ordered fields > Buttons (methods)

        [SerializeField, FoldoutGroup("Foldout Group A1")]
        int foldoutGroupA1;

        [Button]
        void ButtonA1() { }

        [SerializeField] int fieldA1;

        [Button]
        void ButtonA2() { }

        [SerializeField, TabGroup("Tab Group A1", "Tab Group A1")]
        int tabGroupA1;

        [SerializeField, FoldoutGroup("Foldout Group A2")]
        int foldoutGroupA2;

        [Button]
        void ButtonA3() { }

        // fieldB1 > Foldout Group B1 > ButtonB1 > ButtonB2 > Foldout Group B2 > ButtonB3 > Tab Group B1

        [Button, Order(3)]
        void ButtonB3() { }

        [Button, Order(2)]
        void ButtonB2() { }

        [HorizontalLine] [SerializeField, Order(1)]
        int fieldB1;

        [Button, Order(1)]
        void ButtonB1() { }

        [SerializeField, TabGroup("Tab Group B1", "Tab Group B1", order: 4)]
        int tabGroupB1;

        [SerializeField, FoldoutGroup("Foldout Group B2", order: 3)]
        int foldoutGroupB2;

        [SerializeField, FoldoutGroup("Foldout Group B1", order: 1)]
        int foldoutGroupB1;
    }
}
