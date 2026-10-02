using System;
using System.Collections;
using Alchemy.Editor.Elements;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class LazyFoldoutBuildTest
    {
        EditorWindow window;

        [TearDown]
        public void TearDown() => ReleaseWindow();

        [UnityTest]
        public IEnumerator CollapsedNestedClass_BuildsAndBindsChildrenWhenExpanded()
        {
            var host = ScriptableObject.CreateInstance<NestedClassHost>();
            host.nested ??= new NestedClass();
            var serializedObject = new SerializedObject(host);
            AlchemyPropertyField field = null;
            try
            {
                var property = serializedObject.FindProperty(nameof(NestedClassHost.nested));
                property.isExpanded = false;

                field = new AlchemyPropertyField(property, typeof(NestedClass));
                var foldout = (Foldout)field.FieldElement;
                Assert.That(foldout.contentContainer.childCount, Is.EqualTo(0));

                // Value changes are not dispatched while detached. Attach applies an expanded foldout.
                foldout.value = true;
                Assert.That(foldout.contentContainer.childCount, Is.EqualTo(0));

                window = EditModeEditorTestUtility.ShowInWindow(field);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => foldout.Q<PropertyField>() != null))
                    yield return wait;

                var child = foldout.Q<PropertyField>();
                Assert.That(child.bindingPath, Is.EqualTo(property.propertyPath + ".value"));
                var childCount = foldout.contentContainer.childCount;
                foldout.value = false;
                foldout.value = true;
                Assert.That(foldout.contentContainer.childCount, Is.EqualTo(childCount));

                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => child.Q<IntegerField>() != null))
                    yield return wait;

                foreach (var wait in WaitForPendingBindings())
                    yield return wait;
            }
            finally
            {
                ReleaseBuiltUi(field, serializedObject, host);
            }
        }

        [UnityTest]
        public IEnumerator CollapsedNestedClass_BuildsChildrenAfterSetValueWithoutNotify()
        {
            var host = ScriptableObject.CreateInstance<NestedClassHost>();
            host.nested ??= new NestedClass();
            var serializedObject = new SerializedObject(host);
            AlchemyPropertyField field = null;
            try
            {
                var property = serializedObject.FindProperty(nameof(NestedClassHost.nested));
                property.isExpanded = false;

                field = new AlchemyPropertyField(property, typeof(NestedClass));
                var foldout = (Foldout)field.FieldElement;
                Assert.That(foldout.contentContainer.childCount, Is.EqualTo(0));

                window = EditModeEditorTestUtility.ShowInWindow(field);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => foldout.resolvedStyle.width > 0f))
                    yield return wait;
                Assert.That(foldout.contentContainer.childCount, Is.EqualTo(0));

                foldout.SetValueWithoutNotify(true);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => foldout.contentContainer.childCount > 0))
                    yield return wait;

                var expectedPath = property.propertyPath + ".value";
                foreach (var wait in WaitForPendingBindings())
                    yield return wait;

                var child = foldout.Q<PropertyField>();
                Assert.That(child, Is.Not.Null);
                Assert.That(child.bindingPath, Is.EqualTo(expectedPath));
            }
            finally
            {
                ReleaseBuiltUi(field, serializedObject, host);
            }
        }

        [UnityTest]
        public IEnumerator CollapsedNestedClass_BuildsChildrenWhenBoundPropertyIsExpanded()
        {
            var host = ScriptableObject.CreateInstance<NestedClassHost>();
            host.nested ??= new NestedClass();
            var serializedObject = new SerializedObject(host);
            AlchemyPropertyField field = null;
            try
            {
                var property = serializedObject.FindProperty(nameof(NestedClassHost.nested));
                property.isExpanded = false;

                field = new AlchemyPropertyField(property, typeof(NestedClass));
                var foldout = (Foldout)field.FieldElement;
                Assert.That(foldout.contentContainer.childCount, Is.EqualTo(0));

                window = EditModeEditorTestUtility.ShowInWindow(field);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => foldout.resolvedStyle.width > 0f))
                    yield return wait;
                Assert.That(foldout.value, Is.False);
                Assert.That(foldout.contentContainer.childCount, Is.EqualTo(0));

                // Writing isExpanded on the bound SerializedObject does not refresh the foldout.
                // Expanding the attached foldout is what builds and binds the children.
                foldout.value = true;
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => foldout.contentContainer.childCount > 0))
                    yield return wait;

                var expectedPath = property.propertyPath + ".value";
                foreach (var wait in WaitForPendingBindings())
                    yield return wait;

                var child = foldout.Q<PropertyField>();
                Assert.That(child, Is.Not.Null);
                Assert.That(child.bindingPath, Is.EqualTo(expectedPath));
            }
            finally
            {
                ReleaseBuiltUi(field, serializedObject, host);
            }
        }

        // contentContainer.Bind is applied on a later panel update. 6000.0 skips that
        // update unless the window repaints. Run it before SerializedObject.Dispose.
        IEnumerable WaitForPendingBindings()
        {
            if (window != null) window.Repaint();
            yield return null;
        }

        void ReleaseBuiltUi(VisualElement root, SerializedObject serializedObject, UnityEngine.Object host)
        {
            if (root != null)
            {
                UnbindTree(root);
                root.RemoveFromHierarchy();
            }

            ReleaseWindow();
            serializedObject.Dispose();
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
        }

        void ReleaseWindow()
        {
            if (window == null) return;
            UnbindTree(window.rootVisualElement);
            window.rootVisualElement.Clear();
            window.Close();
            if (window != null) UnityEngine.Object.DestroyImmediate(window);
            window = null;
        }

        static void UnbindTree(VisualElement element)
        {
            if (element == null) return;
            element.Unbind();
            for (var i = 0; i < element.childCount; i++)
                UnbindTree(element[i]);
        }

        [Serializable]
        class NestedClass
        {
            public int value;
        }

        class NestedClassHost : ScriptableObject
        {
            public NestedClass nested = new NestedClass();
        }
    }
}
