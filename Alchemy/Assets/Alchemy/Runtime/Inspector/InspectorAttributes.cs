using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Inspector
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class DisableAlchemyEditorAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HideScriptFieldAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class OrderAttribute : Attribute
    {
        public OrderAttribute(int order) => Order = order;
        public int Order { get; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ButtonAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ShowInInspectorAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AssetsOnlyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class InlineEditorAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class IndentAttribute : Attribute
    {
        public IndentAttribute(int indent = 1) => this.indent = indent;
        public readonly int indent;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class ReadOnlyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class HideInPlayModeAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class HideInEditModeAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class DisableInPlayModeAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class DisableInEditModeAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class HideLabelAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class LabelTextAttribute : Attribute
    {
        public LabelTextAttribute(string text) => Text = text;
        public string Text { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class LabelWidthAttribute : Attribute
    {
        public LabelWidthAttribute(float width) => Width = width;
        public float Width { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class HideIfAttribute : Attribute
    {
        public HideIfAttribute(string condition) => Condition = condition;
        public string Condition { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class ShowIfAttribute : Attribute
    {
        public ShowIfAttribute(string condition) => Condition = condition;
        public string Condition { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class DisableIfAttribute : Attribute
    {
        public DisableIfAttribute(string condition) => Condition = condition;
        public string Condition { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class EnableIfAttribute : Attribute
    {
        public EnableIfAttribute(string condition) => Condition = condition;
        public string Condition { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class RequiredAttribute : Attribute
    {
        public RequiredAttribute() => Message = null;
        public RequiredAttribute(string message) => Message = message;
        public string Message { get; }
    }

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

        public string Condition { get; }
        public string Message { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class HelpBoxAttribute : Attribute
    {
        public HelpBoxAttribute(string message, HelpBoxMessageType messageType = HelpBoxMessageType.Info)
        {
            Message = message;
            MessageType = messageType;
        }

        public string Message { get; }
        public HelpBoxMessageType MessageType { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class PreviewAttribute : Attribute { 
        public PreviewAttribute() : this(40, Align.FlexEnd) { }

        public PreviewAttribute(float size) : this (size, Align.FlexEnd) { }

        public PreviewAttribute(float size, Align align) {
            Size = size;
            AlignStyle = align;
        }

        public float Size { get; }
        public StyleEnum<Align> AlignStyle { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class HorizontalLineAttribute : Attribute
    {
        public HorizontalLineAttribute()
        {
            Color = default;
        }

        public HorizontalLineAttribute(float r, float g, float b)
        {
            Color = new Color(r, g, b);
        }

        public HorizontalLineAttribute(float r, float g, float b, float a)
        {
            Color = new Color(r, g, b, a);
        }

        public Color Color { get; }
    }


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


        public string TitleText { get; }
        public string SubtitleText { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class BlockquoteAttribute : Attribute
    {
        public BlockquoteAttribute(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class OnValueChangedAttribute : Attribute
    {
        public OnValueChangedAttribute(string methodName) => MethodName = methodName;
        public string MethodName { get; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnInspectorEnableAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnInspectorDisableAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnInspectorDestroyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ListViewSettingsAttribute : Attribute
    {
        public bool ShowAddRemoveFooter { get; set; } = true;
        public AlternatingRowBackground ShowAlternatingRowBackgrounds { get; set; } = AlternatingRowBackground.None;
        public bool ShowBorder { get; set; } = true;
        public bool ShowBoundCollectionSize { get; set; } = true;
        public bool ShowFoldoutHeader { get; set; } = true;
        public SelectionType SelectionType { get; set; } = SelectionType.Multiple;
        public bool Reorderable { get; set; } = true;
        public ListViewReorderMode ReorderMode { get; set; } = ListViewReorderMode.Animated;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class OnListViewChangedAttribute : Attribute
    {
        public string OnItemChanged { get; set; }
        public string OnItemIndexChanged { get; set; }
        public string OnItemsAdded { get; set; }
        public string OnItemsRemoved { get; set; }
        public string OnItemsChosen { get; set; }
        public string OnItemsSourceChanged { get; set; }
        public string OnSelectionChanged { get; set; }
        public string OnSelectedIndicesChanged { get; set; }
        
    }
}