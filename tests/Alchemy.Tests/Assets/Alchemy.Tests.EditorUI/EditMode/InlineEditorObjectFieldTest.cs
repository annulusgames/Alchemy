using System;
using System.Collections;
using System.Reflection;
using Alchemy.Editor.Elements;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class InlineEditorObjectFieldTest
    {
        class InlineTarget : ScriptableObject
        {
            public int value;
        }

        class InlineHost : ScriptableObject
        {
            public InlineTarget sample;
        }

        InlineHost host;
        InlineTarget first;
        InlineTarget second;
        SerializedObject serializedObject;
        EditorWindow window;

        [SetUp]
        public void SetUp()
        {
            host = ScriptableObject.CreateInstance<InlineHost>();
            first = ScriptableObject.CreateInstance<InlineTarget>();
            second = ScriptableObject.CreateInstance<InlineTarget>();
            first.value = 11;
            second.value = 22;
            host.sample = first;
            serializedObject = new SerializedObject(host);
        }

        [TearDown]
        public void TearDown()
        {
            if (window != null) UnityEngine.Object.DestroyImmediate(window);
            serializedObject?.Dispose();
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            if (first != null) UnityEngine.Object.DestroyImmediate(first);
            if (second != null) UnityEngine.Object.DestroyImmediate(second);
        }

        [UnityTest]
        public IEnumerator Build_KeepsSerializedObjectForTheSameReferenceAndDisposesOnChange()
        {
            var element = Show();
            yield return null;

            var property = serializedObject.FindProperty(nameof(InlineHost.sample));
            var inlineObject = InlineSerializedObject(element);
            var content = InlineContent(element);
            Assert.That(inlineObject.targetObject, Is.EqualTo(first));
            Assert.That(inlineObject.FindProperty(nameof(InlineTarget.value)).intValue, Is.EqualTo(11));

            Notify(element, property);

            Assert.That(InlineSerializedObject(element), Is.SameAs(inlineObject));
            Assert.That(IsDisposed(inlineObject), Is.False);
            Assert.That(InlineContent(element), Is.SameAs(content));

            AssignReference(element, property, second);
            LogAssert.NoUnexpectedReceived();

            Assert.That(IsDisposed(inlineObject), Is.True);
            var replaced = InlineSerializedObject(element);
            Assert.That(replaced, Is.Not.SameAs(inlineObject));
            Assert.That(replaced.targetObject, Is.EqualTo(second));
            Assert.That(replaced.FindProperty(nameof(InlineTarget.value)).intValue, Is.EqualTo(22));
            Assert.That(InlineContent(element), Is.Not.SameAs(content));
            Assert.That(element.IsObjectNull, Is.False);

            AssignReference(element, property, null);
            LogAssert.NoUnexpectedReceived();

            Assert.That(IsDisposed(replaced), Is.True);
            Assert.That(InlineSerializedObject(element), Is.Null);
            Assert.That(element.IsObjectNull, Is.True);
            Assert.That(element.Q<Foldout>().contentContainer.childCount, Is.EqualTo(0));
            Assert.That(element.Q<Foldout>().Q<Toggle>().style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator Detach_DisposesSerializedObjectAndReattachRebuilds()
        {
            var element = Show();
            yield return null;

            var inlineObject = InlineSerializedObject(element);
            Assert.That(inlineObject.targetObject, Is.EqualTo(first));
            var content = InlineContent(element);

            element.RemoveFromHierarchy();
            LogAssert.NoUnexpectedReceived();

            Assert.That(IsDisposed(inlineObject), Is.True);
            Assert.That(InlineSerializedObject(element), Is.Null);

            window.rootVisualElement.Add(element);
            yield return null;

            var rebuilt = InlineSerializedObject(element);
            Assert.That(rebuilt, Is.Not.Null);
            Assert.That(IsDisposed(rebuilt), Is.False);
            Assert.That(rebuilt.targetObject, Is.EqualTo(first));
            Assert.That(rebuilt.FindProperty(nameof(InlineTarget.value)).intValue, Is.EqualTo(11));
            Assert.That(InlineContent(element), Is.Not.SameAs(content));
            Assert.That(RootField(element).value, Is.EqualTo(first));
            Assert.That(element.Q<Foldout>().Q<Toggle>().style.display.value, Is.EqualTo(DisplayStyle.Flex));
            LogAssert.NoUnexpectedReceived();
        }

        InlineEditorObjectField Show()
        {
            var property = serializedObject.FindProperty(nameof(InlineHost.sample));
            property.isExpanded = true;
            var element = new InlineEditorObjectField(property, typeof(InlineTarget));
            window = EditModeEditorTestUtility.ShowInWindow(element);
            return element;
        }

        static VisualElement InlineContent(InlineEditorObjectField element) =>
            element.Q<Foldout>().contentContainer[0];

        static ObjectField RootField(InlineEditorObjectField element)
        {
            foreach (var child in element.Children())
            {
                if (child is ObjectField objectField) return objectField;
            }

            Assert.Fail("Inline editor root ObjectField was not created.");
            return null;
        }

        static void AssignReference(InlineEditorObjectField element, SerializedProperty property, UnityEngine.Object value)
        {
            RootField(element).SetValueWithoutNotify(value);
            property.objectReferenceValue = value;
            property.serializedObject.ApplyModifiedProperties();
            Notify(element, property);
        }

        static void Notify(InlineEditorObjectField element, SerializedProperty property)
        {
            var method = typeof(InlineEditorObjectField).GetMethod(
                "OnPropertyChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(element, new object[] { property });
        }

        static SerializedObject InlineSerializedObject(InlineEditorObjectField element)
        {
            var field = typeof(InlineEditorObjectField).GetField(
                "inlineSerializedObject",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (SerializedObject)field.GetValue(element);
        }

        static bool IsDisposed(SerializedObject serializedObject)
        {
            var native = typeof(SerializedObject).GetField(
                "m_NativeObjectPtr",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(native, Is.Not.Null);
            return (IntPtr)native.GetValue(serializedObject) == IntPtr.Zero;
        }
    }
}
