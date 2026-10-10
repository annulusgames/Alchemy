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
    public class SceneObjectsOnlyDrawerTest
    {
        readonly ObjectReferenceValidationTestHelper helper = new ObjectReferenceValidationTestHelper();

        static string SceneObjectErrorMessage =>
            SceneObjectsOnlyValidation.DefaultErrorMessage("Scene Object");

        [TearDown]
        public void TearDown() => helper.Dispose();

        [Test]
        public void Attribute_ExposesMessageDefault()
        {
            var unnamed = new SceneObjectsOnlyAttribute();
            var named = new SceneObjectsOnlyAttribute("Must be a scene object.");

            Assert.That(unnamed.Message, Is.Null);
            Assert.That(named.Message, Is.EqualTo("Must be a scene object."));
            Assert.That(
                SceneObjectsOnlyValidation.DefaultErrorMessage("Scene Object"),
                Is.EqualTo("Scene Object must be a scene object."));
        }

        [Test]
        public void Validation_AcceptsNull()
        {
            Assert.That(SceneObjectsOnlyValidation.IsValid(null), Is.True);
        }

        [Test]
        public void Validation_AcceptsSceneGameObjectAndComponent()
        {
            var host = CreateHost();
            var other = helper.Create("Other");

            Assert.That(SceneObjectsOnlyValidation.IsValid(host.gameObject), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsValid(host), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsValid(other), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsValid(other.transform), Is.True);
        }

        [Test]
        public void Validation_RejectsAssetsAndNonSceneObjects()
        {
            var host = CreateHost();
            var prefab = helper.CreatePrefabAsset(helper.Create("PrefabSource"));
            var transient = helper.Track(ScriptableObject.CreateInstance<SceneObjectsOnlyScriptable>());

            Assert.That(SceneObjectsOnlyValidation.IsValid(Texture2D.whiteTexture), Is.False);
            Assert.That(SceneObjectsOnlyValidation.IsValid(prefab), Is.False);
            Assert.That(SceneObjectsOnlyValidation.IsValid(transient), Is.False);
            Assert.That(SceneObjectsOnlyValidation.IsValid(host.gameObject), Is.True);
        }

        [Test]
        public void Validation_ResolvesNestedFields()
        {
            var host = CreateHost();
            host.nested.sceneObject = helper.Create("Nested");

            using var serializedObject = new SerializedObject(host);
            var property = serializedObject.FindProperty("nested.sceneObject");

            Assert.That(property, Is.Not.Null);
            Assert.That(SceneObjectsOnlyValidation.IsPropertyValid(property), Is.True);

            host.nested.sceneObject = helper.CreatePrefabAsset(helper.Create("PrefabSource"));
            serializedObject.Update();
            Assert.That(SceneObjectsOnlyValidation.IsPropertyValid(property), Is.False);
            Assert.That(host.nested.sceneObject, Is.Not.Null);
            Assert.That(EditorUtility.IsPersistent(host.nested.sceneObject), Is.True);
        }

        [Test]
        public void Validation_ValidatesArrayElementsAndReportsUnsupportedUse()
        {
            var host = CreateHost();
            host.sceneObjects = new[] { helper.Create("A"), (GameObject)null };
            host.unsupported = 1;
            host.texture = Texture2D.whiteTexture;

            using var serializedObject = new SerializedObject(host);
            var sceneObjects = serializedObject.FindProperty("sceneObjects");
            var unsupported = serializedObject.FindProperty("unsupported");
            var texture = serializedObject.FindProperty("texture");
            var anyObject = serializedObject.FindProperty("anyObject");

            Assert.That(SceneObjectsOnlyValidation.IsSupportedProperty(sceneObjects), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSupportedProperty(anyObject), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSupportedProperty(texture), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsPropertyValid(sceneObjects), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsPropertyValid(texture), Is.False);

            host.sceneObjects[0] = helper.CreatePrefabAsset(helper.Create("PrefabSource"));
            serializedObject.Update();
            Assert.That(SceneObjectsOnlyValidation.IsPropertyValid(sceneObjects), Is.False);
            Assert.That(SceneObjectsOnlyValidation.IsSupportedProperty(unsupported), Is.False);
            Assert.That(SceneObjectsOnlyValidation.IsPropertyValid(unsupported), Is.False);
        }

        [Test]
        public void Validation_MixedMultiObjectSelectionIsInvalidWhenAnyTargetFails()
        {
            var (host1, host2) = TwoHosts();
            var prefab = helper.CreatePrefabAsset(helper.Create("PrefabSource"));
            host1.sceneObject = helper.Create("SceneA");
            host2.sceneObject = prefab;
            host1.sceneObjects = new[] { host1.sceneObject };
            host2.sceneObjects = new[] { host2.sceneObject };

            using var serializedObject = ObjectReferenceValidationTestHelper.Multi(host1, host2);
            var sceneObject = serializedObject.FindProperty("sceneObject");
            var sceneObjects = serializedObject.FindProperty("sceneObjects");

            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObject), Is.False);
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObjects), Is.False);

            host2.sceneObject = helper.Create("SceneB");
            host2.sceneObjects = new[] { host2.sceneObject };
            serializedObject.Update();
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObject), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObjects), Is.True);
        }

        [Test]
        public void Validation_PendingMultiObjectAssetMustBeInvalidForEveryTarget()
        {
            var (hostA, hostB) = TwoHosts();
            var prefab = helper.CreatePrefabAsset(helper.Create("PrefabSource"));

            using var serializedObject = ObjectReferenceValidationTestHelper.Multi(hostA, hostB);
            var sceneObject = serializedObject.FindProperty("sceneObject");
            sceneObject.objectReferenceValue = prefab;

            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObject), Is.False);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(hostA.sceneObject, Is.Null);
            Assert.That(hostB.sceneObject, Is.Null);
            Assert.That(sceneObject.objectReferenceValue, Is.SameAs(prefab));
        }

        [Test]
        public void Validation_AcceptsValidPendingChangeWithoutApplying()
        {
            var host = CreateHost();
            var prefab = helper.CreatePrefabAsset(helper.Create("PrefabSource"));
            var scene = helper.Create("Scene");
            host.sceneObject = prefab;
            host.sceneObjects = new[] { prefab };

            using var serializedObject = new SerializedObject(host);
            var sceneObject = serializedObject.FindProperty("sceneObject");
            var sceneObjects = serializedObject.FindProperty("sceneObjects");
            sceneObject.objectReferenceValue = scene;
            sceneObjects.GetArrayElementAtIndex(0).objectReferenceValue = scene;

            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObject), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObjects), Is.True);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(host.sceneObject, Is.SameAs(prefab));
            Assert.That(host.sceneObjects[0], Is.SameAs(prefab));
            Assert.That(sceneObject.objectReferenceValue, Is.SameAs(scene));
            Assert.That(sceneObjects.GetArrayElementAtIndex(0).objectReferenceValue, Is.SameAs(scene));
        }

        [Test]
        public void Validation_SingleTargetPendingArrayShrinkUsesLiveProperty()
        {
            var host = CreateHost();
            var prefab = helper.CreatePrefabAsset(helper.Create("PrefabSource"));
            host.sceneObjects = new[] { helper.Create("Scene"), prefab };

            using var serializedObject = new SerializedObject(host);
            var sceneObjects = serializedObject.FindProperty("sceneObjects");
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObjects), Is.False);

            sceneObjects.arraySize = 1;

            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(sceneObjects), Is.True);
            Assert.That(serializedObject.hasModifiedProperties, Is.True);
            Assert.That(host.sceneObjects.Length, Is.EqualTo(2));
            Assert.That(host.sceneObjects[1], Is.SameAs(prefab));
            Assert.That(sceneObjects.arraySize, Is.EqualTo(1));
        }

        [Test]
        public void Validation_SupportsScriptableObjectTargets()
        {
            var asset = helper.Track(ScriptableObject.CreateInstance<SceneObjectsOnlyScriptable>());
            var scene = helper.Create("Scene");
            var prefab = helper.CreatePrefabAsset(helper.Create("PrefabSource"));
            asset.sceneObject = scene;

            using var serializedObject = new SerializedObject(asset);
            var property = serializedObject.FindProperty("sceneObject");

            Assert.That(SceneObjectsOnlyValidation.IsSupportedProperty(property), Is.True);
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(property), Is.True);

            property.objectReferenceValue = prefab;
            Assert.That(SceneObjectsOnlyValidation.IsSerializedPropertyValid(property), Is.False);
            Assert.That(asset.sceneObject, Is.SameAs(scene));
        }

        [UnityTest]
        public IEnumerator Drawer_ShowsErrorHelpBoxWhilePreservingInvalidValue()
        {
            var host = CreateHost();
            var prefab = helper.CreatePrefabAsset(helper.Create("PrefabSource"));
            host.sceneObject = prefab;
            helper.ShowInspector(host);
            yield return null;

            var helpBox = helper.FindHelpBox(SceneObjectErrorMessage);
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(helpBox, DisplayStyle.Flex))
                yield return wait;
            Assert.That(host.sceneObject, Is.SameAs(prefab));

            var scene = helper.Create("Scene");
            var serializedObject = helper.Editor.serializedObject;
            serializedObject.FindProperty("sceneObject").objectReferenceValue = scene;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(host.sceneObject, Is.SameAs(scene));
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(helpBox, DisplayStyle.None))
                yield return wait;

            var customHelpBox = helper.FindHelpBox("Must be a scene object.");
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(customHelpBox, DisplayStyle.None))
                yield return wait;

            serializedObject.FindProperty("sceneTransform").objectReferenceValue = prefab.transform;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(host.sceneTransform, Is.SameAs(prefab.transform));
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(customHelpBox, DisplayStyle.Flex))
                yield return wait;

            var unsupportedHelpBoxes = helper.InspectorRoot.Query<HelpBox>().ToList()
                .Where(box => box.messageType == HelpBoxMessageType.Warning)
                .ToList();
            Assert.That(unsupportedHelpBoxes, Is.Not.Empty);
            Assert.That(
                unsupportedHelpBoxes.All(box => box.text.Contains("SceneObjectsOnly can only be used")),
                Is.True);
        }

        [Test]
        public void Drawer_ShowsErrorForMixedMultiObjectSelection()
        {
            var (host1, host2) = TwoHosts();
            var prefab = helper.CreatePrefabAsset(helper.Create("PrefabSource"));
            host1.sceneObject = helper.Create("SceneA");
            host2.sceneObject = prefab;
            helper.ShowInspector(host1, host2);

            Assert.That(helper.FindHelpBox(SceneObjectErrorMessage).style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(host1.sceneObject, Is.Not.Null);
            Assert.That(host2.sceneObject, Is.SameAs(prefab));
        }

        [Test]
        public void Drawer_ValidatesScriptableObjectTargetsInsteadOfUnsupported()
        {
            var asset = helper.Track(ScriptableObject.CreateInstance<SceneObjectsOnlyScriptable>());
            asset.sceneObject = helper.CreatePrefabAsset(helper.Create("PrefabSource"));
            helper.CreateInspector(asset);

            var helpBoxes = helper.InspectorRoot.Query<HelpBox>().ToList();
            Assert.That(helpBoxes.Any(box => box.messageType == HelpBoxMessageType.Warning), Is.False);
            var error = helpBoxes.FirstOrDefault(box => box.messageType == HelpBoxMessageType.Error);
            Assert.That(error, Is.Not.Null);
            Assert.That(error.text, Is.EqualTo(SceneObjectErrorMessage));
            Assert.That(error.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void Validation_RejectsPrefabAssetsAndAcceptsPrefabStageObjects()
        {
            GameObject contents = null;
            var openedStage = false;
            string path = null;
            try
            {
                var host = CreateHost();
                var prefab = helper.CreatePrefabAsset(host.gameObject, "_AlchemySceneObjectsOnly");
                path = AssetDatabase.GetAssetPath(prefab);
                Assert.That(prefab, Is.Not.Null);
                helper.DestroyCreated();

                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(asset, Is.Not.Null);
                Assert.That(EditorUtility.IsPersistent(asset), Is.True);
                Assert.That(SceneObjectsOnlyValidation.IsValid(asset), Is.False);
                Assert.That(SceneObjectsOnlyValidation.IsValid(asset.GetComponent<SceneObjectsOnlyHost>()), Is.False);

                var sceneInstance = helper.Track(PrefabUtility.InstantiatePrefab(asset) as GameObject);
                Assert.That(EditorUtility.IsPersistent(sceneInstance), Is.False);
                Assert.That(SceneObjectsOnlyValidation.IsValid(sceneInstance), Is.True);

                contents = PrefabUtility.LoadPrefabContents(path);
                Assert.That(EditorUtility.IsPersistent(contents), Is.False);
                Assert.That(SceneObjectsOnlyValidation.IsValid(contents), Is.True);

                var stage = PrefabStageUtility.OpenPrefab(path);
                if (stage == null)
                    return;

                openedStage = true;
                Assert.That(SceneObjectsOnlyValidation.IsValid(stage.prefabContentsRoot), Is.True);
            }
            finally
            {
                if (openedStage)
                    StageUtility.GoToMainStage();
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        SceneObjectsOnlyHost CreateHost(string name = "Owner")
        {
            var host = helper.CreateHost<SceneObjectsOnlyHost>(name);
            host.nested = new SceneObjectsOnlyHost.Nested();
            return host;
        }

        (SceneObjectsOnlyHost hostA, SceneObjectsOnlyHost hostB) TwoHosts() =>
            (CreateHost("OwnerA"), CreateHost("OwnerB"));
    }
}
