using System;
using System.Collections.Generic;
using System.Linq;
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
        VisualElement rootElement;
        readonly Dictionary<string, VisualElement> tabElements = new();

        string[] keyArrayCache = new string[0];
        int tabIndex;
        int prevTabIndex;

        sealed class TabItem
        {
            public string name;
            public VisualElement element;
        }

        public override VisualElement CreateRootElement(string label)
        {
            var configKey = UniqueId + "_TabGroup";
            int.TryParse(EditorUserSettings.GetConfigValue(configKey), out tabIndex);
            prevTabIndex = tabIndex;

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

            var tabGUIElement = new IMGUIContainer(() =>
            {
                var rect = EditorGUILayout.GetControlRect();
                rect.xMin -= 3.7f;
                rect.xMax += 3.7f;
                rect.yMin -= 3.7f;
                rect.yMax -= 1f;
                tabIndex = GUI.Toolbar(rect, tabIndex, keyArrayCache);
                if (tabIndex != prevTabIndex)
                {
                    EditorUserSettings.SetConfigValue(configKey, tabIndex.ToString());
                    prevTabIndex = tabIndex;
                }

                foreach (var kv in tabElements)
                {
                    kv.Value.style.display = keyArrayCache[tabIndex] == kv.Key ? DisplayStyle.Flex : DisplayStyle.None;
                }
            })
            {
                style = {
                    width = Length.Percent(100f),
                    marginLeft = 0f,
                    marginRight = 0f,
                    marginTop = 0f
                }
            };
            rootElement.Add(tabGUIElement);

            return rootElement;
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

                keyArrayCache = tabElements.Keys.ToArray();
            }

            return element;
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
