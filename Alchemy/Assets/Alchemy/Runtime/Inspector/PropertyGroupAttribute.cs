using System;

namespace Alchemy.Inspector
{
    /// <summary>
    /// Base class of attributes for creating Group on Inspector
    /// </summary>
    public abstract class PropertyGroupAttribute : Attribute
    {
        public PropertyGroupAttribute()
        {
            GroupPath = string.Empty;
        }

        public PropertyGroupAttribute(string groupPath)
        {
            GroupPath = groupPath;
        }

        public string GroupPath { get; }
    }
}
