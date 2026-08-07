using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Inspector
{
    /// <summary>
    /// Disables AlchemyEditor for the target class and uses the default Inspector instead. When this attribute is added to a field, only that field is rendered using the default PropertyField.
    /// </summary>
    /// <alchemy-attr-category>General</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class DisableAlchemyEditorAttribute : Attribute { }

    /// <summary>
    /// Hides the target script field.
    /// </summary>
    /// <alchemy-attr-category>General</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HideScriptFieldAttribute : Attribute { }

    /// <summary>
    /// Changes the display order of the member. The default order is 0, and members are displayed in ascending order.
    /// Uses the same scale as group <c>order</c>, so ungrouped members and sibling groups interleave by this value.
    /// </summary>
    /// <alchemy-attr-category>General</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class OrderAttribute : Attribute
    {
        public OrderAttribute(int order) => Order = order;

        /// <summary>
        /// The display order of the member. Shares the same scale as group <c>order</c>.
        /// </summary>
        public int Order { get; }
    }

    /// <summary>
    /// Displays a button in the Inspector that can execute a method. If the method has parameters, input fields for those parameters will be added.
    /// </summary>
    /// <alchemy-attr-category>General</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ButtonAttribute : Attribute { }

    /// <summary>
    /// Displays nonserialized fields and properties in the Inspector. Writable members can be edited, but their values are not serialized or persisted.
    /// </summary>
    /// <alchemy-attr-category>General</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ShowInInspectorAttribute : Attribute { }

    /// <summary>
    /// Restricts an object field to asset references.
    /// </summary>
    /// <alchemy-attr-category>General</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AssetsOnlyAttribute : Attribute { }

    /// <summary>
    /// Displays the Inspector for the referenced ScriptableObject or component inline, allowing it to be edited in place.
    /// </summary>
    /// <alchemy-attr-category>General</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class InlineEditorAttribute : Attribute { }

    /// <summary>
    /// Adds an indent to the field in the Inspector.
    /// </summary>
    /// <alchemy-attr-category>General</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class IndentAttribute : Attribute
    {
        public IndentAttribute(int indent = 1) => this.indent = indent;

        /// <summary>
        /// Number of indent levels.
        /// </summary>
        public readonly int indent;
    }

    /// <summary>
    /// Makes the field uneditable.
    /// </summary>
    /// <alchemy-attr-category>General</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class ReadOnlyAttribute : Attribute { }

    /// <summary>
    /// Hides the field while in Play Mode.
    /// </summary>
    /// <alchemy-attr-category>Conditionals</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class HideInPlayModeAttribute : Attribute { }

    /// <summary>
    /// Hides the field while in Edit Mode.
    /// </summary>
    /// <alchemy-attr-category>Conditionals</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class HideInEditModeAttribute : Attribute { }

    /// <summary>
    /// During Play Mode, the field becomes disabled.
    /// </summary>
    /// <alchemy-attr-category>Conditionals</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class DisableInPlayModeAttribute : Attribute { }

    /// <summary>
    /// During Edit Mode, the field becomes disabled.
    /// </summary>
    /// <alchemy-attr-category>Conditionals</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class DisableInEditModeAttribute : Attribute { }

    /// <summary>
    /// Hides the label of the field.
    /// </summary>
    /// <alchemy-attr-category>Decorations</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class HideLabelAttribute : Attribute { }

    /// <summary>
    /// Overrides the label text of the field.
    /// </summary>
    /// <alchemy-attr-category>Decorations</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class LabelTextAttribute : Attribute
    {
        public LabelTextAttribute(string text) => Text = text;

        /// <summary>
        /// The text to display on the field label.
        /// </summary>
        public string Text { get; }
    }

    /// <summary>
    /// Sets the width of the field label.
    /// </summary>
    /// <alchemy-attr-category>Decorations</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class LabelWidthAttribute : Attribute
    {
        public LabelWidthAttribute(float width) => Width = width;

        /// <summary>
        /// The width of the label in pixels.
        /// </summary>
        public float Width { get; }
    }

    /// <summary>
    /// Hides the member in the Inspector when the specified condition evaluates to true.
    /// </summary>
    /// <alchemy-attr-category>Conditionals</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class HideIfAttribute : Attribute
    {
        public HideIfAttribute(string condition) => Condition = condition;

        /// <summary>
        /// The name of the field, property, or method used to evaluate the condition.
        /// </summary>
        public string Condition { get; }
    }

    /// <summary>
    /// Displays the field in the Inspector when the specified condition evaluates to true.
    /// </summary>
    /// <alchemy-attr-category>Conditionals</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class ShowIfAttribute : Attribute
    {
        public ShowIfAttribute(string condition) => Condition = condition;

        /// <summary>
        /// The name of the field, property, or method used to evaluate the condition.
        /// </summary>
        public string Condition { get; }
    }

    /// <summary>
    /// Disables the field when the specified condition evaluates to true.
    /// </summary>
    /// <alchemy-attr-category>Conditionals</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class DisableIfAttribute : Attribute
    {
        public DisableIfAttribute(string condition) => Condition = condition;

        /// <summary>
        /// The name of the field, property, or method used to evaluate the condition.
        /// </summary>
        public string Condition { get; }
    }

    /// <summary>
    /// Enables the field when the specified condition evaluates to true.
    /// </summary>
    /// <alchemy-attr-category>Conditionals</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class EnableIfAttribute : Attribute
    {
        public EnableIfAttribute(string condition) => Condition = condition;

        /// <summary>
        /// The name of the field, property, or method used to evaluate the condition.
        /// </summary>
        public string Condition { get; }
    }

    /// <summary>
    /// Displays a warning when no object reference is assigned to the field.
    /// </summary>
    /// <alchemy-attr-category>Validation</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class RequiredAttribute : Attribute
    {
        public RequiredAttribute() => Message = null;
        public RequiredAttribute(string message) => Message = message;

        /// <summary>
        /// Text to display in the warning.
        /// </summary>
        public string Message { get; }
    }

    /// <summary>
    /// Displays a warning when the specified validation condition evaluates to false.
    /// </summary>
    /// <alchemy-attr-category>Validation</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateInputAttribute : Attribute
    {
        public ValidateInputAttribute(string condition)
        {
            Condition = condition;
            Message = null;
        }

        public ValidateInputAttribute(string condition, string message)
        {
            Condition = condition;
            Message = message;
        }

        /// <summary>
        /// The name of the field, property, or method used to evaluate the condition.
        /// </summary>
        public string Condition { get; }

        /// <summary>
        /// Text to display in the warning.
        /// </summary>
        public string Message { get; }
    }

    /// <summary>
    /// Adds a note or warning above a field.
    /// </summary>
    /// <alchemy-attr-category>Decorations</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class HelpBoxAttribute : Attribute
    {
        public HelpBoxAttribute(string message, HelpBoxMessageType messageType = HelpBoxMessageType.Info)
        {
            Message = message;
            MessageType = messageType;
        }

        /// <summary>
        /// The text to display inside the box.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The type of message.
        /// </summary>
        public HelpBoxMessageType MessageType { get; }
    }

    /// <summary>
    /// Displays a preview of the referenced asset next to the field.
    /// </summary>
    /// <alchemy-attr-category>Decorations</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class PreviewAttribute : Attribute
    {
        public PreviewAttribute() : this(40, Align.FlexEnd) { }

        public PreviewAttribute(float size) : this(size, Align.FlexEnd) { }

        public PreviewAttribute(float size, Align align)
        {
            Size = size;
            AlignStyle = align;
        }

        /// <summary>
        /// The size of the preview image.
        /// </summary>
        public float Size { get; }

        /// <summary>
        /// The alignment of the preview image.
        /// </summary>
        public StyleEnum<Align> AlignStyle { get; }
    }

    /// <summary>
    /// Adds a horizontal line to the Inspector.
    /// </summary>
    /// <alchemy-attr-category>Decorations</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class HorizontalLineAttribute : Attribute
    {
        public HorizontalLineAttribute()
        {
            Color = default;
        }

        /// <param name="r">Red component of the line color.</param>
        /// <param name="g">Green component of the line color.</param>
        /// <param name="b">Blue component of the line color.</param>
        public HorizontalLineAttribute(float r, float g, float b)
        {
            Color = new Color(r, g, b);
        }

        /// <param name="r">Red component of the line color.</param>
        /// <param name="g">Green component of the line color.</param>
        /// <param name="b">Blue component of the line color.</param>
        /// <param name="a">Alpha value of the line color.</param>
        public HorizontalLineAttribute(float r, float g, float b, float a)
        {
            Color = new Color(r, g, b, a);
        }

        /// <summary>
        /// The color of the line. When unset, the default Inspector line color is used.
        /// </summary>
        public Color Color { get; }
    }


    /// <summary>
    /// Displays a header with a separator line in the Inspector.
    /// </summary>
    /// <alchemy-attr-category>Decorations</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class TitleAttribute : Attribute
    {
        public TitleAttribute(string titleText)
        {
            TitleText = titleText;
            SubtitleText = null;
        }

        public TitleAttribute(string titleText, string subtitle)
        {
            TitleText = titleText;
            SubtitleText = subtitle;
        }


        /// <summary>
        /// Text to display in the header.
        /// </summary>
        public string TitleText { get; }

        /// <summary>
        /// Text displayed below the title in smaller font.
        /// </summary>
        public string SubtitleText { get; }
    }

    /// <summary>
    /// Displays a quotation in the Inspector.
    /// </summary>
    /// <alchemy-attr-category>Decorations</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class BlockquoteAttribute : Attribute
    {
        public BlockquoteAttribute(string text)
        {
            Text = text;
        }

        /// <summary>
        /// The text to display in the quotation.
        /// </summary>
        public string Text { get; }
    }

    /// <summary>
    /// Executes a method with the specified name when the value of the field changes.
    /// </summary>
    /// <alchemy-attr-category>Events</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class OnValueChangedAttribute : Attribute
    {
        public OnValueChangedAttribute(string methodName) => MethodName = methodName;

        /// <summary>
        /// The name of the method to be called when the value changes.
        /// </summary>
        public string MethodName { get; }
    }

    /// <summary>
    /// Executes a method when the Inspector is enabled.
    /// </summary>
    /// <alchemy-attr-category>Events</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnInspectorEnableAttribute : Attribute { }

    /// <summary>
    /// Executes a method when the Inspector is disabled.
    /// </summary>
    /// <alchemy-attr-category>Events</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnInspectorDisableAttribute : Attribute { }

    /// <summary>
    /// Executes a method when the Inspector is destroyed.
    /// </summary>
    /// <alchemy-attr-category>Events</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnInspectorDestroyAttribute : Attribute { }

    /// <summary>
    /// Changes how collections are displayed. This attribute can improve row readability and prevent users from changing the collection size or element order in the Inspector.
    /// </summary>
    /// <alchemy-attr-category>General</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ListViewSettingsAttribute : Attribute
    {
        /// <summary>Whether to show the add/remove footer.</summary>
        public bool ShowAddRemoveFooter { get; set; } = true;
        /// <summary>Alternating row background style.</summary>
        public AlternatingRowBackground ShowAlternatingRowBackgrounds { get; set; } = AlternatingRowBackground.None;
        /// <summary>Whether to show the list border.</summary>
        public bool ShowBorder { get; set; } = true;
        /// <summary>Whether to show the bound collection size field.</summary>
        public bool ShowBoundCollectionSize { get; set; } = true;
        /// <summary>Whether to show the foldout header.</summary>
        public bool ShowFoldoutHeader { get; set; } = true;
        /// <summary>Selection type for list items.</summary>
        public SelectionType SelectionType { get; set; } = SelectionType.Multiple;
        /// <summary>Whether items can be reordered.</summary>
        public bool Reorderable { get; set; } = true;
        /// <summary>Reorder mode for list items.</summary>
        public ListViewReorderMode ReorderMode { get; set; } = ListViewReorderMode.Animated;
    }

    /// <summary>
    /// Detects changes in collections and invokes methods accordingly. Refer to Unity's <see href="https://docs.unity3d.com/ScriptReference/UIElements.ListView.html">ListView documentation</see> for details on each event.
    /// </summary>
    /// <alchemy-attr-note type="WARNING">
    /// Ensure that each callback method's parameter types exactly match the corresponding <c>ListView</c> event signature shown below. Otherwise, Alchemy reports an error and does not invoke the method.
    /// </alchemy-attr-note>
    /// <alchemy-attr-category>Events</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class OnListViewChangedAttribute : Attribute
    {
        /// <summary>Name of the method called when an item's value changes <c>(int index, T value)</c>.</summary>
        public string OnItemChanged { get; set; }
        /// <summary>Name of the method called when an item's index changes <c>(int before, int after)</c>.</summary>
        public string OnItemIndexChanged { get; set; }
        /// <summary>Name of the method called when items are added <c>(IEnumerable&lt;int&gt; indices)</c>.</summary>
        public string OnItemsAdded { get; set; }
        /// <summary>Name of the method called when items are removed <c>(IEnumerable&lt;int&gt; indices)</c>.</summary>
        public string OnItemsRemoved { get; set; }
        /// <summary>Name of the method called when items are chosen by pressing Enter or double-clicking <c>(IEnumerable&lt;object&gt; items)</c>.</summary>
        public string OnItemsChosen { get; set; }
        /// <summary>Name of the method called when the source collection changes, such as when its count changes <c>(no arguments)</c>.</summary>
        public string OnItemsSourceChanged { get; set; }
        /// <summary>Name of the method called when the selected items change <c>(IEnumerable&lt;object&gt; items)</c>.</summary>
        public string OnSelectionChanged { get; set; }
        /// <summary>Name of the method called when the selected indices change <c>(IEnumerable&lt;int&gt; indices)</c>.</summary>
        public string OnSelectedIndicesChanged { get; set; }

    }
}
