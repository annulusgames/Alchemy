using System;
using System.Reflection;

namespace Alchemy.Editor
{
    internal static class MemberInfoExtensions
    {
        public static bool HasCustomAttribute<T>(this MemberInfo memberInfo) where T : Attribute
        {
            return memberInfo.GetCustomAttribute<T>() != null;
        }

        public static bool TryGetCustomAttribute<T>(this MemberInfo memberInfo, out T result) where T : Attribute
        {
            result = memberInfo.GetCustomAttribute<T>();
            return result != null;
        }
    }
}
