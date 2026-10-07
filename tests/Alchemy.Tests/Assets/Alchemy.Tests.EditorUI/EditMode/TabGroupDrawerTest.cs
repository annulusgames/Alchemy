using System;
using System.Collections.Generic;
using System.Reflection;
using Alchemy.Editor.Drawers;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class TabGroupDrawerTest
    {
        string uniqueId;
        string configKey;
        string previousConfig;
        readonly List<EditorWindow> windows = new();

        [SetUp]
        public void SetUp()
        {
            uniqueId = "AlchemyTabGroupTest_" + Guid.NewGuid().ToString("N");
            configKey = uniqueId + "_TabGroup";
            previousConfig = EditorUserSettings.GetConfigValue(configKey);
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < windows.Count; i++)
                UnityEngine.Object.DestroyImmediate(windows[i]);
            windows.Clear();
            EditorUserSettings.SetConfigValue(configKey, previousConfig ?? string.Empty);
        }

        [Test]
        public void TabBar_UsesToolkitButtonsAndRestoresSavedTab()
        {
            EditorUserSettings.SetConfigValue(configKey, "1");
            var built = Build("One", "Two", "Three");

            Assert.That(built.root, Is.Not.InstanceOf<IMGUIContainer>());
            Assert.That(built.root.Q<IMGUIContainer>(), Is.Null);
            Assert.That(built.buttons.Count, Is.EqualTo(3));
            Assert.That(built.buttons[0].text, Is.EqualTo("One"));
            Assert.That(built.buttons[1].text, Is.EqualTo("Two"));
            Assert.That(built.buttons[2].text, Is.EqualTo("Three"));

#if UNITY_2023_2_OR_NEWER
            var bar = built.group;
#else
            var bar = built.buttons[0].parent;
#endif
            Assert.That(bar, Is.Not.Null);
            Assert.That(bar.parent, Is.SameAs(built.root));
            var toolbarHeight = EditorGUIUtility.singleLineHeight + 2.7f;
            Assert.That(bar.style.height.value.value, Is.EqualTo(toolbarHeight).Within(0.01f));
            Assert.That(bar.style.marginTop.value.value, Is.EqualTo(-3.7f).Within(0.01f));
            Assert.That(built.buttons[0].style.flexGrow.value, Is.EqualTo(1f).Within(0.01f));
            Assert.That(built.buttons[0].style.height.value.value, Is.EqualTo(toolbarHeight).Within(0.01f));

            AssertVisible(built.tabs[0], false);
            AssertVisible(built.tabs[1], true);
            AssertVisible(built.tabs[2], false);
#if UNITY_2023_2_OR_NEWER
            Assert.That(built.group.allowEmptySelection, Is.False);
            Assert.That(built.group.isMultipleSelection, Is.False);
            Assert.That(built.group.value[1], Is.True);
            Assert.That(built.group.value[0], Is.False);
            Assert.That(built.buttons[0].ClassListContains(ToggleButtonGroup.buttonLeftClassName), Is.True);
            Assert.That(built.buttons[1].ClassListContains(ToggleButtonGroup.buttonMidClassName), Is.True);
            Assert.That(built.buttons[2].ClassListContains(ToggleButtonGroup.buttonRightClassName), Is.True);
            Assert.That(built.buttons[1].style.backgroundColor.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(built.buttons[1].style.color.keyword, Is.EqualTo(StyleKeyword.Null));
#else
            Assert.That(built.buttons[1].style.unityFontStyleAndWeight.value, Is.EqualTo(FontStyle.Bold));
            Assert.That(built.buttons[0].style.unityFontStyleAndWeight.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(built.buttons[1].style.borderTopLeftRadius.value.value, Is.EqualTo(0f).Within(0.01f));
            Assert.That(built.buttons[0].style.borderTopLeftRadius.value.value, Is.EqualTo(3f).Within(0.01f));
            Assert.That(built.buttons[2].style.borderTopRightRadius.value.value, Is.EqualTo(3f).Within(0.01f));
#endif
        }

        [Test]
        public void SelectingAnotherTab_UpdatesContentAndPersistsIndex()
        {
            EditorUserSettings.SetConfigValue(configKey, "1");
            var built = Build("One", "Two", "Three");

            Click(built.buttons[1]);
            Assert.That(EditorUserSettings.GetConfigValue(configKey), Is.EqualTo("1"));
            AssertVisible(built.tabs[1], true);

            Click(built.buttons[2]);
            Assert.That(EditorUserSettings.GetConfigValue(configKey), Is.EqualTo("2"));
            AssertVisible(built.tabs[0], false);
            AssertVisible(built.tabs[1], false);
            AssertVisible(built.tabs[2], true);
#if UNITY_2023_2_OR_NEWER
            Assert.That(built.group.value[2], Is.True);
            Assert.That(built.group.value[1], Is.False);
            Assert.That(built.buttons[2].style.backgroundColor.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(built.buttons[2].style.color.keyword, Is.EqualTo(StyleKeyword.Null));
#else
            Assert.That(built.buttons[2].style.unityFontStyleAndWeight.value, Is.EqualTo(FontStyle.Bold));
            Assert.That(built.buttons[1].style.unityFontStyleAndWeight.keyword, Is.EqualTo(StyleKeyword.Null));
#endif

            var restored = Build("One", "Two", "Three");
            AssertVisible(restored.tabs[0], false);
            AssertVisible(restored.tabs[1], false);
            AssertVisible(restored.tabs[2], true);
        }

        [Test]
        public void RepeatedTabName_ReusesTheSameContentElement()
        {
            var drawer = new TabGroupDrawer();
            drawer.SetUniqueId(uniqueId);
            var root = drawer.CreateRootElement("Tabs");
            var first = drawer.GetGroupElement(new TabGroupAttribute("Tabs", "A"));
            var again = drawer.GetGroupElement(new TabGroupAttribute("Tabs", "A"));
            drawer.GetGroupElement(new TabGroupAttribute("Tabs", "B"));

            Assert.That(again, Is.SameAs(first));
            var buttons = root.Query<Button>().ToList();
            Assert.That(buttons.Count, Is.EqualTo(2));
            Assert.That(buttons[0].text, Is.EqualTo("A"));
            Assert.That(buttons[1].text, Is.EqualTo("B"));
        }

        [Test]
        public void OutOfRangeSavedIndex_ShowsTheFirstTabWithoutRewritingConfig()
        {
            EditorUserSettings.SetConfigValue(configKey, "9");
            var built = Build("One", "Two");

            Assert.That(EditorUserSettings.GetConfigValue(configKey), Is.EqualTo("9"));
            AssertVisible(built.tabs[0], true);
            AssertVisible(built.tabs[1], false);
#if UNITY_2023_2_OR_NEWER
            Assert.That(built.group.value[0], Is.True);
            Assert.That(built.buttons[0].style.backgroundColor.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(built.buttons[0].style.color.keyword, Is.EqualTo(StyleKeyword.Null));
#else
            Assert.That(built.buttons[0].style.unityFontStyleAndWeight.value, Is.EqualTo(FontStyle.Bold));
#endif

            Click(built.buttons[1]);
            Assert.That(EditorUserSettings.GetConfigValue(configKey), Is.EqualTo("1"));
            AssertVisible(built.tabs[0], false);
            AssertVisible(built.tabs[1], true);
        }

        [Test]
        public void MoreThan64Tabs_RestoresAndSelectsTabsBeyondTheSegmentedControlLimit()
        {
            EditorUserSettings.SetConfigValue(configKey, "64");
            var names = new string[66];
            for (var i = 0; i < names.Length; i++) names[i] = "Tab " + i;
            var built = Build(names);

            Assert.That(built.buttons.Count, Is.EqualTo(66));
            AssertVisible(built.tabs[64], true);
            AssertVisible(built.tabs[0], false);
            Click(built.buttons[65]);
            Assert.That(EditorUserSettings.GetConfigValue(configKey), Is.EqualTo("65"));
            AssertVisible(built.tabs[64], false);
            AssertVisible(built.tabs[65], true);
            Click(built.buttons[0]);
            AssertVisible(built.tabs[0], true);
            AssertVisible(built.tabs[65], false);
        }

        BuiltTabs Build(params string[] names)
        {
            var drawer = new TabGroupDrawer();
            drawer.SetUniqueId(uniqueId);
            var root = drawer.CreateRootElement("Tabs");
            var tabs = new VisualElement[names.Length];
            for (var i = 0; i < names.Length; i++)
                tabs[i] = drawer.GetGroupElement(new TabGroupAttribute("Tabs", names[i]));

            windows.Add(EditModeEditorTestUtility.ShowInWindow(root));
            return new BuiltTabs
            {
                root = root,
#if UNITY_2023_2_OR_NEWER
                group = root.Q<ToggleButtonGroup>(),
#endif
                buttons = root.Query<Button>().ToList(),
                tabs = tabs,
            };
        }

        static void AssertVisible(VisualElement element, bool visible)
        {
            Assert.That(
                element.style.display.value,
                Is.EqualTo(visible ? DisplayStyle.Flex : DisplayStyle.None));
        }

        static void Click(Button button)
        {
#if UNITY_2023_2_OR_NEWER
            var group = button.GetFirstAncestorOfType<ToggleButtonGroup>();
            if (group != null)
            {
                Assert.That(group.panel, Is.Not.Null);
                var index = group.IndexOf(button);
                Assert.That(index, Is.GreaterThanOrEqualTo(0));
                group.value = new ToggleButtonGroupState(1UL << index, group.value.length);
                return;
            }
#endif
            var field = typeof(Clickable).GetField("clicked", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            var action = (Action)field.GetValue(button.clickable);
            Assert.That(action, Is.Not.Null);
            action.Invoke();
        }

        sealed class BuiltTabs
        {
            public VisualElement root;
#if UNITY_2023_2_OR_NEWER
            public ToggleButtonGroup group;
#endif
            public List<Button> buttons;
            public VisualElement[] tabs;
        }
    }
}
