using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class ChildObjectsOnlyScriptable : ScriptableObject
    {
        [ChildObjectsOnly]
        public GameObject child;
    }
}
