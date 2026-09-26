using System;
using System.Collections.Generic;
using System.Reflection;
using Alchemy.Inspector;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Editor.Elements
{
    internal interface IValueDropdownLabel
    {
        string Label { get; set; }
    }

    internal static class ValueDropdownGUI
    {
        public static bool TryCreateSerialized(SerializedProperty property, Type type, MemberInfo member, out VisualElement element)
        {
            element = null;
            member ??= property.GetFieldInfo();
            var attribute = ValueDropdownSource.GetAttribute(member);
            if (attribute == null || member.IsDefined(typeof(DisableAlchemyEditorAttribute), true)) return false;
            var binding = new ValueDropdownBinding(property, member, type);
            element = Create(binding, attribute, ObjectNames.NicifyVariableName(property.displayName));
            return true;
        }

        public static bool TryCreateReflection(object target, MemberInfo member, Action beforeWrite, Action afterWrite, out VisualElement element)
        {
            element = null;
            var attribute = ValueDropdownSource.GetAttribute(member);
            if (attribute == null || member.IsDefined(typeof(DisableAlchemyEditorAttribute), true)) return false;
            Type type;
            Func<object> read;
            Action<object> assign = null;
            if (member is FieldInfo field)
            {
                type = field.FieldType;
                read = () => field.GetValue(field.IsStatic ? null : target);
                if (!field.IsInitOnly && !field.IsLiteral && (field.IsStatic || target != null))
                    assign = value => field.SetValue(field.IsStatic ? null : target, value);
            }
            else if (member is PropertyInfo property && property.CanRead && property.GetIndexParameters().Length == 0 && property.IsDefined(typeof(ShowInInspectorAttribute), true))
            {
                type = property.PropertyType;
                read = () => property.GetValue(property.GetGetMethod(true).IsStatic ? null : target);
                if (property.CanWrite && (property.GetGetMethod(true).IsStatic || target != null))
                    assign = value => property.SetValue(property.GetSetMethod(true).IsStatic ? null : target, value);
            }
            else return false;
            var callback = member.GetCustomAttribute<OnValueChangedAttribute>();
            var callbacks = callback != null && target != null
                ? Drawers.OnValueChangedDrawer.FindCallbacks(target.GetType(), callback.MethodName, type)
                : Array.Empty<MethodInfo>();
            var binding = new ValueDropdownBinding(target, member, type, read, assign, beforeWrite, value =>
            {
                if (target is ISerializationCallbackReceiver receiver) receiver.OnBeforeSerialize();
                afterWrite?.Invoke();
                Drawers.OnValueChangedDrawer.InvokeCallbacks(target, callbacks, () => value);
            });
            element = Create(binding, attribute, ObjectNames.NicifyVariableName(member.Name));
            return true;
        }

        static VisualElement Create(ValueDropdownBinding binding, ValueDropdownAttribute attribute, string label)
        {
            if (binding.IsCollection) return new ValueDropdownCollectionElement(binding, attribute, label);
            var field = new ValueDropdownElement(binding, attribute, () => -1, label);
            binding.Track(field);
            return field;
        }

        // edited receives a reflected field's own edits before they are written.
        public static VisualElement DefaultField(ValueDropdownBinding binding, int index, string label, Action<object> edited = null)
        {
            if (binding.SerializedObject != null)
            {
                var property = binding.FindProperty(index);
                if (property == null) return new Label("Property unavailable");
                VisualElement field = InternalAPIHelper.GetDrawerTypeForType(binding.ValueType, property.propertyType == SerializedPropertyType.ManagedReference) != null
                    ? new PropertyField(property, label)
                    : new AlchemyPropertyField(property, binding.ValueType, index >= 0, true) { Label = label };
                field.Bind(binding.SerializedObject);
                return field;
            }
            var reflected = new GenericField(binding.Read(0, index), binding.ValueType, label, true);
            reflected.OnValueChanged += value =>
            {
                edited?.Invoke(value);
                binding.Set(index, new[] { value });
            };
            return reflected;
        }

        public static void StyleLabel(VisualElement element, MemberInfo member)
        {
            var label = element.Q<Label>();
            if (label == null) return;
            var width = member.GetCustomAttribute<LabelWidthAttribute>();
            if (width == null) GUIHelper.ScheduleAdjustLabelWidth(element);
            else GUIHelper.SetMinAndCurrentWidth(label, width.Width);
        }
    }

    internal sealed class ValueDropdownElement : VisualElement, IValueDropdownLabel
    {
        sealed class DisplayField : BaseField<string>
        {
            public DisplayField(string label, Button input) : base(label, input)
            {
                AddToClassList("unity-base-field__aligned");
            }
        }

        readonly ValueDropdownBinding binding;
        readonly ValueDropdownAttribute attribute;
        readonly Func<int> index;
        readonly Button button;
        readonly DisplayField display;
        VisualElement original;
        object originalValue;
        HelpBox error;
        readonly Action<Exception> reportError;
        readonly bool adding;
        ValueDropdownPopup popup;
        string label;
        bool subscribed;

        // Collection rows pass reportError. Their collection shows their errors and refreshes them, so they do not subscribe to changes.
        public ValueDropdownElement(ValueDropdownBinding binding, ValueDropdownAttribute attribute, Func<int> index, string label, bool adding = false, Action<Exception> reportError = null)
        {
            this.binding = binding;
            this.attribute = attribute;
            this.index = index;
            this.label = label;
            this.adding = adding;
            this.reportError = reportError;
            button = new Button(Open);
            if (adding)
            {
                button.text = "Add from dropdown";
                Add(button);
            }
            else if (attribute.Mode == ValueDropdownMode.Replace)
            {
                display = new DisplayField(label, button);
                button.style.flexGrow = 1;
                Add(display);
                ValueDropdownGUI.StyleLabel(display, binding.Member);
            }
            else
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                original = CreateOriginal();
                row.Add(original);
                button.text = "▾";
                button.tooltip = "Select from available values";
                button.style.width = 24;
                row.Add(button);
                Add(row);
                // PropertyField creates its label after construction. Reflected fields do not support LabelWidth.
                var width = binding.Member.GetCustomAttribute<LabelWidthAttribute>();
                if (width != null && binding.SerializedObject != null) GUIHelper.ScheduleSetLabelWidth(original, width.Width);
            }
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (!subscribed && reportError == null) { binding.Changed += OnChanged; subscribed = true; }
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                popup?.Close();
                if (subscribed) { binding.Changed -= OnChanged; subscribed = false; }
            });
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                var i = index();
                if (i >= -1 && binding.SerializedObject != null && binding.IsPrefabOverride(i))
                    evt.menu.AppendAction("Revert", _ =>
                    {
                        try { binding.Revert(i); }
                        catch (Exception exception) { ShowError(exception); }
                    }, binding.CanWrite && enabledInHierarchy ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }));
        }

        VisualElement CreateOriginal()
        {
            var i = index();
            var field = ValueDropdownGUI.DefaultField(binding, i, label, value => originalValue = value);
            if (binding.SerializedObject == null) originalValue = binding.Read(0, i);
            field.style.flexGrow = 1;
            field.SetEnabled(binding.CanWrite && attribute.Mode != ValueDropdownMode.AppendReadOnly);
            return field;
        }

        public string Label
        {
            get => label;
            set
            {
                label = value;
                if (display != null) display.label = value;
                else if (original is AlchemyPropertyField field) field.Label = value;
                else if (original is PropertyField propertyField) propertyField.label = value;
                else if (original != null && original.Q<Label>() is Label originalLabel) originalLabel.text = value;
            }
        }

        void OnChanged()
        {
            popup?.Close();
            Refresh();
        }

        public void ResetBinding()
        {
            popup?.Close();
            if (error != null) error.style.display = DisplayStyle.None;
        }

        public void Refresh()
        {
            var i = index();
            if (i < -1) return;
            try
            {
                var canWrite = binding.CanWrite;
                button.SetEnabled(adding ? binding.CanResize : canWrite);
                if (original != null)
                {
                    // Reflected fields are not bound. Rebuild one when its value changed elsewhere, such as in the picker.
                    if (binding.SerializedObject == null && !Equals(originalValue, binding.Read(0, i)))
                    {
                        var replacement = CreateOriginal();
                        original.parent.Insert(original.parent.IndexOf(original), replacement);
                        original.RemoveFromHierarchy();
                        original = replacement;
                    }
                    original.SetEnabled(canWrite && attribute.Mode != ValueDropdownMode.AppendReadOnly);
                }
                if (display != null)
                {
                    button.text = binding.DisplayText(i, attribute) + "  ▾";
                    display.labelElement.style.unityFontStyleAndWeight = binding.IsPrefabOverride(i) ? FontStyle.Bold : FontStyle.Normal;
                }
                if (error != null) error.style.display = DisplayStyle.None;
            }
            catch (Exception exception) { ShowError(exception); }
        }

        void Open()
        {
            var i = index();
            if (i < -1 || !enabledInHierarchy || !button.enabledInHierarchy) return;
            try
            {
                popup?.Close();
                popup = null;
                if (error != null) error.style.display = DisplayStyle.None;
                var session = new ValueDropdownSession(binding, attribute, i, adding);
                // The session evaluated the provider again, so refresh the label shown for the current value.
                var current = session.CurrentChoice;
                if (!adding && display != null && current >= 0)
                {
                    binding.RememberLabel(i, session.Snapshot.Entries[current].Text);
                    Refresh();
                }
                popup = new ValueDropdownPopup(button, session, attribute, ShowError, () => popup = null);
                UnityEditor.PopupWindow.Show(button.worldBound, popup);
            }
            catch (Exception exception)
            {
                popup?.Close();
                popup = null;
                ShowError(exception);
            }
        }

        void ShowError(Exception exception)
        {
            if (reportError != null) { reportError(exception); return; }
            if (error == null) { error = new HelpBox(string.Empty, HelpBoxMessageType.Error); Add(error); }
            error.text = $"ValueDropdown ({attribute.ValuesGetter}): {exception.GetBaseException().Message}";
            error.style.display = DisplayStyle.Flex;
        }
    }

    internal sealed class ValueDropdownCollectionElement : VisualElement, IValueDropdownLabel
    {
        sealed class Row : VisualElement
        {
            public int Index;
            public ValueDropdownElement Dropdown;
        }

        readonly ValueDropdownBinding binding;
        readonly ValueDropdownAttribute attribute;
        readonly ListView list;
        readonly List<int> indices = new();
        readonly VisualElement footer;
        readonly HelpBox error;
        bool subscribed;
        bool refreshQueued;

        public ValueDropdownCollectionElement(ValueDropdownBinding binding, ValueDropdownAttribute attribute, string label)
        {
            this.binding = binding;
            this.attribute = attribute;
            var settings = binding.Member.GetCustomAttribute<ListViewSettingsAttribute>();
            list = GUIHelper.CreateDefaultListView(label);
            // The items are local indices, not a bound collection, so the size field and default footer stay hidden.
            list.showBoundCollectionSize = false;
            list.showAddRemoveFooter = false;
            GUIHelper.ApplyListViewSettings(list, settings);
            list.viewDataKey = "alchemy-value-dropdown-" + binding.Key;
            list.virtualizationMethod = attribute.Mode == ValueDropdownMode.Replace && attribute.ListMode != ValueDropdownListMode.AddOnly
                ? CollectionVirtualizationMethod.FixedHeight : CollectionVirtualizationMethod.DynamicHeight;
            list.fixedItemHeight = 24;
            list.style.maxHeight = 340;
            list.makeItem = () => new Row();
            list.bindItem = (element, i) => Bind((Row)element, i);
            list.unbindItem = (element, _) =>
            {
                var row = (Row)element;
                row.Index = -2;
                row.Dropdown?.ResetBinding();
                if (attribute.Mode != ValueDropdownMode.Replace || attribute.ListMode == ValueDropdownListMode.AddOnly)
                {
                    element.Unbind();
                    row.Clear();
                    row.Dropdown = null;
                }
            };
            list.itemIndexChanged += (from, to) => Execute(() => binding.Move(from, to));
            list.itemsSource = indices;
            Add(list);
            if (settings == null || settings.ShowAddRemoveFooter)
            {
                footer = new VisualElement();
                footer.style.flexDirection = FlexDirection.Row;
                if (attribute.ListMode != ValueDropdownListMode.ElementsOnly)
                {
                    var add = new ValueDropdownElement(binding, attribute, () => -1, label, true);
                    footer.Add(add);
                }
                else
                {
                    footer.Add(new Button(() => Execute(binding.AppendDefault)) { text = "+" });
                }
                footer.Add(new Button(() => Execute(() =>
                {
                    var selected = new List<int>(list.selectedIndices);
                    if (selected.Count == 0) return;
                    binding.Remove(selected.ToArray());
                    // Selection is by index; do not let it move onto the elements that shift into place.
                    list.ClearSelection();
                })) { text = "Remove selected" });
                Add(footer);
            }
            error = new HelpBox(string.Empty, HelpBoxMessageType.Error);
            error.style.display = DisplayStyle.None;
            Add(error);
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (!subscribed) { binding.Changed += QueueRefresh; subscribed = true; }
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                if (subscribed) { binding.Changed -= QueueRefresh; subscribed = false; }
            });
            binding.Track(this);
        }

        public string Label { get => list.headerTitle; set => list.headerTitle = value; }

        void Bind(Row row, int index)
        {
            row.Index = index;
            var label = "Element " + index;
            if (attribute.ListMode == ValueDropdownListMode.AddOnly)
            {
                row.Clear();
                row.Add(ValueDropdownGUI.DefaultField(binding, index, label));
            }
            else
            {
                if (row.Dropdown == null || attribute.Mode != ValueDropdownMode.Replace)
                {
                    row.Clear();
                    row.Dropdown = new ValueDropdownElement(binding, attribute, () => row.Index, label, reportError: ShowError);
                    row.Add(row.Dropdown);
                }
                row.Dropdown.Label = label;
                row.Dropdown.Refresh();
            }
        }

        void QueueRefresh()
        {
            if (refreshQueued) return;
            refreshQueued = true;
            schedule.Execute(() => { refreshQueued = false; if (panel != null) Refresh(); });
        }

        void Refresh()
        {
            Execute(() =>
            {
                var count = binding.Count;
                if (indices.Count != count)
                {
                    indices.Clear();
                    for (var i = 0; i < count; i++) indices.Add(i);
                }
                list.SetEnabled(binding.CanWrite);
                footer?.SetEnabled(binding.CanResize);
                list.RefreshItems();
            });
        }

        void ShowError(Exception exception)
        {
            error.text = exception.GetBaseException().Message;
            error.style.display = DisplayStyle.Flex;
        }

        void Execute(Action action)
        {
            // Clear first: rows refreshed by the action report their errors here.
            error.style.display = DisplayStyle.None;
            try { action(); }
            catch (Exception exception) { ShowError(exception); }
        }
    }
}
