using System;
using System.Collections.Generic;
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
        static Dictionary<Type, Type> groupDrawerTypes;

        /// <summary>
        /// Finds the type of drawer that corresponds to PropertyGroupAttribute.
        /// </summary>
        public static Type FindGroupDrawerType(PropertyGroupAttribute attribute)
        {
            groupDrawerTypes ??= CreateGroupDrawerTypes();
            groupDrawerTypes.TryGetValue(attribute.GetType(), out var drawerType);
            return drawerType;
        }

        static Dictionary<Type, Type> CreateGroupDrawerTypes()
        {
            var drawerTypes = new Dictionary<Type, Type>();
            foreach (var drawerType in TypeCache.GetTypesWithAttribute<CustomGroupDrawerAttribute>())
            {
                var targetAttributeType = drawerType.GetCustomAttribute<CustomGroupDrawerAttribute>().targetAttributeType;
                // TypeCache order matches the previous FirstOrDefault scan; the first drawer wins.
                if (targetAttributeType == null || drawerTypes.ContainsKey(targetAttributeType)) continue;
                drawerTypes.Add(targetAttributeType, drawerType);
            }

            return drawerTypes;
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
