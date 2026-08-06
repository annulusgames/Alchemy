using System;

namespace Alchemy.Inspector
{
    /// <summary>
    /// Base class of attributes for creating Group on Inspector
    /// </summary>
    public abstract class PropertyGroupAttribute : Attribute
    {
        public PropertyGroupAttribute(int order = 0)
        {
            GroupPath = string.Empty;
            Order = order;
        }

        public PropertyGroupAttribute(string groupPath, int order = 0)
        {
            GroupPath = groupPath;
            Order = order;
        }

        /// <summary>
        /// Specifies the path of the group. Groups can be nested using `/`.
        /// </summary>
        public string GroupPath { get; }

        /// <summary>
        /// Drawing order of the group.
        /// </summary>
        public int Order { get; }
    }
}
