using System;
using System.Collections;
using System.Collections.Generic;
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

        readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
        EditorWindow window;
        UnityEditor.Editor editor;
        VisualElement inspectorRoot;

        static string ChildErrorMessage =>
            ChildObjectsOnlyValidation.DefaultErrorMessage("Child", true);

        [TearDown]
        public void TearDown()
        {
            CloseInspector();
            DestroyCreated();
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
            var child = CreateChild(host, "Child");
            var grandchild = CreateChild(child, "Grandchild");
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
            var ancestor = Create("Ancestor");
            var host = CreateHost();
            host.transform.SetParent(ancestor.transform);
            var sibling = Create("Sibling");
            sibling.transform.SetParent(ancestor.transform);
            var unrelated = Create("Unrelated");
            var child = CreateChild(host, "Child");

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
            host.nested.child = CreateChild(host, "Child");

            using var serializedObject = new SerializedObject(host);
            var property = serializedObject.FindProperty("nested.child");
            var ownerTransform = ChildObjectsOnlyValidation.GetOwnerTransform(serializedObject);

            Assert.That(ownerTransform, Is.SameAs(host.transform));
            Assert.That(property, Is.Not.Null);
            Assert.That(ChildObjectsOnlyValidation.IsPropertyValid(property, ownerTransform, true), Is.True);

            host.nested.child = Create("Outside");
            serializedObject.Update();
            Assert.That(ChildObjectsOnlyValidation.IsPropertyValid(property, ownerTransform, true), Is.False);
            Assert.That(host.nested.child.name, Is.EqualTo("Outside"));
        }

        [Test]
        public void Validation_ValidatesArrayElementsAndReportsUnsupportedUse()
        {
            var host = CreateHost();
            host.children = new[] { CreateChild(host, "Child"), (GameObject)null };
            host.unsupported = 1;

            using var serializedObject = new SerializedObject(host);
            var children = serializedObject.FindProperty("children");
            var unsupported = serializedObject.FindProperty("unsupported");
            var ownerTransform = ChildObjectsOnlyValidation.GetOwnerTransform(serializedObject);

            Assert.That(ChildObjectsOnlyValidation.IsSupportedProperty(children), Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsSupportedProperty(serializedObject.FindProperty("anyObject")), Is.True);
            Assert.That(ChildObjectsOnlyValidation.IsSupportedProperty(serializedObject.FindProperty("texture")), Is.False);
            Assert.That(ChildObjectsOnlyValidation.IsPropertyValid(children, ownerTransform, true), Is.True);

            host.children[0] = Create("Outside");
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
            var unrelated = Create("Unrelated");
            var child1 = CreateChild(host1, "Child");
            host1.child = child1;
            host2.child = unrelated;
            host1.children = new[] { child1 };
            host2.children = new[] { unrelated };

            using var serializedObject = Multi(host1, host2);
            var childProperty = serializedObject.FindProperty("child");
            var childrenProperty = serializedObject.FindProperty("children");

            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childProperty, true), Is.False);
            Assert.That(ChildObjectsOnlyValidation.IsSerializedPropertyValid(childrenProperty, true), Is.False);

            host2.child = CreateChild(host2, "Child2");
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
            var childA = CreateChild(hostA, "ChildA");

            using var serializedObject = Multi(hostA, hostB);
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
            var unrelated = Create("Unrelated");
            hostA.children = new[] { unrelated };
            hostB.children = Array.Empty<GameObject>();

            using var serializedObject = Multi(hostA, hostB);
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
            var unrelated = Create("Unrelated");
            var child = CreateChild(host, "Child");
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
            hostA.child = CreateChild(hostA, "ChildA");
            hostB.child = CreateChild(hostB, "ChildB");

            using var serializedObject = Multi(hostA, hostB);
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
            var unrelated = Create("Unrelated");
            hostA.child = unrelated;
            hostB.child = unrelated;

            using var serializedObject = Multi(hostA, hostB);
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
            var childA = CreateChild(hostA, "ChildA");

            using var serializedObject = Multi(hostA, hostB);
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
            var unrelated = Create("Unrelated");
            hostA.children = new[] { CreateChild(hostA, "ChildA"), unrelated };
            hostB.children = new[] { CreateChild(hostB, "ChildB"), unrelated };

            using var serializedObject = Multi(hostA, hostB);
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
            hostA.children = new[] { CreateChild(hostA, "ChildA") };
            hostB.children = new[] { CreateChild(hostB, "ChildB") };

            using var serializedObject = Multi(hostA, hostB);
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
            var unrelated = Create("Unrelated");
            hostA.children = new[] { CreateChild(hostA, "ChildA"), unrelated };
            hostB.children = new[] { CreateChild(hostB, "ChildB"), unrelated };

            using var serializedObject = Multi(hostA, hostB);
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
            var unrelated = Create("Unrelated");
            host.child = unrelated;
            ShowInspector(host);
            yield return null;

            var helpBox = FindHelpBox(ChildErrorMessage);
            foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                yield return wait;
            Assert.That(host.child, Is.SameAs(unrelated));

            var child = CreateChild(host, "Child");
            var serializedObject = editor.serializedObject;
            serializedObject.FindProperty("child").objectReferenceValue = child;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(host.child, Is.SameAs(child));
            foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.None))
                yield return wait;

            var customHelpBox = FindHelpBox("Must be a child object.");
            foreach (var wait in WaitUntilDisplay(customHelpBox, DisplayStyle.None))
                yield return wait;

            serializedObject.FindProperty("childTransform").objectReferenceValue = unrelated.transform;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(host.childTransform, Is.SameAs(unrelated.transform));
            foreach (var wait in WaitUntilDisplay(customHelpBox, DisplayStyle.Flex))
                yield return wait;

            var unsupportedHelpBoxes = inspectorRoot.Query<HelpBox>().ToList()
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
            var unrelated = Create("Unrelated");
            host1.child = CreateChild(host1, "Child");
            host2.child = unrelated;
            ShowInspector(host1, host2);

            Assert.That(FindHelpBox(ChildErrorMessage).style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(host1.child, Is.Not.Null);
            Assert.That(host2.child, Is.SameAs(unrelated));
        }

        [UnityTest]
        public IEnumerator Drawer_RevalidatesOnReparentDetachReattachAndUndo()
        {
            var host = CreateHost();
            var siblingRoot = Create("SiblingRoot");
            var moving = Create("Moving");
            moving.transform.SetParent(siblingRoot.transform);
            host.child = moving;
            ShowInspector(host);
            yield return null;

            var helpBox = FindHelpBox(ChildErrorMessage);
            foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                yield return wait;

            moving.transform.SetParent(host.transform);
            foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.None))
                yield return wait;

            window.rootVisualElement.Remove(inspectorRoot);
            moving.transform.SetParent(siblingRoot.transform);
            Assert.That(helpBox.style.display.value, Is.EqualTo(DisplayStyle.None));

            window.rootVisualElement.Add(inspectorRoot);
            foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                yield return wait;

            moving.transform.SetParent(host.transform);
            foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.None))
                yield return wait;

            Undo.IncrementCurrentGroup();
            Undo.SetTransformParent(moving.transform, siblingRoot.transform, "ChildObjectsOnly reparent");
            Undo.FlushUndoRecordObjects();
            foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                yield return wait;

            Undo.PerformUndo();
            Assert.That(moving.transform.parent, Is.SameAs(host.transform));
            foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.None))
                yield return wait;

            Undo.PerformRedo();
            Assert.That(moving.transform.parent, Is.SameAs(siblingRoot.transform));
            foreach (var wait in WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                yield return wait;

            CloseInspector();
            Assert.DoesNotThrow(() => moving.transform.SetParent(siblingRoot.transform));
        }

        [Test]
        public void Drawer_ReportsUnsupportedUseOnScriptableObjectTargets()
        {
            var asset = Track(ScriptableObject.CreateInstance<ChildObjectsOnlyScriptable>());
            CreateInspector(asset);

            var helpBox = inspectorRoot.Query<HelpBox>().ToList()
                .FirstOrDefault(box => box.messageType == HelpBoxMessageType.Warning);
            Assert.That(helpBox, Is.Not.Null);
            Assert.That(helpBox.text, Does.Contain("ChildObjectsOnly can only be used"));
        }

        [Test]
        public void Validation_RejectsPrefabAssetsAndAcceptsPrefabStageDescendants()
        {
            var path = $"Assets/_AlchemyChildObjectsOnly_{Guid.NewGuid():N}.prefab";
            GameObject contents = null;
            var openedStage = false;
            try
            {
                var host = CreateHost();
                CreateChild(host, "Child");
                Assert.That(PrefabUtility.SaveAsPrefabAsset(host.gameObject, path), Is.Not.Null);
                DestroyCreated();

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
                AssetDatabase.DeleteAsset(path);
            }
        }

        ChildObjectsOnlyHost CreateHost(string name = "Owner")
        {
            var owner = new GameObject(name);
            var host = owner.AddComponent<ChildObjectsOnlyHost>();
            host.nested = new ChildObjectsOnlyHost.Nested();
            Track(owner);
            return host;
        }

        (ChildObjectsOnlyHost hostA, ChildObjectsOnlyHost hostB) TwoHosts() =>
            (CreateHost("OwnerA"), CreateHost("OwnerB"));

        GameObject CreateChild(Component parent, string name) => CreateChild(parent.gameObject, name);

        GameObject CreateChild(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return Track(child);
        }

        GameObject Create(string name) => Track(new GameObject(name));

        T Track<T>(T obj) where T : UnityEngine.Object
        {
            created.Add(obj);
            return obj;
        }

        static SerializedObject Multi(params UnityEngine.Object[] targets) => new SerializedObject(targets);

        void ShowInspector(params UnityEngine.Object[] targets)
        {
            CreateInspector(targets);
            window = ScriptableObject.CreateInstance<TestWindow>();
            window.position = new Rect(0f, 0f, 640f, 480f);
            window.rootVisualElement.Add(inspectorRoot);
            window.Show();
        }

        void CreateInspector(params UnityEngine.Object[] targets)
        {
            editor = UnityEditor.Editor.CreateEditor(targets);
            inspectorRoot = editor.CreateInspectorGUI();
        }

        void CloseInspector()
        {
            if (inspectorRoot != null)
            {
                inspectorRoot.Unbind();
                inspectorRoot.RemoveFromHierarchy();
                inspectorRoot = null;
            }

            if (window != null)
            {
                window.Close();
                if (window != null)
                    UnityEngine.Object.DestroyImmediate(window);
                window = null;
            }

            if (editor != null)
            {
                UnityEngine.Object.DestroyImmediate(editor);
                editor = null;
            }
        }

        void DestroyCreated()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                    UnityEngine.Object.DestroyImmediate(created[i]);
            }
            created.Clear();
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

        HelpBox FindHelpBox(string text)
        {
            var helpBox = inspectorRoot.Query<HelpBox>().ToList()
                .FirstOrDefault(box => box.text == text);
            Assert.That(helpBox, Is.Not.Null, $"Expected HelpBox '{text}'.");
            return helpBox;
        }
    }
}
