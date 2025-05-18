namespace Alchemy.Inspector
{
    public sealed class GroupAttribute : PropertyGroupAttribute
    {
        public GroupAttribute(int order = 0) : base(order) { }
        public GroupAttribute(string groupPath, int order = 0) : base(groupPath, order) { }
    }

    public sealed class BoxGroupAttribute : PropertyGroupAttribute
    {
        public BoxGroupAttribute(int order = 0) : base(order) { }
        public BoxGroupAttribute(string groupPath, int order = 0) : base(groupPath, order) { }
    }

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

        public string TabName { get; }
    }

    public sealed class FoldoutGroupAttribute : PropertyGroupAttribute
    {
        public FoldoutGroupAttribute(int order = 0) : base(order) { }
        public FoldoutGroupAttribute(string groupPath, int order = 0) : base(groupPath, order) { }
    }

    public sealed class HorizontalGroupAttribute : PropertyGroupAttribute
    {
        public HorizontalGroupAttribute(int order = 0) : base(order) { }
        public HorizontalGroupAttribute(string groupPath, int order = 0) : base(groupPath, order) { }
    }

    public sealed class InlineGroupAttribute : PropertyGroupAttribute
    {
        public InlineGroupAttribute(int order = 0) : base(order) { }
        public InlineGroupAttribute(string groupPath, int order = 0) : base(groupPath, order) { }
    }
}