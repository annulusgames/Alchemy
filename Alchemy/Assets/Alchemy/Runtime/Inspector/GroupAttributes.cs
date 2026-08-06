namespace Alchemy.Inspector
{
    /// <summary>
    /// Creates a group to display multiple members together.
    /// </summary>
    public sealed class GroupAttribute : PropertyGroupAttribute
    {
        public GroupAttribute(int order = 0) : base(order) { }
        public GroupAttribute(string groupPath, int order = 0) : base(groupPath, order) { }
    }

    /// <summary>
    /// Creates a group that wraps multiple members in a box for display.
    /// </summary>
    public sealed class BoxGroupAttribute : PropertyGroupAttribute
    {
        public BoxGroupAttribute(int order = 0) : base(order) { }
        public BoxGroupAttribute(string groupPath, int order = 0) : base(groupPath, order) { }
    }

    /// <summary>
    /// Creates a group that divides multiple members into tabs.
    /// </summary>
    public sealed class TabGroupAttribute : PropertyGroupAttribute
    {
        public TabGroupAttribute(string tabName, int order = 0) : base(order)
        {
            TabName = tabName;
        }

        public TabGroupAttribute(string groupPath, string tabName, int order = 0) : base(groupPath, order)
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
    public sealed class FoldoutGroupAttribute : PropertyGroupAttribute
    {
        public FoldoutGroupAttribute(int order = 0) : base(order) { }
        public FoldoutGroupAttribute(string groupPath, int order = 0) : base(groupPath, order) { }
    }

    /// <summary>
    /// Creates a group that displays multiple members horizontally.
    /// </summary>
    public sealed class HorizontalGroupAttribute : PropertyGroupAttribute
    {
        public HorizontalGroupAttribute(int order = 0) : base(order) { }
        public HorizontalGroupAttribute(string groupPath, int order = 0) : base(groupPath, order) { }
    }

    /// <summary>
    /// Creates an inline group that displays members without additional visual chrome.
    /// </summary>
    public sealed class InlineGroupAttribute : PropertyGroupAttribute
    {
        public InlineGroupAttribute(int order = 0) : base(order) { }
        public InlineGroupAttribute(string groupPath, int order = 0) : base(groupPath, order) { }
    }
}
