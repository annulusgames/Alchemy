using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.Editor
{
    internal static class EditorTestUtility
    {
        sealed class TestWindow : EditorWindow { }

        public static EditorWindow ShowInWindow(VisualElement content)
        {
            var window = ScriptableObject.CreateInstance<TestWindow>();
            window.position = new Rect(0f, 0f, 640f, 480f);
            window.rootVisualElement.Add(content);
            window.Show();
            return window;
        }

        public static T QueryRequired<T>(VisualElement root) where T : VisualElement
        {
            var element = root.Q<T>();
            Assert.That(
                element,
                Is.Not.Null,
                $"Expected a {typeof(T).Name} in {root.GetType().Name}.");
            return element;
        }

        public static T QueryRequired<T>(VisualElement root, Func<T, bool> predicate) where T : VisualElement
        {
            var element = root.Query<T>().ToList().FirstOrDefault(predicate);
            Assert.That(
                element,
                Is.Not.Null,
                $"Expected a matching {typeof(T).Name} in {root.GetType().Name}.");
            return element;
        }

        public static void Click(Button button)
        {
            button.Focus();
            using var submitEvent = NavigationSubmitEvent.GetPooled();
            submitEvent.target = button;
            button.SendEvent(submitEvent);
        }
    }
}
