using System;
using System.Collections;
using System.Linq;
using Alchemy.Editor;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class ChildObjectsOnlyDrawerTest
    {
        readonly ObjectReferenceValidationTestHelper helper = new ObjectReferenceValidationTestHelper();

        static string ChildErrorMessage =>
            ChildObjectsOnlyValidation.DefaultErrorMessage("Child", true);

        [TearDown]
        public void TearDown()
        {
            helper.Dispose();
        }

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

        [TestCase(true)]
        [TestCase(false)]
        public void Validation_AcceptsNullEvenWhenIncludeSelfIsFalse(bool includeSelf)
        {
            var host = CreateHost();
            Assert.That(ChildObjectsOnlyValidation.IsValid(null, host.transform, includeSelf), Is.True);
        }

        [Test]
        public void Validation_AcceptsOwnerAndDescendantsIncludingInactive()
        {
            var host = CreateHost();
            var child = helper.CreateChild(host, "Child");
            var grandchild = helper.CreateChild(child, "Grandchild");
            child.SetActive(false);
            grandchild.SetActive(false);

            Assert.That(ChildObjectsOnlyValidation.IsValid(host.gameObject, host.transform, true), Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsValid(host, host.transform, true), Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsValid(child, host.transform, true), Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsValid(grandchild, host.transform, true), Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsValid(grandchild.transform, host.transform, false), Is.True);
        }

        [Test]
        public void Validation_RejectsSelfWhenIncludeSelfIsFalse()
        {
            var host = CreateHost();
            Assert.That(ChildObjectsOnlyValidation.IsValid(host.gameObject, host.transform, false), Is.False);
            Assert.That(ChildObjectsOnlyValidation.IsValid(host, host.transform, false), Is.False);
        }

        [Test]
        public void Validation_RejectsUnrelatedAncestorSiblingAndNonSceneAssets()
        {
            var ancestor = helper.Create("Ancestor");
            var host = CreateHost();
            host.transform.SetParent(ancestor.transform);
            var sibling = helper.Create("Sibling");
            sibling.transform.SetParent(ancestor.transform);
            var unrelated = helper.Create("Unrelated");
            var child = helper.CreateChild(host, "Child");

            Assert.That(ChildObjectsOnlyValidation.IsValid(unrelated, host.transform, true), Is.False);
            Assert.That(ChildObjectsOnlyValidation.IsValid(ancestor, host.transform, true), Is.False);
            Assert.That(ChildObjectsOnlyValidation.IsValid(sibling, host.transform, true), Is.False);
            Assert.That(ChildObjectsOnlyValidation.IsValid(child, host.transform, true), Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsValid(Texture2D.whiteTexture, host.transform, true), Is.False);
        }

        [Test]
        public void Validation_ResolvesOwnerFromSerializedRootEvenForNestedFields()
        {
            var host = CreateHost();
            host.nested.child = helper.CreateChild(host, "Child");

            using var serializedObject = new SerializedObject(host);
            var property = serializedObject.FindProperty("nested.child");
            var ownerTransform = ChildObjectsOnlyValidation.GetOwnerTransform(serializedObject);

            Assert.That(ownerTransform, Is.SameAs(host.transform));
            Assert.That(property, Is.Not.Null);
            Assert.That(ChildObjectsOnlyValidation.IsPropertyValid(property, ownerTransform, true), Is.True);

            host.nested.child = helper.Create("Outside");
            serializedObject.Update();
            Assert.That(ChildObjectsOnlyValidation.IsPropertyValid(property, ownerTransform, true), Is.False);
            Assert.That(host.nested.child.name, Is.EqualTo("Outside"));
        }

        [Test]
        public void Validation_ValidatesArrayElementsAndReportsUnsupportedUse()
        {
            var host = CreateHost();
            host.children = new[] { helper.CreateChild(host, "Child"), (GameObject)null };
            host.unsupported = 1;

            using var serializedObject = new SerializedObject(host);
            var children = serializedObject.FindProperty("children");
            var unsupported = serializedObject.FindProperty("unsupported");
            var ownerTransform = ChildObjectsOnlyValidation.GetOwnerTransform(serializedObject);

            Assert.That(ChildObjectsOnlyValidation.IsSupportedProperty(children), Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsSupportedProperty(serializedObject.FindProperty("anyObject")), Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsSupportedProperty(serializedObject.FindProperty("texture")), Is.False);
            Assert.That(ChildObjectsOnlyValidation.IsPropertyValid(children, ownerTransform, true), Is.True);

            host.children[0] = helper.Create("Outside");
            serializedObject.Update();
            Assert.That(ChildObjectsOnlyValidation.IsPropertyValid(children, ownerTransform, true), Is.False);
            Assert.That(host.children[0].name, Is.EqualTo("Outside"));
            Assert.That(ChildObjectsOnlyValidation.IsSupportedProperty(unsupported), Is.False);
            Assert.That(ChildObjectsOnlyValidation.IsPropertyValid(unsupported, ownerTransform, true), Is.False);
        }

        [Test]
        public void Validation_MixedMultiObjectSelectionIsInvalidWhenAnyTargetFails()
        {
            var (host1, host2) = TwoHosts();
            var unrelated = helper.Create("Unrelated");
            var child1 = helper.CreateChild(host1, "Child");
            host1.child = child1;
            host2.child = unrelated;
            host1.children = new[] { child1 };
            host2.children = new[] { unrelated };

            using var serializedObject = ObjectReferenceValidationTestHelper.Multi(host1, host2);
            var childProperty = serializedObject.FindProperty("child");
            var childrenProperty = serializedObject.FindProperty("children");

            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childProperty, true), Is.False);
            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true), Is.False);

            host2.child = helper.CreateChild(host2, "Child2");
            host2.children = new[] { host2.child };
            serializedObject.Update();
            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childProperty, true), Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true), Is.True);

            host1.children = new[] { child1, unrelated };
            host2.children = Array.Empty<GameObject>();
            serializedObject.Update();
            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true), Is.False);

            host2.children = new[] { host2.child };
            serializedObject.Update();
            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true), Is.False);
        }

        [Test]
        public void Validation_PendingMultiObjectChildMustBeValidForEveryOwner()
        {
            var (hostA, hostB) = TwoHosts();
            var childA = helper.CreateChild(hostA, "ChildA");

            using var serializedObject = ObjectReferenceValidationTestHelper.Multi(hostA, hostB);
            var childProperty = serializedObject.FindProperty("child");
            childProperty.objectReferenceValue = childA;

            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childProperty, true), Is.False);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(hostA.child, Is.Null);
            Assert.That(hostB.child, Is.Null);
            Assert.That(childProperty.objectReferenceValue, Is.SameAs(childA));
        }

        [Test]
        public void Validation_UnrelatedPendingDoesNotHideSharedArrayTail()
        {
            var (hostA, hostB) = TwoHosts();
            var unrelated = helper.Create("Unrelated");
            hostA.children = new[] { unrelated };
            hostB.children = Array.Empty<GameObject>();

            using var serializedObject = ObjectReferenceValidationTestHelper.Multi(hostA, hostB);
            var childrenProperty = serializedObject.FindProperty("children");
            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true), Is.False);

            serializedObject.FindProperty("unsupported").intValue = 1;

            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true), Is.False);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(hostA.children, Has.Length.EqualTo(1));
            Assert.That(hostA.children[0], Is.SameAs(unrelated));
            Assert.That(hostB.children, Is.Empty);
            Assert.That(serializedObject.FindProperty("unsupported").intValue, Is.EqualTo(1));
        }

        [Test]
        public void Validation_AcceptsValidPendingChangeWithoutApplying()
        {
            var host = CreateHost();
            var unrelated = helper.Create("Unrelated");
            var child = helper.CreateChild(host, "Child");
            host.child = unrelated;
            host.children = new[] { unrelated };

            using var serializedObject = new SerializedObject(host);
            var childProperty = serializedObject.FindProperty("child");
            var childrenProperty = serializedObject.FindProperty("children");
            childProperty.objectReferenceValue = child;
            childrenProperty.GetArrayElementAtIndex(0).objectReferenceValue = child;

            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childProperty, true), Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true), Is.True);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(host.child, Is.SameAs(unrelated));
            Assert.That(host.children[0], Is.SameAs(unrelated));
            Assert.That(childProperty.objectReferenceValue, Is.SameAs(child));
            Assert.That(childrenProperty.GetArrayElementAtIndex(0).objectReferenceValue, Is.SameAs(child));
        }

        [Test]
        public void Validation_UnrelatedPendingPreservesPerTargetScalarValues()
        {
            var (hostA, hostB) = TwoHosts();
            hostA.child = helper.CreateChild(hostA, "ChildA");
            hostB.child = helper.CreateChild(hostB, "ChildB");

            using var serializedObject = ObjectReferenceValidationTestHelper.Multi(hostA, hostB);
            var childProperty = serializedObject.FindProperty("child");
            serializedObject.FindProperty("unsupported").intValue = 1;

            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childProperty, true), Is.True);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(hostA.child.name, Is.EqualTo("ChildA"));
            Assert.That(hostB.child.name, Is.EqualTo("ChildB"));
        }

        [Test]
        public void Validation_PendingNullClearsInvalidMultiObjectChildWithoutApplying()
        {
            var (hostA, hostB) = TwoHosts();
            var unrelated = helper.Create("Unrelated");
            hostA.child = unrelated;
            hostB.child = unrelated;

            using var serializedObject = ObjectReferenceValidationTestHelper.Multi(hostA, hostB);
            var childProperty = serializedObject.FindProperty("child");
            childProperty.objectReferenceValue = null;

            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childProperty, true), Is.True);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(hostA.child, Is.SameAs(unrelated));
            Assert.That(hostB.child, Is.SameAs(unrelated));
        }

        [Test]
        public void Validation_PendingSharedArrayGrowMustBeValidForEveryOwner()
        {
            var (hostA, hostB) = TwoHosts();
            hostA.children = Array.Empty<GameObject>();
            hostB.children = Array.Empty<GameObject>();
            var childA = helper.CreateChild(hostA, "ChildA");

            using var serializedObject = ObjectReferenceValidationTestHelper.Multi(hostA, hostB);
            var childrenProperty = serializedObject.FindProperty("children");
            childrenProperty.arraySize = 1;
            childrenProperty.GetArrayElementAtIndex(0).objectReferenceValue = childA;

            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true), Is.False);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(hostA.children, Is.Empty);
            Assert.That(hostB.children, Is.Empty);
            Assert.That(childrenProperty.GetArrayElementAtIndex(0).objectReferenceValue, Is.SameAs(childA));
        }

        [Test]
        public void Validation_PendingArrayShrinkIgnoresRemovedInvalidTail()
        {
            var (hostA, hostB) = TwoHosts();
            var unrelated = helper.Create("Unrelated");
            hostA.children = new[] { helper.CreateChild(hostA, "ChildA"), unrelated };
            hostB.children = new[] { helper.CreateChild(hostB, "ChildB"), unrelated };

            using var serializedObject = ObjectReferenceValidationTestHelper.Multi(hostA, hostB);
            var childrenProperty = serializedObject.FindProperty("children");
            childrenProperty.arraySize = 1;

            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true), Is.True);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(hostA.children, Has.Length.EqualTo(2));
            Assert.That(hostB.children, Has.Length.EqualTo(2));
            Assert.That(childrenProperty.arraySize, Is.EqualTo(1));
        }

        [Test]
        public void Validation_PendingArrayGrowDuplicatesPerTargetLastElement()
        {
            var (hostA, hostB) = TwoHosts();
            hostA.children = new[] { helper.CreateChild(hostA, "ChildA") };
            hostB.children = new[] { helper.CreateChild(hostB, "ChildB") };

            using var serializedObject = ObjectReferenceValidationTestHelper.Multi(hostA, hostB);
            var childrenProperty = serializedObject.FindProperty("children");
            childrenProperty.arraySize = 2;

            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true), Is.True);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(hostA.children, Has.Length.EqualTo(1));
            Assert.That(hostB.children, Has.Length.EqualTo(1));
            Assert.That(childrenProperty.arraySize, Is.EqualTo(2));
        }

        [Test]
        public void Validation_PendingUniformElementEditDoesNotDropInvalidTails()
        {
            var (hostA, hostB) = TwoHosts();
            var unrelated = helper.Create("Unrelated");
            hostA.children = new[] { helper.CreateChild(hostA, "ChildA"), unrelated };
            hostB.children = new[] { helper.CreateChild(hostB, "ChildB"), unrelated };

            using var serializedObject = ObjectReferenceValidationTestHelper.Multi(hostA, hostB);
            var childrenProperty = serializedObject.FindProperty("children");
            childrenProperty.GetArrayElementAtIndex(0).objectReferenceValue = null;

            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true), Is.False);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(hostA.children, Has.Length.EqualTo(2));
            Assert.That(hostB.children, Has.Length.EqualTo(2));
            Assert.That(hostA.children[1], Is.SameAs(unrelated));
            Assert.That(hostB.children[1], Is.SameAs(unrelated));
        }

        [UnityTest]
        public IEnumerator Drawer_ShowsErrorHelpBoxWhilePreservingInvalidValue()
        {
            var host = CreateHost();
            var unrelated = helper.Create("Unrelated");
            host.child = unrelated;
            helper.ShowInspector(host);
            yield return null;

            var helpBox = helper.FindHelpBox(ChildErrorMessage);
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                yield return wait;
            Assert.That(host.child, Is.SameAs(unrelated));

            var child = helper.CreateChild(host, "Child");
            var serializedObject = helper.Editor.serializedObject;
            serializedObject.FindProperty("child").objectReferenceValue = child;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(host.child, Is.SameAs(child));
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(helpBox, DisplayStyle.None))
                yield return wait;

            var customHelpBox = helper.FindHelpBox("Must be a child object.");
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(customHelpBox, DisplayStyle.None))
                yield return wait;

            serializedObject.FindProperty("childTransform").objectReferenceValue = unrelated.transform;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(host.childTransform, Is.SameAs(unrelated.transform));
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(customHelpBox, DisplayStyle.Flex))
                yield return wait;

            var unsupportedHelpBoxes = helper.InspectorRoot.Query<HelpBox>().ToList()
                .Where(box => box.messageType == HelpBoxMessageType.Warning)
                .ToList();
            Assert.That(unsupportedHelpBoxes, Is.Not.Empty);
            Assert.That(
                unsupportedHelpBoxes.All(box => box.text.Contains("ChildObjectsOnly can only be used")),
                Is.True);
        }

        [Test]
        public void Drawer_ShowsErrorForMixedMultiObjectSelection()
        {
            var (host1, host2) = TwoHosts();
            var unrelated = helper.Create("Unrelated");
            host1.child = helper.CreateChild(host1, "Child");
            host2.child = unrelated;
            helper.ShowInspector(host1, host2);

            Assert.That(helper.FindHelpBox(ChildErrorMessage).style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(host1.child, Is.Not.Null);
            Assert.That(host2.child, Is.SameAs(unrelated));
        }

        [UnityTest]
        public IEnumerator Drawer_RevalidatesOnReparentDetachReattachAndUndo()
        {
            var host = CreateHost();
            var siblingRoot = helper.Create("SiblingRoot");
            var moving = helper.Create("Moving");
            moving.transform.SetParent(siblingRoot.transform);
            host.child = moving;
            helper.ShowInspector(host);
            yield return null;

            var helpBox = helper.FindHelpBox(ChildErrorMessage);
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                yield return wait;

            moving.transform.SetParent(host.transform);
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(helpBox, DisplayStyle.None))
                yield return wait;

            helper.Window.rootVisualElement.Remove(helper.InspectorRoot);
            moving.transform.SetParent(siblingRoot.transform);
            Assert.That(helpBox.style.display.value, Is.EqualTo(DisplayStyle.None));

            helper.Window.rootVisualElement.Add(helper.InspectorRoot);
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                yield return wait;

            moving.transform.SetParent(host.transform);
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(helpBox, DisplayStyle.None))
                yield return wait;

            Undo.IncrementCurrentGroup();
            Undo.SetTransformParent(moving.transform, siblingRoot.transform, "ChildObjectsOnly reparent");
            Undo.FlushUndoRecordObjects();
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                yield return wait;

            Undo.PerformUndo();
            Assert.That(moving.transform.parent, Is.SameAs(host.transform));
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(helpBox, DisplayStyle.None))
                yield return wait;

            Undo.PerformRedo();
            Assert.That(moving.transform.parent, Is.SameAs(siblingRoot.transform));
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                yield return wait;

            helper.CloseInspector();
            Assert.DoesNotThrow(() => moving.transform.SetParent(siblingRoot.transform));
        }

        [UnityTest]
        public IEnumerator Drawer_ThrottlesExternalChangesToOneRefresh()
        {
            var host = CreateHost();
            var siblingRoot = helper.Create("SiblingRoot");
            var moving = helper.Create("Moving");
            moving.transform.SetParent(siblingRoot.transform);
            host.child = moving;
            helper.ShowInspector(host);
            yield return null;

            var helpBox = helper.FindHelpBox(ChildErrorMessage);
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                yield return wait;

            // Setup can raise a deferred hierarchyChanged. Let that trailing refresh finish.
            foreach (var wait in WaitForEditorSeconds(0.25))
                yield return wait;
            Assert.That(helpBox.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            var displayChanges = 0;
            var lastDisplay = helpBox.style.display.value;

            // Record several undoable reparents, then undo and redo them in this tick.
            // PerformUndo/PerformRedo raise undoRedoPerformed synchronously; the help box stays stale.
            RecordParent(host.transform);
            RecordParent(siblingRoot.transform);
            RecordParent(host.transform);
            Undo.FlushUndoRecordObjects();
            Undo.IncrementCurrentGroup();

            Undo.PerformUndo();
            Undo.PerformRedo();
            Undo.PerformUndo();
            Undo.PerformRedo();
            NoteDisplay();

            Assert.That(moving.transform.parent, Is.SameAs(host.transform));
            Assert.That(displayChanges, Is.Zero);
            Assert.That(helpBox.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            var deadline = EditorApplication.timeSinceStartup + 2.0;
            while (displayChanges == 0 && EditorApplication.timeSinceStartup < deadline)
            {
                yield return null;
                NoteDisplay();
            }

            Assert.That(helpBox.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(displayChanges, Is.EqualTo(1));

            foreach (var wait in WaitForEditorSeconds(0.25))
            {
                yield return wait;
                NoteDisplay();
            }

            Assert.That(displayChanges, Is.EqualTo(1));
            Assert.That(moving.transform.parent, Is.SameAs(host.transform));
            LogAssert.NoUnexpectedReceived();

            void RecordParent(Transform parent)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetTransformParent(moving.transform, parent, "ChildObjectsOnly reparent");
            }

            void NoteDisplay()
            {
                var current = helpBox.style.display.value;
                if (current == lastDisplay) return;
                displayChanges++;
                lastDisplay = current;
            }

            static IEnumerable WaitForEditorSeconds(double seconds)
            {
                var end = EditorApplication.timeSinceStartup + seconds;
                while (EditorApplication.timeSinceStartup < end)
                    yield return null;
            }
        }

        [Test]
        public void Drawer_ReportsUnsupportedUseOnScriptableObjectTargets()
        {
            var asset = helper.Track(ScriptableObject.CreateInstance<ChildObjectsOnlyScriptable>());
            helper.CreateInspector(asset);

            var helpBox = helper.InspectorRoot.Query<HelpBox>().ToList()
                .FirstOrDefault(box => box.messageType == HelpBoxMessageType.Warning);
            Assert.That(helpBox, Is.Not.Null);
            Assert.That(helpBox.text, Does.Contain("ChildObjectsOnly can only be used"));
        }

        [Test]
        public void Validation_RejectsPrefabAssetsAndAcceptsPrefabStageDescendants()
        {
            GameObject contents = null;
            var openedStage = false;
            string path = null;
            try
            {
                var host = CreateHost();
                helper.CreateChild(host, "Child");
                var prefab = helper.CreatePrefabAsset(host.gameObject, "_AlchemyChildObjectsOnly");
                path = AssetDatabase.GetAssetPath(prefab);
                Assert.That(prefab, Is.Not.Null);
                helper.DestroyCreated();

                var instance = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var assetHost = instance.GetComponent<ChildObjectsOnlyHost>();
                Assert.That(assetHost, Is.Not.Null);
                var child = instance.transform.Find("Child").gameObject;
                Assert.That(EditorUtility.IsPersistent(child), Is.True);
                Assert.That(ChildObjectsOnlyValidation.IsValid(child, assetHost.transform, true), Is.False);

                contents = PrefabUtility.LoadPrefabContents(path);
                var contentsHost = contents.GetComponent<ChildObjectsOnlyHost>();
                var contentsChild = contents.transform.Find("Child").gameObject;
                Assert.That(EditorUtility.IsPersistent(contentsChild), Is.False);
                Assert.That(ChildObjectsOnlyValidation.IsValid(contentsChild, contentsHost.transform, true), Is.True);

                var stage = PrefabStageUtility.OpenPrefab(path);
                if (stage == null)
                    return;

                openedStage = true;
                var stageRoot = stage.prefabContentsRoot;
                Assert.That(
                    ChildObjectsOnlyValidation.IsValid(
                        stageRoot.transform.Find("Child").gameObject,
                        stageRoot.GetComponent<ChildObjectsOnlyHost>().transform,
                        true),
                    Is.True);
            }
            finally
            {
                if (openedStage)
                    StageUtility.GoToMainStage();
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        ChildObjectsOnlyHost CreateHost(string name = "Owner")
        {
            var host = helper.CreateHost<ChildObjectsOnlyHost>(name);
            host.nested = new ChildObjectsOnlyHost.Nested();
            return host;
        }

        (ChildObjectsOnlyHost hostA, ChildObjectsOnlyHost hostB) TwoHosts() =>
            (CreateHost("OwnerA"), CreateHost("OwnerB"));
    }
}
