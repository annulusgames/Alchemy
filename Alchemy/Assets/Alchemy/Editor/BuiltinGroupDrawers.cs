using System;
using System.Collections.Generic;
using Alchemy.Editor.Elements;
using Alchemy.Inspector;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Editor.Drawers
{
    [CustomGroupDrawer(typeof(GroupAttribute))]
    public sealed class GroupDrawer : AlchemyGroupDrawer
    {
        public override VisualElement CreateRootElement(string label)
        {
            return new Box()
            {
                style = {
                    width = Length.Percent(100f),
                    marginTop = 3f,
                    paddingBottom = 2f,
                    paddingRight = 1f,
                    paddingLeft = 1f,
                }
            };
        }
    }

    [CustomGroupDrawer(typeof(BoxGroupAttribute))]
    public sealed class BoxGroupDrawer : AlchemyGroupDrawer
    {
        public override VisualElement CreateRootElement(string label)
        {
            var helpBox = new HelpBox()
            {
                text = label,
                style = {
                    flexDirection = FlexDirection.Column,
                    width = Length.Percent(100f),
                    marginTop = 3f,
                    paddingBottom = 3f,
                    paddingRight = 3f,
                    paddingLeft = 3f,
                }
            };

            var labelElement = helpBox.Q<Label>();
            labelElement.style.top = 2f;
            labelElement.style.left = 2f;
            labelElement.style.fontSize = 12f;
            labelElement.style.minHeight = EditorGUIUtility.singleLineHeight;
            labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            labelElement.style.alignSelf = Align.Stretch;

            return helpBox;
        }
    }

    [CustomGroupDrawer(typeof(TabGroupAttribute))]
    public sealed class TabGroupDrawer : AlchemyGroupDrawer
    {
        // Adding a button past value.length assigns value and notifies. Stay at the 64-option
        // maximum, with one bit set, so building the bar does not rewrite the saved tab.
#if UNITY_2023_2_OR_NEWER
        const int ToggleStateLength = 64;
#endif

        VisualElement rootElement;
        VisualElement tabBar;
#if UNITY_2023_2_OR_NEWER
        ToggleButtonGroup segmentedTabBar;
#endif
        readonly Dictionary<string, VisualElement> tabElements = new();
        readonly List<string> tabNames = new();
        readonly List<Button> tabButtons = new();

        string configKey;
        int tabIndex;
        float toolbarHeight;

        public override VisualElement CreateRootElement(string label)
        {
            configKey = UniqueId + "_TabGroup";
            int.TryParse(EditorUserSettings.GetConfigValue(configKey), out tabIndex);

            rootElement = new HelpBox()
            {
                style = {
                    flexDirection = FlexDirection.Column,
                    width = Length.Percent(100f),
                    marginTop = 3f,
                    paddingBottom = 3f,
                    paddingRight = 3f,
                    paddingLeft = 3f,
                }
            };
            rootElement.Q<Label>()?.RemoveFromHierarchy();

            // Match the old IMGUI toolbar rect: 3.7px bleed over the HelpBox padding, 1px short of the layout slot.
            const float bleed = 3.7f;
            toolbarHeight = EditorGUIUtility.singleLineHeight + bleed - 1f;
#if UNITY_2023_2_OR_NEWER
            var initialBit = (uint)tabIndex < (uint)ToggleStateLength ? tabIndex : 0;
            segmentedTabBar = new ToggleButtonGroup(new ToggleButtonGroupState(1UL << initialBit, ToggleStateLength))
            {
                allowEmptySelection = false,
                isMultipleSelection = false,
            };
            segmentedTabBar.RegisterValueChangedCallback(OnTabGroupChanged);
            segmentedTabBar.contentContainer.style.flexGrow = 1f;
            segmentedTabBar.contentContainer.style.flexDirection = FlexDirection.Row;
            tabBar = segmentedTabBar;
#else
            tabBar = new VisualElement();
#endif
            ConfigureTabBar();
            rootElement.Add(tabBar);
            return rootElement;
        }

        void ConfigureTabBar()
        {
            const float bleed = 3.7f;
            tabBar.style.flexDirection = FlexDirection.Row;
            tabBar.style.flexShrink = 0f;
            tabBar.style.width = Length.Percent(100f);
            tabBar.style.height = toolbarHeight;
            tabBar.style.marginLeft = -bleed;
            tabBar.style.marginRight = -bleed;
            tabBar.style.marginTop = -bleed;
            tabBar.style.marginBottom = 1f;
        }

        public override VisualElement GetGroupElement(Attribute attribute)
        {
            var tabGroupAttribute = (TabGroupAttribute)attribute;

            var tabName = tabGroupAttribute.TabName;
            if (!tabElements.TryGetValue(tabName, out var element))
            {
                element = new VisualElement()
                {
                    style = {
                        width = Length.Percent(100f)
                    }
                };
                rootElement.Add(element);
                tabElements.Add(tabName, element);

                var index = tabNames.Count;
                tabNames.Add(tabName);
#if UNITY_2023_2_OR_NEWER
                if (segmentedTabBar != null && index == ToggleStateLength)
                    UseRegularButtons();
#endif
                tabBar.Add(CreateTabButton(tabName, index));
                ApplyTabState();
            }

            return element;
        }

        Button CreateTabButton(string tabName, int index)
        {
            var button = new Button(() =>
            {
#if UNITY_2023_2_OR_NEWER
                if (segmentedTabBar != null) return;
#endif
                SelectTab(index);
            })
            {
                text = tabName,
                style = {
                    flexGrow = 1f,
                    flexShrink = 1f,
                    flexBasis = Length.Percent(0f),
                    minWidth = 0f,
                    height = toolbarHeight,
                    minHeight = toolbarHeight,
                    maxHeight = toolbarHeight,
                    marginLeft = 0f,
                    marginRight = 0f,
                    marginTop = 0f,
                    marginBottom = 0f,
                    paddingTop = 0f,
                    paddingBottom = 0f,
                    paddingLeft = 4f,
                    paddingRight = 4f,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    overflow = Overflow.Hidden,
                    whiteSpace = WhiteSpace.NoWrap,
                }
            };
            tabButtons.Add(button);
            return button;
        }

        void SelectTab(int index)
        {
            if (index == tabIndex) return;

            tabIndex = index;
            EditorUserSettings.SetConfigValue(configKey, tabIndex.ToString());
            ApplyTabState();
        }

#if UNITY_2023_2_OR_NEWER
        // ToggleButtonGroup stores selection in a ulong. Keep all tabs accessible
        // by switching to the ordinary-button implementation beyond its limit.
        void UseRegularButtons()
        {
            var previous = segmentedTabBar;
            previous.UnregisterValueChangedCallback(OnTabGroupChanged);
            segmentedTabBar = null;
            tabBar = new VisualElement();
            ConfigureTabBar();
            rootElement.Insert(rootElement.IndexOf(previous), tabBar);
            foreach (var button in tabButtons)
            {
                button.RemoveFromHierarchy();
                button.RemoveFromClassList(ToggleButtonGroup.buttonClassName);
                button.RemoveFromClassList(ToggleButtonGroup.buttonLeftClassName);
                button.RemoveFromClassList(ToggleButtonGroup.buttonMidClassName);
                button.RemoveFromClassList(ToggleButtonGroup.buttonRightClassName);
                button.RemoveFromClassList(ToggleButtonGroup.buttonStandaloneClassName);
                tabBar.Add(button);
            }
            previous.RemoveFromHierarchy();
        }

        void OnTabGroupChanged(ChangeEvent<ToggleButtonGroupState> evt)
        {
            var state = evt.newValue;
            var count = tabButtons.Count;
            var limit = state.length < count ? state.length : count;
            for (var i = 0; i < limit; i++)
            {
                if (!state[i]) continue;
                SelectTab(i);
                return;
            }
        }
#endif

        // Visibility updates when tabs are added or the selection changes, not on repaint.
        void ApplyTabState()
        {
            var count = tabButtons.Count;
            if (count == 0) return;

            var selectedIndex = (uint)tabIndex < (uint)count ? tabIndex : 0;
#if UNITY_2023_2_OR_NEWER
            if (segmentedTabBar != null)
                segmentedTabBar.SetValueWithoutNotify(new ToggleButtonGroupState(1UL << selectedIndex, ToggleStateLength));
#endif
            for (var i = 0; i < count; i++)
            {
                var selected = i == selectedIndex;
                tabElements[tabNames[i]].style.display = selected ? DisplayStyle.Flex : DisplayStyle.None;

#if UNITY_2023_2_OR_NEWER
                if (segmentedTabBar != null) continue;
#endif
                var button = tabButtons[i];
                var first = i == 0;
                button.style.borderTopLeftRadius = first ? 3f : 0f;
                button.style.borderBottomLeftRadius = first ? 3f : 0f;
                button.style.borderTopRightRadius = i == count - 1 ? 3f : 0f;
                button.style.borderBottomRightRadius = i == count - 1 ? 3f : 0f;
                if (first) button.style.borderLeftWidth = StyleKeyword.Null;
                else button.style.borderLeftWidth = 0f;
                button.style.unityFontStyleAndWeight = selected ? FontStyle.Bold : StyleKeyword.Null;
            }
        }
    }

    [CustomGroupDrawer(typeof(FoldoutGroupAttribute))]
    public sealed class FoldoutGroupDrawer : AlchemyGroupDrawer
    {
        public override VisualElement CreateRootElement(string label)
        {
            var configKey = UniqueId + "_FoldoutGroup";
            bool.TryParse(EditorUserSettings.GetConfigValue(configKey), out var result);

            var foldout = new Foldout()
            {
                style = {
                    width = Length.Percent(100f)
                },
                text = label,
                value = result
            };

            foldout.RegisterValueChangedCallback(x =>
            {
                EditorUserSettings.SetConfigValue(configKey, x.newValue.ToString());
            });

            return foldout;
        }
    }

    [CustomGroupDrawer(typeof(HorizontalGroupAttribute))]
    public sealed class HorizontalGroupDrawer : AlchemyGroupDrawer
    {
        public override VisualElement CreateRootElement(string label)
        {
            var root = new VisualElement()
            {
                style = {
                    width = Length.Percent(100f),
                    flexDirection = FlexDirection.Row
                }
            };

            static void AdjustLabel(PropertyField element, VisualElement inspector, int childCount)
            {
                if (element.childCount == 0) return;
                if (element.Q<Foldout>() != null) return;

                var field = element[0];
                field.RemoveFromClassList("unity-base-field__aligned");

                var labelElement = field.Q<Label>();
                if (labelElement != null)
                {
                    labelElement.style.minWidth = 0f;
                    labelElement.style.width = GUIHelper.CalculateLabelWidth(element, inspector) * 0.8f / childCount;
                }
            }

            root.schedule.Execute(() =>
            {
                if (root.childCount <= 1) return;

                var visualTree = root.panel.visualTree;

                foreach (var field in root.Query<PropertyField>().Build())
                {
                    AdjustLabel(field, visualTree, root.childCount);
                }
                foreach (var field in root.Query<GenericField>().Children<PropertyField>().Build())
                {
                    AdjustLabel(field, visualTree, root.childCount);
                }
            });

            return root;
        }
    }
    [CustomGroupDrawer(typeof(InlineGroupAttribute))]
    public sealed class InlineGroupDrawer : AlchemyGroupDrawer
    {
        public override VisualElement CreateRootElement(string label)
        {
            return new VisualElement();
        }
    }
}
