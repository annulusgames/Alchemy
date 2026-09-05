using System;
using System.Collections;
using System.Linq;
using Alchemy.Editor;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class ChildObjectsOnlyDrawerTest
    {
        sealed class TestWindow : EditorWindow { }

        static string ChildErrorMessage =>
            ChildObjectsOnlyValidation.DefaultErrorMessage("Child", true);

        [Test]
        public void Attribute_ExposesMessageAndIncludeSelfDefaults()
        {
            var unnamed = new ChildObjectsOnlyAttribute();
            var named = new ChildObjectsOnlyAttribute("Must be a child object.");

            Assert.That(unnamed.Message, Is.Null);
            Assert.That(unnamed.IncludeSelf, Is.True);
            Assert.That(named.Message, Is.EqualTo("Must be a child object."));
            Assert.That(named.IncludeSelf, Is.True);

            unnamed.IncludeSelf = false;
            Assert.That(unnamed.IncludeSelf, Is.False);
            Assert.That(
                ChildObjectsOnlyValidation.DefaultErrorMessage("Child", true),
                Does.Contain("this GameObject, a descendant, or a component"));
            Assert.That(
                ChildObjectsOnlyValidation.DefaultErrorMessage("Child", false),
                Does.Contain("descendant GameObject or a component on a descendant"));
        }

        [Test]
        public void Validation_AcceptsNullEvenWhenIncludeSelfIsFalse()
        {
            var owner = CreateOwner();
            try
            {
                Assert.That(ChildObjectsOnlyValidation.IsValid(null, owner.transform, true), Is.True);
                Assert.That(ChildObjectsOnlyValidation.IsValid(null, owner.transform, false), Is.True);
            }
            finally
            {
                DestroyHierarchy(owner);
            }
        }

        [Test]
        public void Validation_AcceptsOwnerAndDescendantsIncludingInactive()
        {
            var owner = CreateOwner();
            try
            {
                var child = CreateChild(owner, "Child");
                var grandchild = CreateChild(child, "Grandchild");
                child.SetActive(false);
                grandchild.SetActive(false);

                Assert.That(ChildObjectsOnlyValidation.IsValid(owner, owner.transform, true), Is.True);
                Assert.That(ChildObjectsOnlyValidation.IsValid(owner.GetComponent<ChildObjectsOnlyHost>(), owner.transform, true), Is.True);
                Assert.That(ChildObjectsOnlyValidation.IsValid(child, owner.transform, true), Is.True);
                Assert.That(ChildObjectsOnlyValidation.IsValid(grandchild, owner.transform, true), Is.True);
                Assert.That(ChildObjectsOnlyValidation.IsValid(grandchild.transform, owner.transform, false), Is.True);
            }
            finally
            {
                DestroyHierarchy(owner);
            }
        }

        [Test]
        public void Validation_RejectsSelfWhenIncludeSelfIsFalse()
        {
            var owner = CreateOwner();
            try
            {
                Assert.That(ChildObjectsOnlyValidation.IsValid(owner, owner.transform, false), Is.False);
                Assert.That(
                    ChildObjectsOnlyValidation.IsValid(owner.GetComponent<ChildObjectsOnlyHost>(), owner.transform, false),
                    Is.False);
            }
            finally
            {
                DestroyHierarchy(owner);
            }
        }

        [Test]
        public void Validation_RejectsUnrelatedAncestorSiblingAndNonSceneAssets()
        {
            var owner = CreateOwner();
            var unrelated = new GameObject("Unrelated");
            var sibling = new GameObject("Sibling");
            var ancestor = new GameObject("Ancestor");
            owner.transform.SetParent(ancestor.transform);
            try
            {
                var child = CreateChild(owner, "Child");
                sibling.transform.SetParent(ancestor.transform);

                Assert.That(ChildObjectsOnlyValidation.IsValid(unrelated, owner.transform, true), Is.False);
                Assert.That(ChildObjectsOnlyValidation.IsValid(ancestor, owner.transform, true), Is.False);
                Assert.That(ChildObjectsOnlyValidation.IsValid(sibling, owner.transform, true), Is.False);
                Assert.That(ChildObjectsOnlyValidation.IsValid(child, owner.transform, true), Is.True);
                Assert.That(
                    ChildObjectsOnlyValidation.IsValid(Texture2D.whiteTexture, owner.transform, true),
                    Is.False);
            }
            finally
            {
                DestroyHierarchy(ancestor);
                UnityEngine.Object.DestroyImmediate(unrelated);
            }
        }

        [Test]
        public void Validation_ResolvesOwnerFromSerializedRootEvenForNestedFields()
        {
            var owner = CreateOwner();
            try
            {
                var child = CreateChild(owner, "Child");
                var host = owner.GetComponent<ChildObjectsOnlyHost>();
                host.nested.child = child;

                using var serializedObject = new SerializedObject(host);
                var property = serializedObject.FindProperty("nested.child");
                var ownerTransform = ChildObjectsOnlyValidation.GetOwnerTransform(serializedObject);

                Assert.That(ownerTransform, Is.SameAs(owner.transform));
                Assert.That(property, Is.Not.Null);
                Assert.That(
                    ChildObjectsOnlyValidation.IsPropertyValid(property, ownerTransform, true),
                    Is.True);

                host.nested.child = new GameObject("Outside");
                try
                {
                    serializedObject.Update();
                    Assert.That(
                        ChildObjectsOnlyValidation.IsPropertyValid(property, ownerTransform, true),
                        Is.False);
                    Assert.That(host.nested.child.name, Is.EqualTo("Outside"));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(host.nested.child);
                }
            }
            finally
            {
                DestroyHierarchy(owner);
            }
        }

        [Test]
        public void Validation_ValidatesArrayElementsAndReportsUnsupportedUse()
        {
            var owner = CreateOwner();
            try
            {
                var child = CreateChild(owner, "Child");
                var host = owner.GetComponent<ChildObjectsOnlyHost>();
                host.children = new[] { child, (GameObject)null };
                host.unsupported = 1;

                using var serializedObject = new SerializedObject(host);
                var children = serializedObject.FindProperty("children");
                var unsupported = serializedObject.FindProperty("unsupported");
                var texture = serializedObject.FindProperty("texture");
                var anyObject = serializedObject.FindProperty("anyObject");
                var ownerTransform = ChildObjectsOnlyValidation.GetOwnerTransform(serializedObject);

                Assert.That(ChildObjectsOnlyValidation.IsSupportedProperty(children), Is.True);
                Assert.That(ChildObjectsOnlyValidation.IsSupportedProperty(anyObject), Is.True);
                Assert.That(ChildObjectsOnlyValidation.IsSupportedProperty(texture), Is.False);
                Assert.That(
                    ChildObjectsOnlyValidation.IsPropertyValid(children, ownerTransform, true),
                    Is.True);

                host.children[0] = new GameObject("Outside");
                try
                {
                    serializedObject.Update();
                    Assert.That(
                        ChildObjectsOnlyValidation.IsPropertyValid(children, ownerTransform, true),
                        Is.False);
                    Assert.That(host.children[0].name, Is.EqualTo("Outside"));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(host.children[0]);
                }

                Assert.That(ChildObjectsOnlyValidation.IsSupportedProperty(unsupported), Is.False);
                Assert.That(
                    ChildObjectsOnlyValidation.IsPropertyValid(unsupported, ownerTransform, true),
                    Is.False);
            }
            finally
            {
                DestroyHierarchy(owner);
            }
        }

        [Test]
        public void Validation_MixedMultiObjectSelectionIsInvalidWhenAnyTargetFails()
        {
            var owner1 = CreateOwner();
            var owner2 = CreateOwner();
            var unrelated = new GameObject("Unrelated");
            try
            {
                var host1 = owner1.GetComponent<ChildObjectsOnlyHost>();
                var host2 = owner2.GetComponent<ChildObjectsOnlyHost>();
                var child1 = CreateChild(owner1, "Child");
                host1.child = child1;
                host2.child = unrelated;
                host1.children = new[] { child1 };
                host2.children = new[] { unrelated };

                using var serializedObject = new SerializedObject(new UnityEngine.Object[] { host1, host2 });
                var childProperty = serializedObject.FindProperty("child");
                var childrenProperty = serializedObject.FindProperty("children");

                Assert.That(
                    ChildObjectsOnlyValidation.IsSerializedPropertyValid(childProperty, true),
                    Is.False);
                Assert.That(
                    ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true),
                    Is.False);

                host2.child = CreateChild(owner2, "Child2");
                host2.children = new[] { host2.child };
                serializedObject.Update();

                Assert.That(
                    ChildObjectsOnlyValidation.IsSerializedPropertyValid(childProperty, true),
                    Is.True);
                Assert.That(
                    ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true),
                    Is.True);

                host1.children = new[] { child1, unrelated };
                host2.children = Array.Empty<GameObject>();
                serializedObject.Update();
                Assert.That(
                    ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true),
                    Is.False);

                host2.children = new[] { host2.child };
                serializedObject.Update();
                Assert.That(
                    ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unrelated);
                DestroyHierarchy(owner1);
                DestroyHierarchy(owner2);
            }
        }

        [UnityTest]
        public IEnumerator Drawer_ShowsErrorHelpBoxWhilePreservingInvalidValue()
        {
            var owner = CreateOwner();
            var unrelated = new GameObject("Unrelated");
            UnityEditor.Editor editor = null;
            EditorWindow window = null;
            VisualElement root = null;
            try
            {
                var host = owner.GetComponent<ChildObjectsOnlyHost>();
                host.child = unrelated;
                editor = UnityEditor.Editor.CreateEditor(host);
                root = editor.CreateInspectorGUI();
                window = ShowInWindow(root);
                yield return null;

                var helpBox = FindHelpBox(root, ChildErrorMessage);
                foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                    yield return wait;
                Assert.That(host.child, Is.SameAs(unrelated));

                var child = CreateChild(owner, "Child");
                var serializedObject = editor.serializedObject;
                serializedObject.FindProperty("child").objectReferenceValue = child;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(host.child, Is.SameAs(child));
                foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.None))
                    yield return wait;

                var customHelpBox = FindHelpBox(root, "Must be a child object.");
                foreach (var wait in WaitUntilDisplay(customHelpBox, DisplayStyle.None))
                    yield return wait;

                serializedObject.FindProperty("childTransform").objectReferenceValue = unrelated.transform;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(host.childTransform, Is.SameAs(unrelated.transform));
                foreach (var wait in WaitUntilDisplay(customHelpBox, DisplayStyle.Flex))
                    yield return wait;

                var unsupportedHelpBoxes = root.Query<HelpBox>().ToList()
                    .Where(box => box.messageType == HelpBoxMessageType.Warning)
                    .ToList();
                Assert.That(unsupportedHelpBoxes, Is.Not.Empty);
                Assert.That(
                    unsupportedHelpBoxes.All(box => box.text.Contains("ChildObjectsOnly can only be used")),
                    Is.True);
            }
            finally
            {
                TeardownInspector(ref window, ref editor, root);
                UnityEngine.Object.DestroyImmediate(unrelated);
                DestroyHierarchy(owner);
            }
        }

        [Test]
        public void Drawer_ShowsErrorForMixedMultiObjectSelection()
        {
            var owner1 = CreateOwner();
            var owner2 = CreateOwner();
            var unrelated = new GameObject("Unrelated");
            UnityEditor.Editor editor = null;
            EditorWindow window = null;
            VisualElement root = null;
            try
            {
                var host1 = owner1.GetComponent<ChildObjectsOnlyHost>();
                var host2 = owner2.GetComponent<ChildObjectsOnlyHost>();
                host1.child = CreateChild(owner1, "Child");
                host2.child = unrelated;

                editor = UnityEditor.Editor.CreateEditor(new UnityEngine.Object[] { host1, host2 });
                root = editor.CreateInspectorGUI();
                window = ShowInWindow(root);

                var helpBox = FindHelpBox(root, ChildErrorMessage);
                Assert.That(helpBox.style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(host1.child, Is.Not.Null);
                Assert.That(host2.child, Is.SameAs(unrelated));
            }
            finally
            {
                TeardownInspector(ref window, ref editor, root);
                UnityEngine.Object.DestroyImmediate(unrelated);
                DestroyHierarchy(owner1);
                DestroyHierarchy(owner2);
            }
        }

        [UnityTest]
        public IEnumerator Drawer_RevalidatesOnReparentDetachReattachAndUndo()
        {
            var owner = CreateOwner();
            var siblingRoot = new GameObject("SiblingRoot");
            var moving = new GameObject("Moving");
            moving.transform.SetParent(siblingRoot.transform);
            UnityEditor.Editor editor = null;
            EditorWindow window = null;
            VisualElement root = null;
            try
            {
                var host = owner.GetComponent<ChildObjectsOnlyHost>();
                host.child = moving;
                editor = UnityEditor.Editor.CreateEditor(host);
                root = editor.CreateInspectorGUI();
                window = ShowInWindow(root);
                yield return null;

                var helpBox = FindHelpBox(root, ChildErrorMessage);
                foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                    yield return wait;

                moving.transform.SetParent(owner.transform);
                foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.None))
                    yield return wait;

                window.rootVisualElement.Remove(root);

                moving.transform.SetParent(siblingRoot.transform);
                Assert.That(helpBox.style.display.value, Is.EqualTo(DisplayStyle.None));

                window.rootVisualElement.Add(root);
                foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                    yield return wait;

                moving.transform.SetParent(owner.transform);
                foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.None))
                    yield return wait;

                Undo.IncrementCurrentGroup();
                Undo.SetTransformParent(moving.transform, siblingRoot.transform, "ChildObjectsOnly reparent");
                Undo.FlushUndoRecordObjects();
                foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                    yield return wait;

                Undo.PerformUndo();
                Assert.That(moving.transform.parent, Is.SameAs(owner.transform));
                foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.None))
                    yield return wait;

                Undo.PerformRedo();
                Assert.That(moving.transform.parent, Is.SameAs(siblingRoot.transform));
                foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                    yield return wait;

                TeardownInspector(ref window, ref editor, root);
                root = null;

                Assert.DoesNotThrow(() => moving.transform.SetParent(siblingRoot.transform));
            }
            finally
            {
                TeardownInspector(ref window, ref editor, root);
                DestroyHierarchy(owner);
                UnityEngine.Object.DestroyImmediate(siblingRoot);
            }
        }

        [Test]
        public void Drawer_ReportsUnsupportedUseOnScriptableObjectTargets()
        {
            var asset = ScriptableObject.CreateInstance<ChildObjectsOnlyScriptable>();
            UnityEditor.Editor editor = null;
            try
            {
                editor = UnityEditor.Editor.CreateEditor(asset);
                var root = editor.CreateInspectorGUI();
                var helpBox = root.Query<HelpBox>().ToList()
                    .FirstOrDefault(box => box.messageType == HelpBoxMessageType.Warning);

                Assert.That(helpBox, Is.Not.Null);
                Assert.That(helpBox.text, Does.Contain("ChildObjectsOnly can only be used"));
            }
            finally
            {
                if (editor != null) UnityEngine.Object.DestroyImmediate(editor);
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void Validation_RejectsPrefabAssetsAndAcceptsPrefabStageDescendants()
        {
            var path = $"Assets/_AlchemyChildObjectsOnly_{Guid.NewGuid():N}.prefab";
            GameObject contents = null;
            try
            {
                var owner = CreateOwner();
                CreateChild(owner, "Child");
                Assert.That(PrefabUtility.SaveAsPrefabAsset(owner, path), Is.Not.Null);
                DestroyHierarchy(owner);

                var instance = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var host = instance.GetComponent<ChildObjectsOnlyHost>();
                Assert.That(host, Is.Not.Null);
                var child = instance.transform.Find("Child").gameObject;

                Assert.That(EditorUtility.IsPersistent(child), Is.True);
                Assert.That(ChildObjectsOnlyValidation.IsValid(child, host.transform, true), Is.False);

                contents = PrefabUtility.LoadPrefabContents(path);
                var contentsHost = contents.GetComponent<ChildObjectsOnlyHost>();
                var contentsChild = contents.transform.Find("Child").gameObject;
                Assert.That(EditorUtility.IsPersistent(contentsChild), Is.False);
                Assert.That(
                    ChildObjectsOnlyValidation.IsValid(contentsChild, contentsHost.transform, true),
                    Is.True);

                var stage = PrefabStageUtility.OpenPrefab(path);
                if (stage != null)
                {
                    try
                    {
                        var stageRoot = stage.prefabContentsRoot;
                        var stageHost = stageRoot.GetComponent<ChildObjectsOnlyHost>();
                        var stageChild = stageRoot.transform.Find("Child").gameObject;
                        Assert.That(
                            ChildObjectsOnlyValidation.IsValid(stageChild, stageHost.transform, true),
                            Is.True);
                    }
                    finally
                    {
                        StageUtility.GoToMainStage();
                    }
                }
            }
            finally
            {
                if (contents != null)
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }

                AssetDatabase.DeleteAsset(path);
            }
        }

        static GameObject CreateOwner()
        {
            var owner = new GameObject("Owner");
            var host = owner.AddComponent<ChildObjectsOnlyHost>();
            host.nested = new ChildObjectsOnlyHost.Nested();
            return owner;
        }

        static GameObject CreateChild(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child;
        }

        static void DestroyHierarchy(GameObject root)
        {
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        static EditorWindow ShowInWindow(VisualElement content)
        {
            var window = ScriptableObject.CreateInstance<TestWindow>();
            window.position = new Rect(0f, 0f, 640f, 480f);
            window.rootVisualElement.Add(content);
            window.Show();
            return window;
        }

        static void TeardownInspector(ref EditorWindow window, ref UnityEditor.Editor editor, VisualElement root)
        {
            if (root != null)
            {
                root.Unbind();
                root.RemoveFromHierarchy();
            }

            if (window != null)
            {
                window.Close();
                if (window != null)
                {
                    UnityEngine.Object.DestroyImmediate(window);
                }
            }
            window = null;

            if (editor != null)
            {
                UnityEngine.Object.DestroyImmediate(editor);
                editor = null;
            }
        }

        static IEnumerable WaitUntilDisplay(HelpBox helpBox, DisplayStyle expected, float timeoutSeconds = 2f)
        {
            var deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (helpBox.style.display.value != expected)
            {
                if (EditorApplication.timeSinceStartup >= deadline)
                {
                    Assert.That(
                        helpBox.style.display.value,
                        Is.EqualTo(expected),
                        $"HelpBox display did not become {expected} within {timeoutSeconds} seconds.");
                    yield break;
                }

                yield return null;
            }
        }

        static HelpBox FindHelpBox(VisualElement root, string text)
        {
            var helpBox = root.Query<HelpBox>().ToList()
                .FirstOrDefault(box => box.text == text);
            Assert.That(helpBox, Is.Not.Null, $"Expected HelpBox '{text}'.");
            return helpBox;
        }
    }
}
