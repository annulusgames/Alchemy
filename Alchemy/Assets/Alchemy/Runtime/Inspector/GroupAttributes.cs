namespace Alchemy.Inspector
{
    /// <summary>
    /// Creates a group to display multiple members together.
    /// </summary>
    public sealed class GroupAttribute : PropertyGroupAttribute
    {
        public GroupAttribute() : base() { }
        public GroupAttribute(string groupPath) : base(groupPath) { }
    }

    /// <summary>
    /// Creates a group that wraps multiple members in a box for display.
    /// </summary>
    public sealed class BoxGroupAttribute : PropertyGroupAttribute
    {
        public BoxGroupAttribute() : base() { }
        public BoxGroupAttribute(string groupPath) : base(groupPath) { }
    }

    /// <summary>
    /// Creates a group that divides multiple members into tabs.
    /// </summary>
    public sealed class TabGroupAttribute : PropertyGroupAttribute
    {
        public TabGroupAttribute(string tabName) : base()
        {
            TabName = tabName;
        }

        public TabGroupAttribute(string groupPath, string tabName) : base(groupPath)
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
        public FoldoutGroupAttribute() : base() { }
        public FoldoutGroupAttribute(string groupPath) : base(groupPath) { }
    }

    /// <summary>
    /// Creates a group that displays multiple members horizontally.
    /// </summary>
    public sealed class HorizontalGroupAttribute : PropertyGroupAttribute
    {
        public HorizontalGroupAttribute() : base() { }
        public HorizontalGroupAttribute(string groupPath) : base(groupPath) { }
    }

    /// <summary>
    /// Creates an inline group that displays members without additional visual chrome.
    /// </summary>
    public sealed class InlineGroupAttribute : PropertyGroupAttribute
    {
        public InlineGroupAttribute() : base() { }
        public InlineGroupAttribute(string groupPath) : base(groupPath) { }
    }
}
