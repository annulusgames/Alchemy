using System;
using System.Collections.Generic;
using System.Linq;
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
        public static (MemberInfo Member, int DeclaredAt)[] OrderMembers(Type targetType, IEnumerable<MemberInfo> members)
        {
            var hierarchyOrder = GetHierarchy(targetType)
                .Select((type, index) => (type, index))
                .ToDictionary(x => x.type, x => x.index);

            return members
                .Where(x => x is MethodInfo or FieldInfo or PropertyInfo)
                .OrderBy(x => hierarchyOrder.TryGetValue(x.DeclaringType ?? targetType, out var index) ? index : 0)
                .ThenBy(GetMemberKindRank)
                .ThenBy(x => x.MetadataToken)
                .Select((member, declaredAt) => (member, declaredAt))
                .ToArray();
        }

        static IEnumerable<Type> GetHierarchy(Type targetType)
        {
            var chain = new List<Type>();
            for (var type = targetType; type != null; type = type.BaseType)
            {
                chain.Add(type);
            }

            chain.Reverse();
            return chain;
        }

        static int GetMemberKindRank(MemberInfo member) => member switch
        {
            FieldInfo => 0,
            PropertyInfo => 1,
            MethodInfo => 2,
            _ => 3
        };
    }
}
