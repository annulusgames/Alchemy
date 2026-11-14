using System;
using System.Collections;
using System.Reflection;
using Alchemy.Editor;
using Alchemy.Editor.Elements;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    /// <summary>
    /// Verifies that the saved open/close state of a [Button] foldout and a [FoldoutGroup]
    /// only follows the foldout itself, not the bool controls placed inside it.
    /// </summary>
    public class FoldoutStatePersistenceTest
    {
        [Serializable]
        class ButtonOptions
        {
            public int count;
        }

        class ButtonTarget
        {
            [Button]
            public void Run(ButtonOptions options) { }
        }

        class GroupTarget { }

        const string GroupName = "Group";

        static readonly MethodInfo RunMethod = typeof(ButtonTarget).GetMethod(nameof(ButtonTarget.Run));

        // Only used to reset the persisted state between tests; assertions go through a rebuilt element instead.
        static readonly string ButtonConfigKey =
            $"{typeof(ButtonTarget).FullName}_{nameof(ButtonTarget.Run)}_{nameof(ButtonOptions)}_MethodButton";
        static readonly string GroupConfigKey =
            $"AlchemyGroupId_{typeof(GroupTarget).FullName}_{GroupName}_FoldoutGroup";

        EditorWindow window;

        [SetUp]
        public void SetUp()
        {
            ClearSavedState();
            // Fails loudly if the keys above drift from the production keys and stale state leaks between runs.
            Assert.That(CreateButtonFoldout().value, Is.False);
            Assert.That(CreateGroupFoldout().value, Is.False);
        }

        [TearDown]
        public void TearDown()
        {
            if (window != null) UnityEngine.Object.DestroyImmediate(window);
            ClearSavedState();
        }

        /// <summary>
        /// Opening and closing a nested foldout of a class-typed parameter must not collapse the saved [Button] foldout.
        /// </summary>
        [UnityTest]
        public IEnumerator MethodButton_NestedFoldoutChange_KeepsSavedFoldoutState()
        {
            Foldout foldout = ShowButtonFoldout();
            yield return null;
            foldout.value = true;
            yield return null;

            Foldout nestedFoldout = foldout.contentContainer.Q<Foldout>();
            Assert.That(nestedFoldout, Is.Not.Null);
            nestedFoldout.value = true;
            yield return null;
            nestedFoldout.value = false;
            yield return null;

            Assert.That(CreateButtonFoldout().value, Is.True);
        }

        /// <summary>
        /// Changing a bool control inside a [FoldoutGroup] must not be stored as the open/close state of the group.
        /// </summary>
        [UnityTest]
        public IEnumerator FoldoutGroup_ChildBoolChange_KeepsSavedFoldoutState()
        {
            Foldout foldout = CreateGroupFoldout();
            Toggle childToggle = new Toggle();
            foldout.Add(childToggle);
            window = EditModeEditorTestUtility.ShowInWindow(foldout);
            yield return null;
            foldout.value = true;
            yield return null;

            childToggle.value = true;
            yield return null;
            childToggle.value = false;
            yield return null;

            Assert.That(CreateGroupFoldout().value, Is.True);
        }

        Foldout ShowButtonFoldout()
        {
            MethodButton button = new MethodButton(new ButtonTarget(), RunMethod);
            window = EditModeEditorTestUtility.ShowInWindow(button);
            return button.Q<Foldout>();
        }

        // A freshly built element restores its state from the saved value, like re-selecting the object in the Inspector.
        static Foldout CreateButtonFoldout()
        {
            MethodButton button = new MethodButton(new ButtonTarget(), RunMethod);
            return button.Q<Foldout>();
        }

        static Foldout CreateGroupFoldout()
        {
            AlchemyGroupDrawer drawer = AlchemyEditorUtility.CreateGroupDrawer(new FoldoutGroupAttribute(GroupName), typeof(GroupTarget));
            return (Foldout)drawer.CreateRootElement(GroupName);
        }

        static void ClearSavedState()
        {
            EditorUserSettings.SetConfigValue(ButtonConfigKey, null);
            EditorUserSettings.SetConfigValue(GroupConfigKey, null);
        }
    }
}
