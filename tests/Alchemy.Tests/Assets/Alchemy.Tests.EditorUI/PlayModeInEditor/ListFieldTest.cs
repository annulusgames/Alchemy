using System.Collections;
using System.Reflection;
using Alchemy.Editor.Elements;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.PlayModeInEditor
{
    public class ListFieldTest
    {
        [UnityTest]
        public IEnumerator Test_ArrayCanAddAndRemoveElements()
        {
            var gameObject = new GameObject(nameof(ArrayTest));
            var target = gameObject.AddComponent<ArrayTest>();
            var fieldInfo = typeof(ArrayTest).GetField(nameof(ArrayTest.array));
            Assert.That(fieldInfo, Is.Not.Null);

            var reflectionField = new ReflectionField(target, fieldInfo);
            var window = EditorTestUtility.ShowInWindow(reflectionField);
            try
            {
                yield return null;

                var listView = EditorTestUtility.QueryRequired<ListView>(reflectionField);
                var addButton = EditorTestUtility.QueryRequired<Button>(
                    listView,
                    button => button.name == BaseListView.footerAddButtonName);
                EditorTestUtility.Click(addButton);
                yield return null;

                CollectionAssert.AreEqual(new[] { 10, 20, 0 }, target.array);
                Assert.That(listView.itemsSource.Count, Is.EqualTo(3));

                RemoveItem(listView, 1);
                yield return null;

                CollectionAssert.AreEqual(new[] { 10, 0 }, target.array);
                Assert.That(listView.itemsSource.Count, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        static void RemoveItem(ListView listView, int index)
        {
            var viewControllerProperty = typeof(BaseListView).GetProperty(
                "viewController",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);
            Assert.That(viewControllerProperty, Is.Not.Null);

            var viewController = viewControllerProperty.GetValue(listView);
            Assert.That(viewController, Is.Not.Null);

            var removeItemMethod = viewController.GetType().GetMethod(
                "RemoveItem",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(removeItemMethod, Is.Not.Null);
            removeItemMethod.Invoke(viewController, new object[] { index });
        }

        [UnityTest]
        public IEnumerator Test_NullArrayCanBeCreated()
        {
            var gameObject = new GameObject(nameof(ArrayTest));
            var target = gameObject.AddComponent<ArrayTest>();
            var fieldInfo = typeof(ArrayTest).GetField(nameof(ArrayTest.nullArray));
            Assert.That(fieldInfo, Is.Not.Null);

            var reflectionField = new ReflectionField(target, fieldInfo);
            var window = EditorTestUtility.ShowInWindow(reflectionField);
            try
            {
                yield return null;

                var createButton = EditorTestUtility.QueryRequired<Button>(
                    reflectionField,
                    button => button.text?.Contains("Create") == true);
                EditorTestUtility.Click(createButton);
                yield return null;

                Assert.That(target.nullArray, Is.Not.Null);
                Assert.That(target.nullArray, Is.Empty);
                EditorTestUtility.QueryRequired<ListField>(reflectionField);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
