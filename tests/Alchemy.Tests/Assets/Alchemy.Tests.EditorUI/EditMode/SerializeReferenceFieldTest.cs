using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Alchemy.Editor;
using Alchemy.Editor.Elements;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class SerializeReferenceFieldTest
    {
        SerializeReferenceFieldHost host;
        SerializedObject serializedObject;
        EditorWindow window;

        [TearDown]
        public void TearDown()
        {
            CloseWindow();
            DisposeSerializedObject();
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            host = null;
        }

        [UnityTest]
        public IEnumerator Reattach_StillUpdatesButtonWidth()
        {
            host = ScriptableObject.CreateInstance<SerializeReferenceFieldHost>();
            serializedObject = new SerializedObject(host);
            var field = new SerializeReferenceField(serializedObject.FindProperty(nameof(SerializeReferenceFieldHost.node)));
            window = EditModeEditorTestUtility.ShowInWindow(field);
            try
            {
                var visualTree = field.panel.visualTree;
                var button = field.buttonContainer;
                window.position = new Rect(0f, 0f, 800f, 480f);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    Mathf.Abs(visualTree.resolvedStyle.width - 800f) < 1f &&
                    WidthMatches(button, visualTree)))
                    yield return wait;

                var originalWidth = button.resolvedStyle.width;

                field.RemoveFromHierarchy();
                var detachedWidth = button.style.width.value.value;
                window.position = new Rect(0f, 0f, 500f, 480f);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    Mathf.Abs(visualTree.resolvedStyle.width - 500f) < 1f))
                    yield return wait;

                Assert.That(button.style.width.value.value, Is.EqualTo(detachedWidth).Within(1f));

                window.rootVisualElement.Add(field);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    button.resolvedStyle.width < originalWidth &&
                    WidthMatches(button, field.panel.visualTree)))
                    yield return wait;

                Assert.That(button.resolvedStyle.width, Is.LessThan(originalWidth));
                Assert.That(button.resolvedStyle.width,
                    Is.EqualTo(ExpectedWidth(button, field.panel.visualTree)).Within(1f));
            }
            finally
            {
                // Detach before dispose so the button's IMGUI cannot run against a disposed property.
                field.RemoveFromHierarchy();
                CloseWindow();
                DisposeSerializedObject();
            }
        }

        [UnityTest]
        public IEnumerator Detach_DoesNotKeepFieldAlive()
        {
            window = EditModeEditorTestUtility.ShowInWindow(new VisualElement());
            try
            {
                var weak = CreateDetachedField(window.rootVisualElement);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    return !weak.IsAlive;
                }))
                    yield return wait;

                Assert.That(weak.IsAlive, Is.False);
            }
            finally
            {
                CloseWindow();
                // The field is already detached and, on success, collected. Dispose only after that
                // check so the SerializedObject finalizer cannot run while the field still exists.
                DisposeSerializedObject();
            }
        }

        static float ExpectedWidth(VisualElement button, VisualElement visualTree)
        {
            return GUIHelper.CalculateFieldWidth(button, visualTree) -
                (button.GetFirstAncestorOfType<Foldout>() != null ? 18f : 0f);
        }

        static bool WidthMatches(VisualElement button, VisualElement visualTree)
        {
            return Mathf.Abs(button.resolvedStyle.width - ExpectedWidth(button, visualTree)) < 1f;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        WeakReference CreateDetachedField(VisualElement root)
        {
            host = ScriptableObject.CreateInstance<SerializeReferenceFieldHost>();
            serializedObject = new SerializedObject(host);
            var field = new SerializeReferenceField(serializedObject.FindProperty(nameof(SerializeReferenceFieldHost.node)));
            root.Add(field);
            field.RemoveFromHierarchy();
            return new WeakReference(field);
        }

        void CloseWindow()
        {
            if (window == null) return;
            window.rootVisualElement.Clear();
            window.Close();
            if (window != null) UnityEngine.Object.DestroyImmediate(window);
            window = null;
        }

        void DisposeSerializedObject()
        {
            if (serializedObject == null) return;
            serializedObject.Dispose();
            serializedObject = null;
        }
    }

    public class SerializeReferenceFieldHost : ScriptableObject
    {
        [SerializeReference] public SerializeReferenceFieldNode node;
    }

    [Serializable]
    public class SerializeReferenceFieldNode
    {
        public int value;
    }
}
