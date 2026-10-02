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
        public void TearDown()
        {
            if (window != null) UnityEngine.Object.DestroyImmediate(window);
        }

        [UnityTest]
        public IEnumerator CollapsedNestedClass_BuildsAndBindsChildrenWhenExpanded()
        {
            var host = ScriptableObject.CreateInstance<NestedClassHost>();
            host.nested ??= new NestedClass();
            var serializedObject = new SerializedObject(host);
            try
            {
                var property = serializedObject.FindProperty(nameof(NestedClassHost.nested));
                property.isExpanded = false;

                var field = new AlchemyPropertyField(property, typeof(NestedClass));
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
            }
            finally
            {
                if (window != null)
                {
                    UnityEngine.Object.DestroyImmediate(window);
                    window = null;
                }

                serializedObject.Dispose();
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [UnityTest]
        public IEnumerator CollapsedNestedClass_BuildsChildrenAfterSetValueWithoutNotify()
        {
            var host = ScriptableObject.CreateInstance<NestedClassHost>();
            host.nested ??= new NestedClass();
            var serializedObject = new SerializedObject(host);
            try
            {
                var property = serializedObject.FindProperty(nameof(NestedClassHost.nested));
                property.isExpanded = false;

                var field = new AlchemyPropertyField(property, typeof(NestedClass));
                var foldout = (Foldout)field.FieldElement;
                Assert.That(foldout.contentContainer.childCount, Is.EqualTo(0));

                window = EditModeEditorTestUtility.ShowInWindow(field);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => foldout.resolvedStyle.width > 0f))
                    yield return wait;
                Assert.That(foldout.contentContainer.childCount, Is.EqualTo(0));

                foldout.SetValueWithoutNotify(true);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => foldout.contentContainer.childCount > 0))
                    yield return wait;

                var child = foldout.Q<PropertyField>();
                Assert.That(child, Is.Not.Null);
                Assert.That(child.bindingPath, Is.EqualTo(property.propertyPath + ".value"));
            }
            finally
            {
                if (window != null)
                {
                    UnityEngine.Object.DestroyImmediate(window);
                    window = null;
                }

                serializedObject.Dispose();
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [UnityTest]
        public IEnumerator CollapsedNestedClass_BuildsChildrenWhenBoundPropertyIsExpanded()
        {
            var host = ScriptableObject.CreateInstance<NestedClassHost>();
            host.nested ??= new NestedClass();
            var serializedObject = new SerializedObject(host);
            try
            {
                var property = serializedObject.FindProperty(nameof(NestedClassHost.nested));
                property.isExpanded = false;

                var field = new AlchemyPropertyField(property, typeof(NestedClass));
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

                var child = foldout.Q<PropertyField>();
                Assert.That(child, Is.Not.Null);
                Assert.That(child.bindingPath, Is.EqualTo(property.propertyPath + ".value"));
            }
            finally
            {
                if (window != null)
                {
                    UnityEngine.Object.DestroyImmediate(window);
                    window = null;
                }

                serializedObject.Dispose();
                UnityEngine.Object.DestroyImmediate(host);
            }
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
