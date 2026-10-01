using Alchemy.Editor;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class PrefabKindUtilityTest
    {
        readonly ObjectReferenceValidationTestHelper helper = new ObjectReferenceValidationTestHelper();

        [TearDown]
        public void TearDown() => helper.Dispose();

        [Test]
        public void PrefabKind_CompositeFlagsMatchPrimitives()
        {
            Assert.That(PrefabKind.PrefabAsset, Is.EqualTo(PrefabKind.Regular | PrefabKind.Variant));
            Assert.That(PrefabKind.PrefabInstance, Is.EqualTo(PrefabKind.InstanceInPrefab | PrefabKind.InstanceInScene));
            Assert.That(
                PrefabKind.PrefabInstanceAndNonPrefabInstance,
                Is.EqualTo(PrefabKind.PrefabInstance | PrefabKind.NonPrefabInstance));
            Assert.That(
                PrefabKind.All,
                Is.EqualTo(PrefabKind.PrefabAsset | PrefabKind.PrefabInstanceAndNonPrefabInstance));
            Assert.That((PrefabKind.PrefabAsset & PrefabKind.Regular) != 0, Is.True);
            Assert.That((PrefabKind.None & PrefabKind.All) != 0, Is.False);
        }

        [Test]
        public void GetPrefabKind_NullAndNonHierarchyObjects_AreNone()
        {
            var scriptable = helper.Track(ScriptableObject.CreateInstance<SceneObjectsOnlyScriptable>());

            AssertKind(null, PrefabKind.None);
            AssertKind(scriptable, PrefabKind.None);
            AssertKind(Texture2D.whiteTexture, PrefabKind.None);
        }

        [Test]
        public void GetPrefabKind_RecomputesAfterSceneObjectIsConnectedToPrefab()
        {
            var scene = helper.Create("Scene");
            AssertKind(scene, PrefabKind.NonPrefabInstance);

            var path = $"Assets/_AlchemyPrefabKindConnect_{System.Guid.NewGuid():N}.prefab";
            try
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(scene, path, InteractionMode.AutomatedAction);
                AssertKind(scene, PrefabKind.InstanceInScene);
                AssertKind(scene, PrefabKind.InstanceInScene);
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void GetPrefabKind_NonPrefabSceneObject_IsNonPrefabInstance()
        {
            var gameObject = helper.Create("Scene");

            AssertKind(gameObject, PrefabKind.NonPrefabInstance);
            AssertKind(gameObject.transform, PrefabKind.NonPrefabInstance);
        }

        [Test]
        public void GetPrefabKind_RegularPrefabAsset_IsRegular()
        {
            var prefab = helper.CreatePrefabAsset("Regular", "_AlchemyPrefabKindRegular");

            Assert.That(PrefabUtility.GetPrefabAssetType(prefab), Is.EqualTo(PrefabAssetType.Regular));
            AssertKind(prefab, PrefabKind.Regular);
            AssertKind(prefab.transform, PrefabKind.Regular);
        }

        [Test]
        public void GetPrefabKind_VariantPrefabAsset_IsVariant()
        {
            var variant = helper.CreatePrefabVariant("Base", "_AlchemyPrefabKindVariant");

            Assert.That(PrefabUtility.GetPrefabAssetType(variant), Is.EqualTo(PrefabAssetType.Variant));
            AssertKind(variant, PrefabKind.Variant);
        }

        [Test]
        public void GetPrefabKind_ModelPrefabAsset_IsNone()
        {
            var model = helper.CreateModelPrefabAsset("_AlchemyPrefabKindModel");

            Assert.That(PrefabUtility.GetPrefabAssetType(model), Is.EqualTo(PrefabAssetType.Model));
            AssertKind(model, PrefabKind.None);
        }

        [Test]
        public void GetPrefabKind_PrefabInstanceInScene_IsInstanceInScene()
        {
            var instance = helper.InstantiatePrefab(
                helper.CreatePrefabAsset("Regular", "_AlchemyPrefabKindScene"));

            AssertKind(instance, PrefabKind.InstanceInScene);
            AssertKind(instance.transform, PrefabKind.InstanceInScene);
        }

        [Test]
        public void GetPrefabKind_NestedPrefabInstance_IsInstanceInPrefab()
        {
            var outerPrefab = helper.CreateNestedPrefabAsset("_AlchemyPrefabKindOuter");
            var sceneOuter = helper.InstantiatePrefab(outerPrefab);

            AssertKind(outerPrefab, PrefabKind.Regular);
            AssertKind(FirstChild(outerPrefab), PrefabKind.InstanceInPrefab);
            AssertKind(sceneOuter, PrefabKind.InstanceInScene);
            AssertKind(FirstChild(sceneOuter), PrefabKind.InstanceInPrefab);
        }

        [Test]
        public void GetPrefabKind_ModelPrefabInstanceInScene_IsInstanceInScene()
        {
            AssertKind(
                helper.InstantiatePrefab(helper.CreateModelPrefabAsset("_AlchemyPrefabKindModelInstance")),
                PrefabKind.InstanceInScene);
        }

        [Test]
        public void GetPrefabKind_LoadPrefabContents_UsesAssetKindNotNonPrefabInstance()
        {
            var regularContents = helper.LoadPrefabContents(
                helper.CreatePrefabAsset("Regular", "_AlchemyPrefabKindLoadRegular"));
            var variantContents = helper.LoadPrefabContents(
                helper.CreatePrefabVariant("Base", "_AlchemyPrefabKindLoadVariant"));
            var nestedContents = helper.LoadPrefabContents(
                helper.CreateNestedPrefabAsset("_AlchemyPrefabKindLoadOuter"));
            var nestedVariantContents = helper.LoadPrefabContents(
                helper.CreateNestedPrefabAsset(
                    helper.CreatePrefabVariant("NestedBase", "_AlchemyPrefabKindLoadNestedVariant"),
                    "_AlchemyPrefabKindLoadRegularVariant"));

            Assert.That(EditorUtility.IsPersistent(regularContents), Is.False);
            Assert.That(EditorUtility.IsPersistent(variantContents), Is.False);
            AssertKind(regularContents, PrefabKind.Regular);
            AssertKind(variantContents, PrefabKind.Variant);
            AssertKind(nestedContents, PrefabKind.Regular);
            AssertKind(FirstChild(nestedContents), PrefabKind.InstanceInPrefab);
            AssertKind(nestedVariantContents, PrefabKind.Regular);
            AssertKind(FirstChild(nestedVariantContents), PrefabKind.InstanceInPrefab);
        }

        [Test]
        public void GetPrefabKind_PrefabStage_UsesAssetKindAndNestedInstance()
        {
            var regularStage = helper.OpenPrefab(
                helper.CreateNestedPrefabAsset("_AlchemyPrefabKindStageRegular"));
            if (regularStage == null)
                return;

            var regularRoot = regularStage.prefabContentsRoot;
            Assert.That(EditorUtility.IsPersistent(regularRoot), Is.False);
            AssertKind(regularRoot, PrefabKind.Regular);
            AssertKind(FirstChild(regularRoot), PrefabKind.InstanceInPrefab);

            var variantStage = helper.OpenPrefab(
                helper.CreatePrefabVariant("Base", "_AlchemyPrefabKindStageVariant"));
            if (variantStage == null)
                return;

            AssertKind(variantStage.prefabContentsRoot, PrefabKind.Variant);
        }

        [Test]
        public void GetPrefabKind_VariantContainingItsOwnBase_DistinguishesNestedInstance()
        {
            var baseAsset = helper.CreatePrefabAsset("Base", "_AlchemySameBase");
            var variantSource = helper.InstantiatePrefab(baseAsset);
            helper.InstantiatePrefab(baseAsset).transform.SetParent(variantSource.transform);
            var variant = helper.CreatePrefabAsset(variantSource, "_AlchemySameBaseVariant");

            AssertKind(variant, PrefabKind.Variant);
            AssertKind(FirstChild(variant), PrefabKind.InstanceInPrefab);
            AssertKind(FirstChild(variant).transform, PrefabKind.InstanceInPrefab);

            var contents = helper.LoadPrefabContents(variant);
            AssertKind(contents, PrefabKind.Variant);
            AssertKind(FirstChild(contents), PrefabKind.InstanceInPrefab);

            var stage = helper.OpenPrefab(variant);
            Assert.That(stage, Is.Not.Null);
            AssertKind(stage.prefabContentsRoot, PrefabKind.Variant);
            AssertKind(FirstChild(stage.prefabContentsRoot), PrefabKind.InstanceInPrefab);
        }

        static GameObject FirstChild(GameObject parent) => parent.transform.GetChild(0).gameObject;

        static void AssertKind(UnityEngine.Object target, PrefabKind expected) =>
            Assert.That(PrefabKindUtility.GetPrefabKind(target), Is.EqualTo(expected));
    }
}
