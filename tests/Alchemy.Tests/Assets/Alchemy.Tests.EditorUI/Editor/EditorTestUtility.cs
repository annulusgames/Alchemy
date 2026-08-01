using System.Reflection;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.Editor
{
    internal static class EditorTestUtility
    {
        public static T QueryRequired<T>(VisualElement root) where T : VisualElement
        {
            var element = root.Q<T>();
            Assert.That(
                element,
                Is.Not.Null,
                $"Expected a {typeof(T).Name} in {root.GetType().Name}.");
            return element;
        }

        public static object InvokeNonPublicMethod(object target, string methodName, params object[] arguments)
        {
            var method = target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(
                method,
                Is.Not.Null,
                $"Expected {target.GetType().Name}.{methodName} to exist.");
            return method.Invoke(target, arguments);
        }
    }
}
