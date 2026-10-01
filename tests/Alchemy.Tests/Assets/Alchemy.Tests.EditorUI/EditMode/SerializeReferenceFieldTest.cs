using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Alchemy.Editor;
using Alchemy.Editor.Elements;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
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

    public class SerializeReferenceTypeCacheTest
    {
        [Test]
        public void GetManagedReferenceFieldType_CachesResolvedTypeByFieldTypeName()
        {
            var host = ScriptableObject.CreateInstance<SerializeReferenceTypeCacheHost>();
            try
            {
                var serializedObject = new SerializedObject(host);
                try
                {
                    var node = serializedObject.FindProperty(nameof(SerializeReferenceTypeCacheHost.node));
                    var abstractNode = serializedObject.FindProperty(nameof(SerializeReferenceTypeCacheHost.abstractNode));
                    var cache = ManagedReferenceFieldTypeCache();

                    var nodeType = node.GetManagedReferenceFieldType();
                    var abstractType = abstractNode.GetManagedReferenceFieldType();

                    Assert.That(nodeType, Is.EqualTo(typeof(SerializeReferenceFieldNode)));
                    Assert.That(abstractType, Is.EqualTo(typeof(SerializeReferenceAbstractNode)));
                    Assert.That(node.managedReferenceFieldTypename, Is.Not.EqualTo(abstractNode.managedReferenceFieldTypename));
                    Assert.That(cache[node.managedReferenceFieldTypename], Is.SameAs(nodeType));
                    Assert.That(cache[abstractNode.managedReferenceFieldTypename], Is.SameAs(abstractType));
                    Assert.That(node.GetManagedReferenceFieldType(), Is.SameAs(nodeType));
                    Assert.That(abstractNode.GetManagedReferenceFieldType(), Is.SameAs(abstractType));
                }
                finally
                {
                    serializedObject.Dispose();
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void GetCandidateTypes_CachesFilteredTypesInFullNameOrder()
        {
            var baseType = typeof(SerializeReferenceAbstractNode);
            var actual = SerializeReferenceDropdown.GetCandidateTypes(baseType);

            CollectionAssert.AreEqual(UncachedCandidates(baseType), actual);
            Assert.That(SerializeReferenceDropdown.GetCandidateTypes(baseType), Is.SameAs(actual));
            Assert.That(actual, Has.Member(typeof(SerializeReferenceConcreteNode)));
            Assert.That(actual, Has.Member(typeof(SerializeReferenceOuterNode.Nested)));
            Assert.That(actual, Has.No.Member(typeof(SerializeReferenceAbstractNode)));
            Assert.That(actual, Has.No.Member(typeof(SerializeReferenceGenericNode<>)));
            Assert.That(actual, Has.No.Member(typeof(SerializeReferenceHiddenNode)));
            Assert.That(actual, Has.No.Member(typeof(SerializeReferenceNonSerializedNode)));

            var nodeTypes = SerializeReferenceDropdown.GetCandidateTypes(typeof(SerializeReferenceFieldNode));
            Assert.That(nodeTypes, Is.Not.SameAs(actual));
            CollectionAssert.AreEqual(UncachedCandidates(typeof(SerializeReferenceFieldNode)), nodeTypes);
            Assert.That(nodeTypes, Has.Member(typeof(SerializeReferenceFieldNode)));

            var objectTypes = SerializeReferenceDropdown.GetCandidateTypes(typeof(SerializeReferenceObjectNode));
            Assert.That(objectTypes, Is.Empty);
            Assert.That(SerializeReferenceDropdown.GetCandidateTypes(typeof(SerializeReferenceObjectNode)), Is.SameAs(objectTypes));
        }

        [Test]
        public void SetSortedTypes_StoresCachedArrayWithoutCopying()
        {
            var cached = SerializeReferenceDropdown.GetCandidateTypes(typeof(SerializeReferenceAbstractNode));
            var dropdown = new SerializeReferenceDropdown(13, new AdvancedDropdownState());
            dropdown.SetSortedTypes(cached);

            Assert.That(StoredTypes(dropdown), Is.SameAs(cached));
            CollectionAssert.AreEqual(UncachedCandidates(typeof(SerializeReferenceAbstractNode)), cached);
        }

        [Test]
        public void SetTypes_SortsByFullNameIntoANewArray()
        {
            var unsorted = new[]
            {
                typeof(SerializeReferenceOuterNode.Nested),
                typeof(SerializeReferenceConcreteNode),
                typeof(SerializeReferenceFieldNode),
            };
            var dropdown = new SerializeReferenceDropdown(unsorted, 13, new AdvancedDropdownState());

            var stored = StoredTypes(dropdown);
            CollectionAssert.AreEqual(unsorted.OrderBy(t => t.FullName).ToArray(), stored);
            Assert.That(stored, Is.Not.SameAs(unsorted));
        }

        static Type[] UncachedCandidates(Type baseType)
        {
            return TypeCache.GetTypesDerivedFrom(baseType).Append(baseType).Where(t =>
                (t.IsPublic || t.IsNestedPublic) &&
                !t.IsAbstract &&
                !t.IsGenericType &&
                !typeof(UnityEngine.Object).IsAssignableFrom(t) &&
                t.IsSerializable
            ).OrderBy(x => x.FullName).ToArray();
        }

        static Type[] StoredTypes(SerializeReferenceDropdown dropdown)
        {
            var field = typeof(SerializeReferenceDropdown).GetField(
                "types",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (Type[])field.GetValue(dropdown);
        }

        static Dictionary<string, Type> ManagedReferenceFieldTypeCache()
        {
            var field = typeof(SerializedPropertyExtensions).GetField(
                "managedReferenceFieldTypes",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (Dictionary<string, Type>)field.GetValue(null);
        }
    }

    public class SerializeReferenceFieldHost : ScriptableObject
    {
        [SerializeReference] public SerializeReferenceFieldNode node;
    }

    public class SerializeReferenceTypeCacheHost : ScriptableObject
    {
        [SerializeReference] public SerializeReferenceFieldNode node;
        [SerializeReference] public SerializeReferenceAbstractNode abstractNode;
    }

    [Serializable]
    public abstract class SerializeReferenceAbstractNode
    {
        public int value;
    }

    [Serializable]
    public class SerializeReferenceConcreteNode : SerializeReferenceAbstractNode
    {
    }

    [Serializable]
    public class SerializeReferenceOuterNode
    {
        [Serializable]
        public class Nested : SerializeReferenceAbstractNode
        {
        }
    }

    [Serializable]
    public class SerializeReferenceGenericNode<T> : SerializeReferenceAbstractNode
    {
    }

    [Serializable]
    class SerializeReferenceHiddenNode : SerializeReferenceAbstractNode
    {
    }

    public class SerializeReferenceNonSerializedNode : SerializeReferenceAbstractNode
    {
    }

    public class SerializeReferenceObjectNode : ScriptableObject
    {
    }

    [Serializable]
    public class SerializeReferenceFieldNode
    {
        public int value;
    }
}
