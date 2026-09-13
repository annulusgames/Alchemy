using System;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    public class PrefabConditionalHost : MonoBehaviour
    {
        [ShowIn(PrefabKind.NonPrefabInstance)] public int sceneOnly;
        [HideIn(PrefabKind.NonPrefabInstance)] public int hiddenInScene;
        [EnableIn(PrefabKind.NonPrefabInstance)] public int editableInScene;
        [DisableIn(PrefabKind.NonPrefabInstance)] public int disabledInScene;
        [ShowIn(PrefabKind.All), HideIn(PrefabKind.NonPrefabInstance)] public int hiddenWins;
        [EnableIn(PrefabKind.All), DisableIn(PrefabKind.NonPrefabInstance)] public int disabledWins;
        [EnableIn(PrefabKind.All), ReadOnly] public int readOnly;
        [ShowIn(PrefabKind.Regular), ShowIf(nameof(condition))] public int showIfCannotReveal;
        [DisableIn(PrefabKind.NonPrefabInstance), EnableIf(nameof(condition))] public int enableIfCannotEnable;
        [ShowIn(PrefabKind.All), HideIf(nameof(condition))] public int hideIfStillApplies;
        [EnableIn(PrefabKind.All), DisableIf(nameof(condition))] public int disableIfStillApplies;
        public bool condition = true;
        [ShowIn(PrefabKind.Regular), HelpBox("Prefab decoration")] public int decorated;
        [ShowInInspector, ShowIn(PrefabKind.NonPrefabInstance)] public int ReflectedProperty { get; set; }
        [Button, DisableIn(PrefabKind.NonPrefabInstance)] public void Action() { }
        [ShowInInspector] public Nested ReflectedNested { get; set; } = new Nested();
        public Nested serializedNested = new Nested();

        [Serializable]
        public class Nested
        {
            [ShowIn(PrefabKind.NonPrefabInstance)] public int nestedSceneOnly;
            [EnableIn(PrefabKind.NonPrefabInstance)] public int nestedEditableInScene;
        }
    }
}
