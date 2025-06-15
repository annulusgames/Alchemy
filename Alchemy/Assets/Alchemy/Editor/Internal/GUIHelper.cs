using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using Alchemy.Inspector;

namespace Alchemy.Editor
{
    internal static class GUIHelper
    {
        public static Color LineColor => EditorGUIUtility.isProSkin ? new(0.4f, 0.4f, 0.4f) : new(0.6f, 0.6f, 0.6f);
        public static Color SubtitleColor => new(0.5f, 0.5f, 0.5f);
        public static Color TextColor => EditorGUIUtility.isProSkin ? new(0.725f, 0.725f, 0.725f) : new(0.141f, 0.141f, 0.141f);

        public static float CalculateFieldWidth(VisualElement element, VisualElement root)
        {
            var labelWidth = CalculateLabelWidth(element, root);
            return root.resolvedStyle.width - labelWidth - 27f;
        }

        public static float CalculateLabelWidth(VisualElement element, VisualElement root)
        {
            // This code is a partial modification of the Label width calculation method actually used inside PropertyField.
            var num = root.resolvedStyle.paddingLeft;
            var num2 = 37f;
            var num3 = 123f;
            var num4 = element.GetFirstAncestorOfType<Foldout>() == null ? 0f : 15f;

            var width = root.resolvedStyle.width;
            var a = width * 0.45f - num2 - num - num4;
            var b = Mathf.Max(num3 - num - num4, 0f);

            return Mathf.Max(a, b) + 12f;
        }

        public static ListView CreateDefaultListView(string label)
        {
            return new ListView()
            {
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showBorder = true,
                showFoldoutHeader = true,
                headerTitle = label,
                showAddRemoveFooter = true,
                fixedItemHeight = 20f,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                showAlternatingRowBackgrounds = AlternatingRowBackground.None,
            };
        }

        public static ListView CreateListViewFromFieldInfo(object target, FieldInfo fieldInfo)
        {
            var settings = fieldInfo.GetCustomAttribute<ListViewSettingsAttribute>();
            var listView = new ListView
            {
                reorderable = settings == null ? true : settings.Reorderable,
                reorderMode = settings == null ? ListViewReorderMode.Animated : settings.ReorderMode,
                showBorder = settings == null ? true : settings.ShowBorder,
                showFoldoutHeader = settings == null ? true : settings.ShowFoldoutHeader,
                showBoundCollectionSize = settings == null ? true : (settings.ShowFoldoutHeader && settings.ShowBoundCollectionSize),
                selectionType = settings == null ? SelectionType.Multiple : settings.SelectionType,
                showAddRemoveFooter = settings == null ? true : settings.ShowAddRemoveFooter,
                fixedItemHeight = 20f,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                showAlternatingRowBackgrounds = settings == null ? AlternatingRowBackground.None : settings.ShowAlternatingRowBackgrounds,
            };

            var events = fieldInfo.GetCustomAttribute<OnListViewChangedAttribute>();
            if (events != null)
            {
                listView.itemsAdded += indices =>
                {
                    if (events.OnItemsAdded == null) return;
                    ReflectionHelper.Invoke(target, events.OnItemsAdded, new object[] { indices });
                };
                listView.itemsRemoved += indices =>
                {
                    if (events.OnItemsRemoved == null) return;
                    ReflectionHelper.Invoke(target, events.OnItemsRemoved, new object[] { indices });
                };
#if UNITY_2022_1_OR_NEWER
                listView.itemsChosen += items =>
                {
                    if (events.OnItemsChosen == null) return;
                    ReflectionHelper.Invoke(target, events.OnItemsChosen, new object[] { items });
                };
#else 
                 listView.onItemsChosen += items =>
                {
                    if (events.OnItemsChosen == null) return;
                    ReflectionHelper.Invoke(target, events.OnItemsChosen, new object[] { items });
                };
#endif
                
                listView.itemIndexChanged += (before, after) =>
                {
                    if (events.OnItemIndexChanged == null) return;
                    ReflectionHelper.Invoke(target, events.OnItemIndexChanged, new object[] { before, after });
                };
#if UNITY_2022_1_OR_NEWER
                listView.selectionChanged += items =>
                {
                    if (events.OnSelectionChanged == null) return;
                    ReflectionHelper.Invoke(target, events.OnSelectionChanged, new object[] { items });
                };

                listView.selectedIndicesChanged += indices =>
                {
                    if (events.OnSelectedIndicesChanged== null) return;
                    ReflectionHelper.Invoke(target, events.OnSelectedIndicesChanged, new object[] { indices });
                };
#else
                listView.onSelectionChange += items =>
                {
                    if (events.OnSelectionChanged == null) return;
                    ReflectionHelper.Invoke(target, events.OnSelectionChanged, new object[] { items });
                };

                listView.onSelectedIndicesChange += indices =>
                {
                    if (events.OnSelectedIndicesChanged== null) return;
                    ReflectionHelper.Invoke(target, events.OnSelectedIndicesChanged, new object[] { indices });
                };
#endif
                listView.itemsSourceChanged += () =>
                {
                    if (events.OnItemsSourceChanged == null) return;
                    ReflectionHelper.Invoke(target, events.OnItemsSourceChanged, null);
                };
            }

            return listView;
        }

        public static PropertyField CreateObjectPropertyField(SerializedProperty property, Type type)
        {
            Assert.IsTrue(property.propertyType == SerializedPropertyType.ObjectReference);

            var fieldInfo = property.GetFieldInfo();
            var isAssetsOnly = fieldInfo.HasCustomAttribute<AssetsOnlyAttribute>();

            var propertyField = new PropertyField(property);
            propertyField.RegisterValueChangeCallback(x =>
            {
                var objectField = propertyField.Q<ObjectField>();
                if (objectField == null) return;
                objectField.objectType = type;
                objectField.allowSceneObjects = !isAssetsOnly;
            });

            return propertyField;
        }

        public static void ScheduleAdjustLabelWidth(VisualElement element)
        {
            void Adjust(VisualElement visualElement)
            {
                var label = element.Q<Label>();
                if (label == null) return;
                label.style.minWidth = 0f;
                label.style.width = CalculateLabelWidth(element, visualElement);
            }

            // Adjust label width
            element.schedule.Execute(() =>
            {
                var visualTree = element.panel.visualTree;
                visualTree.RegisterCallback<GeometryChangedEvent>(x => Adjust(visualTree));
                Adjust(visualTree);
            });
        }

        public static IMGUIContainer CreateLine(Color color, float height)
        {
            return new IMGUIContainer(() =>
            {
                var rect = EditorGUILayout.GetControlRect(false, height);
                rect.xMin += 3f;
                rect.y += rect.height * 0.5f;
                rect.height = 1f;
                EditorGUI.DrawRect(rect, color);
            });
        }

        public static void SetMinAndCurrentWidth(VisualElement visualElement, float value)
        {
            visualElement.style.minWidth = value;
            visualElement.style.width = value;
        }
        
        public static void ModifyChildFoldouts(VisualElement element, string className)
        {
            element.schedule.Execute(() =>
            {
                foreach (var foldout in element.Query<Foldout>().Build())
                {
                    if (foldout.parent == element || foldout.parent.parent == element)
                        foldout.AddToClassList(className);
                }
            });
        }
    }
}