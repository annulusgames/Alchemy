using System;

namespace Alchemy.Inspector
{
    /// <summary>
    /// Base class of attributes for creating Group on Inspector
    /// </summary>
    public abstract class PropertyGroupAttribute : Attribute
    {
        protected PropertyGroupAttribute()
        {
            GroupPath = string.Empty;
        }

        protected PropertyGroupAttribute(string groupPath)
        {
            GroupPath = groupPath;
        }

        protected PropertyGroupAttribute(int order)
        {
            GroupPath = string.Empty;
            Order = order;
            HasDefinedOrder = true;
        }

        protected PropertyGroupAttribute(string groupPath, int order)
        {
            GroupPath = groupPath;
            Order = order;
            HasDefinedOrder = true;
        }

        /// <summary>
        /// Specifies the path of the group. Groups can be nested using `/`.
        /// </summary>
        public string GroupPath { get; }

        /// <summary>
        /// Drawing order among sibling groups and ungrouped members. Uses the same scale as <c>OrderAttribute</c>.
        /// Lower values are drawn first. Groups without an explicit order default to 0 (same as unordered members);
        /// ties are broken by declaration order.
        /// For nested paths (for example <c>A/B</c>), the order applies only to the leaf group (<c>B</c>).
        /// </summary>
        public int Order { get; }

        public bool HasDefinedOrder { get; }
    }
}
