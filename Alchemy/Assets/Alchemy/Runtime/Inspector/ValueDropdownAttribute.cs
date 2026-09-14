using System;
using System.Collections.Generic;

namespace Alchemy.Inspector
{
    /// <summary>Displays a searchable selection of values supplied by a field, property, or method.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValueDropdownAttribute : Attribute
    {
        public ValueDropdownAttribute(string valuesGetter) => ValuesGetter = valuesGetter;

        public ValueDropdownAttribute(Type sourceType, string valuesGetter)
        {
            SourceType = sourceType;
            ValuesGetter = valuesGetter;
        }

        public string ValuesGetter { get; }
        public Type SourceType { get; }
        public ValueDropdownMode Mode { get; set; }
        public ValueDropdownListMode ListMode { get; set; }
        public bool IsUniqueList { get; set; }
        public int SearchThreshold { get; set; } = 10;
        public string DropdownTitle { get; set; }
        public bool FlattenTreeView { get; set; }
    }

    public enum ValueDropdownMode
    {
        Replace,
        Append,
        AppendReadOnly,
    }

    public enum ValueDropdownListMode
    {
        ElementsAndAdd,
        ElementsOnly,
        AddOnly,
    }

    /// <summary>Optional context for a provider accepting a single ValueDropdownContext argument.</summary>
    public readonly struct ValueDropdownContext
    {
        public ValueDropdownContext(object root, object owner, object currentValue, int index, bool isAdding)
        {
            Root = root;
            Owner = owner;
            CurrentValue = currentValue;
            Index = index;
            IsAdding = isAdding;
        }

        /// <summary>The serialized root, or null when drawing a standalone reflected value.</summary>
        public object Root { get; }
        public object Owner { get; }
        public object CurrentValue { get; }
        public int Index { get; }
        public bool IsAdding { get; }
    }

    /// <summary>Untyped interoperability for providers containing differently typed named items.</summary>
    public interface IValueDropdownItem
    {
        string Text { get; }
        object Value { get; }
        string Tooltip { get; }
        bool Enabled { get; }
    }

    public readonly struct ValueDropdownItem<T> : IValueDropdownItem
    {
        public ValueDropdownItem(string text, T value, string tooltip = null, bool enabled = true)
        {
            Text = text;
            Value = value;
            Tooltip = tooltip;
            Enabled = enabled;
        }

        public string Text { get; }
        public T Value { get; }
        public string Tooltip { get; }
        public bool Enabled { get; }
        object IValueDropdownItem.Value => Value;
        public override string ToString() => Text ?? Value?.ToString() ?? "(Null)";
    }

    /// <summary>Named choices with optional equality and per-selection value creation.</summary>
    public sealed class ValueDropdownList<T> : List<ValueDropdownItem<T>>
    {
        public ValueDropdownList() { }
        public ValueDropdownList(int capacity) : base(capacity) { }
        public ValueDropdownList(IEnumerable<ValueDropdownItem<T>> items) : base(items) { }

        public IEqualityComparer<T> Comparer { get; set; }

        /// <summary>Invoked once per selected value and destination, only when committing a selection.</summary>
        public Func<T, T> ValueFactory { get; set; }

        public void Add(string text, T value) => Add(new ValueDropdownItem<T>(text, value));
        public void Add(string text, T value, string tooltip, bool enabled = true) =>
            Add(new ValueDropdownItem<T>(text, value, tooltip, enabled));
    }
}
