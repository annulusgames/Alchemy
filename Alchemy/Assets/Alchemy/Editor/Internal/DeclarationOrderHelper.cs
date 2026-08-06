using System;
using System.Collections.Generic;
using System.Reflection;

namespace Alchemy.Editor
{
    /// <summary>
    /// Resolves a stable declaration ordinal across fields, properties, and methods
    /// using Reflection. Member kinds do not interleave as in source: fields before
    /// properties before methods. Within a kind, tokens follow
    /// declaration order. Base-class members precede derived members.
    /// </summary>
    internal static class DeclarationOrderHelper
    {
        public static int GetOrdinal(MemberInfo member, Type targetType)
        {
            if (member == null) return int.MaxValue;

            var declaringType = member.DeclaringType ?? targetType;
            var hierarchyIndex = GetHierarchyIndex(targetType, declaringType);
            var kindRank = member switch
            {
                FieldInfo => 0,
                PropertyInfo => 1,
                MethodInfo => 2,
                _ => 3
            };
            return hierarchyIndex * 1_000_000_000 + kindRank * 10_000_000 + (member.MetadataToken & 0x00FFFFFF);
        }

        static int GetHierarchyIndex(Type targetType, Type declaringType)
        {
            var chain = new List<Type>();
            for (var type = targetType; type != null; type = type.BaseType)
            {
                chain.Add(type);
            }

            chain.Reverse();
            var index = chain.IndexOf(declaringType);
            return index >= 0 ? index : 0;
        }
    }
}
