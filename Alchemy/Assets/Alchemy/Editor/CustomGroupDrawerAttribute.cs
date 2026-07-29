using System;

namespace Alchemy.Editor
{
    /// <summary>
    /// Attribute for specifying the target attribute of AlchemyGroupDrawer.
    /// </summary>
    public sealed class CustomGroupDrawerAttribute : Attribute
    {
        public CustomGroupDrawerAttribute(Type targetAttributeType)
        {
            this.targetAttributeType = targetAttributeType;
        }

        public readonly Type targetAttributeType;
    }
}
