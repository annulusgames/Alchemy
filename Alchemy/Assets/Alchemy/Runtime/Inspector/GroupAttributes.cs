namespace Alchemy.Inspector
{
    /// <summary>
    /// Creates a group to display multiple members together.
    /// </summary>
    /// <alchemy-attr-category>Groups</alchemy-attr-category>
    public sealed class GroupAttribute : PropertyGroupAttribute
    {
        public GroupAttribute() { }

        public GroupAttribute(string groupPath) : base(groupPath) { }

        public GroupAttribute(int order) : base(order) { }

        public GroupAttribute(string groupPath, int order) : base(groupPath, order) { }
    }

    /// <summary>
    /// Creates a group that wraps multiple members in a box for display.
    /// </summary>
    /// <alchemy-attr-category>Groups</alchemy-attr-category>
    public sealed class BoxGroupAttribute : PropertyGroupAttribute
    {
        public BoxGroupAttribute() { }

        public BoxGroupAttribute(string groupPath) : base(groupPath) { }

        public BoxGroupAttribute(int order) : base(order) { }

        public BoxGroupAttribute(string groupPath, int order) : base(groupPath, order) { }
    }

    /// <summary>
    /// Creates a group that divides multiple members into tabs.
    /// Order controls this tab group among sibling groups only; tab order follows member declaration order.
    /// </summary>
    /// <alchemy-attr-category>Groups</alchemy-attr-category>
    public sealed class TabGroupAttribute : PropertyGroupAttribute
    {
        public TabGroupAttribute(string tabName)
        {
            TabName = tabName;
        }

        public TabGroupAttribute(string groupPath, string tabName) : base(groupPath)
        {
            TabName = tabName;
        }

        public TabGroupAttribute(string tabName, int order) : base(order)
        {
            TabName = tabName;
        }

        public TabGroupAttribute(string groupPath, string tabName, int order) : base(groupPath, order)
        {
            TabName = tabName;
        }

        /// <summary>
        /// The name of the tab.
        /// </summary>
        public string TabName { get; }
    }

    /// <summary>
    /// Creates collapsible groups for multiple members.
    /// </summary>
    /// <alchemy-attr-category>Groups</alchemy-attr-category>
    public sealed class FoldoutGroupAttribute : PropertyGroupAttribute
    {
        public FoldoutGroupAttribute() { }

        public FoldoutGroupAttribute(string groupPath) : base(groupPath) { }

        public FoldoutGroupAttribute(int order) : base(order) { }

        public FoldoutGroupAttribute(string groupPath, int order) : base(groupPath, order) { }
    }

    /// <summary>
    /// Creates a group that displays multiple members horizontally.
    /// </summary>
    /// <alchemy-attr-category>Groups</alchemy-attr-category>
    public sealed class HorizontalGroupAttribute : PropertyGroupAttribute
    {
        public HorizontalGroupAttribute() { }

        public HorizontalGroupAttribute(string groupPath) : base(groupPath) { }

        public HorizontalGroupAttribute(int order) : base(order) { }

        public HorizontalGroupAttribute(string groupPath, int order) : base(groupPath, order) { }
    }

    /// <summary>
    /// Creates an inline group that displays members without additional visual chrome.
    /// </summary>
    /// <alchemy-attr-category>Groups</alchemy-attr-category>
    public sealed class InlineGroupAttribute : PropertyGroupAttribute
    {
        public InlineGroupAttribute() { }

        public InlineGroupAttribute(string groupPath) : base(groupPath) { }

        public InlineGroupAttribute(int order) : base(order) { }

        public InlineGroupAttribute(string groupPath, int order) : base(groupPath, order) { }
    }
}
