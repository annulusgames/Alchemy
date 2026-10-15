using System;

namespace Alchemy.Editor
{
    /// <summary>
    /// Attribute for specifying the target attribute of AlchemyAttributeDrawer.
    /// </summary>
    public sealed class CustomAttributeDrawerAttribute : Attribute
    {
        public CustomAttributeDrawerAttribute(Type targetAttributeType, int order = 0)
        {
            this.targetAttributeType = targetAttributeType;
            this.order = order;
        }

        public readonly Type targetAttributeType;
        /// <summary>
        /// Reserved for future use. Currently not referenced when resolving drawers; the first registered drawer for an attribute type is used.
        /// </summary>
        public readonly int order;
    }
}
