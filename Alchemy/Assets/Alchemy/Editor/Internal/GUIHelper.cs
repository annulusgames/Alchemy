using System;
using System.Reflection;
using Alchemy.Inspector;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

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
            var listView = new ListView();
            ApplyListViewSettings(listView, settings);
            listView.showBoundCollectionSize = settings == null ? true : (settings.ShowFoldoutHeader && settings.ShowBoundCollectionSize);
            listView.showAddRemoveFooter = settings == null ? true : settings.ShowAddRemoveFooter;
            listView.fixedItemHeight = 20f;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

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
                    if (events.OnSelectedIndicesChanged == null) return;
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

        // Appearance, selection, and reordering options. The footer and size field depend on how the list is bound.
        public static void ApplyListViewSettings(ListView listView, ListViewSettingsAttribute settings)
        {
            listView.reorderable = settings == null ? true : settings.Reorderable;
            listView.reorderMode = settings == null ? ListViewReorderMode.Animated : settings.ReorderMode;
            listView.showBorder = settings == null ? true : settings.ShowBorder;
            listView.showFoldoutHeader = settings == null ? true : settings.ShowFoldoutHeader;
            listView.selectionType = settings == null ? SelectionType.Multiple : settings.SelectionType;
            listView.showAlternatingRowBackgrounds = settings == null ? AlternatingRowBackground.None : settings.ShowAlternatingRowBackgrounds;
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
            EventCallback<GeometryChangedEvent> onGeometryChanged = null;
            VisualElement registeredTree = null;

            void Adjust(VisualElement visualElement)
            {
                var label = element.Q<Label>();
                if (label == null) return;
                label.style.minWidth = 0f;
                label.style.width = CalculateLabelWidth(element, visualElement);
            }

            void Unregister()
            {
                if (registeredTree != null && onGeometryChanged != null)
                {
                    registeredTree.UnregisterCallback(onGeometryChanged);
                }

                registeredTree = null;
                onGeometryChanged = null;
            }

            void Register(IPanel panel)
            {
                var visualTree = panel?.visualTree;
                if (visualTree == null) return;
                if (registeredTree == visualTree) return;

                Unregister();
                registeredTree = visualTree;
                onGeometryChanged = _ => Adjust(visualTree);
                visualTree.RegisterCallback(onGeometryChanged);
                Adjust(visualTree);
            }

            element.RegisterCallback<AttachToPanelEvent>(evt => Register(evt.destinationPanel));
            element.RegisterCallback<DetachFromPanelEvent>(_ => Unregister());

            if (element.panel != null)
            {
                Register(element.panel);
            }
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

        // Unity 6000.0 can add a PropertyField label without a later GeometryChangedEvent.
        // Every() repeats once per scheduler update and Pause() stops it. Until() alone
        // replaces the run-once stop and would poll forever when no label appears.
        internal const int LabelWidthRetryLimit = 60;

        public static void ScheduleSetLabelWidth(VisualElement element, float width)
        {
            if (TrySet()) return;

            EventCallback<GeometryChangedEvent> onGeometryChanged = null;
            EventCallback<AttachToPanelEvent> onAttach = null;
            EventCallback<DetachFromPanelEvent> onDetach = null;
            IVisualElementScheduledItem retry = null;
            var attempts = 0;

            void StopRetry()
            {
                retry?.Pause();
            }

            void Unregister()
            {
                StopRetry();
                retry = null;

                if (onGeometryChanged != null)
                {
                    element.UnregisterCallback(onGeometryChanged);
                    onGeometryChanged = null;
                }

                if (onAttach != null)
                {
                    element.UnregisterCallback(onAttach);
                    onAttach = null;
                }

                if (onDetach != null)
                {
                    element.UnregisterCallback(onDetach);
                    onDetach = null;
                }
            }

            void RegisterGeometry()
            {
                if (onGeometryChanged != null) return;

                onGeometryChanged = _ =>
                {
                    if (TrySet()) Unregister();
                };
                element.RegisterCallback(onGeometryChanged);
            }

            void StartRetry()
            {
                if (element.panel == null) return;
                if (retry != null && retry.isActive) return;

                attempts = 0;
                if (retry == null)
                {
                    retry = element.schedule.Execute(() =>
                    {
                        if (element.panel == null) return;

                        if (TrySet())
                        {
                            Unregister();
                            return;
                        }

                        if (++attempts >= LabelWidthRetryLimit) StopRetry();
                    }).Every(0);
                    return;
                }

                retry.Resume();
            }

            onAttach = _ =>
            {
                if (TrySet())
                {
                    Unregister();
                    return;
                }

                RegisterGeometry();
                StartRetry();
            };
            onDetach = _ =>
            {
                StopRetry();
                if (onGeometryChanged == null) return;
                element.UnregisterCallback(onGeometryChanged);
                onGeometryChanged = null;
            };

            element.RegisterCallback(onAttach);
            element.RegisterCallback(onDetach);
            RegisterGeometry();
            StartRetry();

            bool TrySet()
            {
                var label = element.Q<Label>();
                if (label == null) return false;
                SetMinAndCurrentWidth(label, width);
                return true;
            }
        }

        public static void SetMinAndCurrentWidth(VisualElement visualElement, float value)
        {
            visualElement.style.minWidth = value;
            visualElement.style.width = value;
        }
    }
}
