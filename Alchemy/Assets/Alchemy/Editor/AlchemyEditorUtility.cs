using System;
using System.Linq;
using System.Reflection;
using Alchemy.Inspector;
using UnityEditor;

namespace Alchemy.Editor
{
    /// <summary>
    /// Alchemy Editor utility functions.
    /// </summary>
    public static class AlchemyEditorUtility
    {
        /// <summary>
        /// Finds the type of drawer that corresponds to PropertyGroupAttribute.
        /// </summary>
        public static Type FindGroupDrawerType(PropertyGroupAttribute attribute)
        {
            return TypeCache.GetTypesWithAttribute<CustomGroupDrawerAttribute>()
                .FirstOrDefault(x => x.GetCustomAttribute<CustomGroupDrawerAttribute>().targetAttributeType == attribute.GetType());
        }

        internal static AlchemyGroupDrawer CreateGroupDrawer(PropertyGroupAttribute attribute, Type targetType, string groupPath = null)
        {
            var drawerType = FindGroupDrawerType(attribute);
            var drawer = (AlchemyGroupDrawer)Activator.CreateInstance(drawerType);
            // Use the concrete node path (not always attribute.GroupPath) so nested
            // intermediates like "A" from "A/B" do not share state keys with "B".
            drawer.SetUniqueId("AlchemyGroupId_" + targetType.FullName + "_" + (groupPath ?? attribute.GroupPath));
            return drawer;
        }
    }
}
