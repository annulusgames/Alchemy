using System;
using System.Collections;
using System.Linq;
using Alchemy.Editor.Elements;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class PrefabConditionalDrawerTest
    {
        readonly ObjectReferenceValidationTestHelper helper = new ObjectReferenceValidationTestHelper();

        [TearDown]
        public void TearDown() => helper.Dispose();

        [TestCase(PrefabKind.NonPrefabInstance)]
        [TestCase(PrefabKind.Regular)]
        [TestCase(PrefabKind.Variant)]
        [TestCase(PrefabKind.InstanceInScene)]
        [TestCase(PrefabKind.InstanceInPrefab)]
        [TestCase(PrefabKind.None)]
        public void Conditions_MatchEveryFlagCombination(PrefabKind context)
        {
            var target = CreateTarget(context);
            using var serialized = new SerializedObject(target);
            for (var flags = 0; flags <= (int)PrefabKind.All; flags++)
            {
                var mask = (PrefabKind)flags;
                var matches = (mask & context) != 0;
                AssertState(Wrap(serialized, new ShowInAttribute(mask)), matches, true);
                AssertState(Wrap(serialized, new HideInAttribute(mask)), !matches, true);
                AssertState(Wrap(serialized, new EnableInAttribute(mask)), true, matches);
                AssertState(Wrap(serialized, new DisableInAttribute(mask)), true, !matches);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Conditions_ApplyToEverySelectedOwner(bool reverse)
        {
            var scene = CreateTarget(PrefabKind.NonPrefabInstance);
            var asset = CreateTarget(PrefabKind.Regular);
            using var serialized = new SerializedObject(reverse ? new[] { asset, scene } : new[] { scene, asset });
            AssertState(Wrap(serialized, new ShowInAttribute(PrefabKind.NonPrefabInstance)), false, true);
            AssertState(Wrap(serialized, new EnableInAttribute(PrefabKind.NonPrefabInstance)), true, false);
            AssertState(Wrap(serialized, new HideInAttribute(PrefabKind.Regular)), false, true);
            AssertState(Wrap(serialized, new DisableInAttribute(PrefabKind.Regular)), true, false);
            AssertState(Wrap(serialized, new ShowInAttribute(PrefabKind.Regular | PrefabKind.NonPrefabInstance)), true, true);
        }

        [Test]
        public void Inspector_ComposesConditionsAndIncludesDecorationsPropertiesAndMethods()
        {
            helper.ShowInspector(helper.CreateHost<PrefabConditionalHost>());
            AssertState(FieldScope("sceneOnly"), true, true);
            AssertState(FieldScope("hiddenInScene"), false, true);
            AssertState(FieldScope("editableInScene"), true, true);
            AssertState(FieldScope("disabledInScene"), true, false);
            AssertState(FieldScope("hiddenWins"), false, true);
            AssertState(FieldScope("disabledWins"), true, false);
            Assert.That(FieldScope("readOnly")[0].enabledInHierarchy, Is.False);
            AssertState(FieldScope("showIfCannotReveal"), false, true);
            AssertState(FieldScope("enableIfCannotEnable"), true, false);
            Assert.That(FieldScope("hideIfStillApplies")[0].style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(FieldScope("disableIfStillApplies")[0].enabledInHierarchy, Is.False);
            AssertState(FieldScope("decorated"), false, true);
            Assert.That(FieldScope("decorated").Q<HelpBox>(), Is.Not.Null);
            var property = helper.InspectorRoot.Query<GenericField>().ToList()
                .Single(field => field.Q<IntegerField>()?.label == "Reflected Property");
            AssertState(property.parent.parent, true, true);
            var button = helper.InspectorRoot.Q<MethodButton>();
            Assert.That(button, Is.Not.Null);
            Assert.That(button.enabledInHierarchy, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NestedMembers_UseInspectedRoots(bool mixedSelection)
        {
            var host = helper.CreateHost<PrefabConditionalHost>();
            if (mixedSelection)
            {
                var asset = helper.CreatePrefabAsset(host.gameObject).GetComponent<PrefabConditionalHost>();
                helper.ShowInspector(host, helper.InstantiatePrefab(asset.gameObject).GetComponent<PrefabConditionalHost>());
            }
            else
            {
                helper.ShowInspector(host);
            }

            ExpandFoldout("Reflected Nested");
            var reflectedFields = helper.InspectorRoot.Query<IntegerField>().ToList()
                .Where(field => field.label == "Nested Scene Only" || field.label == "Nested Editable In Scene")
                .ToList();
            Assert.That(reflectedFields.Count, Is.EqualTo(2));
            foreach (var field in reflectedFields)
            {
                var scope = field.GetFirstAncestorOfType<PrefabConditionalElement>();
                Assert.That(scope, Is.Not.Null);
                AssertState(scope,
                    field.label != "Nested Scene Only" || !mixedSelection,
                    field.label != "Nested Editable In Scene" || !mixedSelection);
            }
            ExpandFoldout("Serialized Nested");
            AssertState(FieldScope("serializedNested.nestedSceneOnly"), !mixedSelection, true);
            AssertState(FieldScope("serializedNested.nestedEditableInScene"), true, !mixedSelection);
        }

        [UnityTest]
        public IEnumerator Conditions_RefreshAfterUnpackAndReattachment()
        {
            var asset = helper.CreatePrefabAsset(helper.CreateHost<PrefabConditionalHost>().gameObject);
            var instance = helper.InstantiatePrefab(asset);
            helper.ShowInspector(instance.GetComponent<PrefabConditionalHost>());
            var scope = FieldScope("sceneOnly");
            AssertState(scope, false, true);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            yield return null;
            AssertState(scope, true, true);

            var root = helper.InspectorRoot;
            root.RemoveFromHierarchy();
            helper.Window.rootVisualElement.Add(root);
            yield return null;
            AssertState(scope, true, true);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Conditions_UsePrefabStageAssetContext(bool variant)
        {
            var asset = helper.CreatePrefabAsset(helper.CreateHost<PrefabConditionalHost>().gameObject);
            if (variant) asset = helper.CreatePrefabVariant(asset);
            var stage = helper.OpenPrefab(asset);
            var host = stage.prefabContentsRoot.GetComponent<PrefabConditionalHost>();
            using var serialized = new SerializedObject(host);
            AssertState(Wrap(serialized, new ShowInAttribute(variant ? PrefabKind.Variant : PrefabKind.Regular)), true, true);
            AssertState(Wrap(serialized, new EnableInAttribute(PrefabKind.InstanceInScene)), true, false);
        }

        [Test]
        public void Conditions_CanAttachAfterSerializedObjectIsDisposed()
        {
            var host = helper.CreateHost<PrefabConditionalHost>();
            VisualElement scope;
            using (var serialized = new SerializedObject(host))
                scope = Wrap(serialized, new ShowInAttribute(PrefabKind.NonPrefabInstance));
            helper.ShowInspector(host);
            helper.Window.rootVisualElement.Add(scope);
            AssertState(scope, true, true);
            scope.RemoveFromHierarchy();
            helper.Window.rootVisualElement.Add(scope);
            AssertState(scope, true, true);
        }

        UnityEngine.Object CreateTarget(PrefabKind kind)
        {
            if (kind == PrefabKind.None)
                return helper.Track(ScriptableObject.CreateInstance<ContextlessTarget>());
            var scene = helper.CreateHost<PrefabConditionalHost>();
            if (kind == PrefabKind.NonPrefabInstance) return scene;
            var asset = helper.CreatePrefabAsset(scene.gameObject);
            return kind switch
            {
                PrefabKind.Regular => asset.GetComponent<PrefabConditionalHost>(),
                PrefabKind.Variant => helper.CreatePrefabVariant(asset).GetComponent<PrefabConditionalHost>(),
                PrefabKind.InstanceInScene => helper.InstantiatePrefab(asset).GetComponent<PrefabConditionalHost>(),
                PrefabKind.InstanceInPrefab => helper.CreateNestedPrefabAsset(asset).GetComponentInChildren<PrefabConditionalHost>(),
                _ => throw new ArgumentOutOfRangeException(nameof(kind)),
            };
        }

        static VisualElement Wrap(SerializedObject serialized, Attribute attribute)
        {
            var parent = new VisualElement();
            var field = new VisualElement();
            parent.Add(field);
            PrefabConditionalElement.Wrap(serialized, new object(), new[] { attribute }, field);
            return parent[0];
        }

        void ExpandFoldout(string text)
        {
            var foldout = helper.InspectorRoot.Query<Foldout>().ToList().Single(element => element.text == text);
            foldout.value = true;
        }

        VisualElement FieldScope(string name)
        {
            var field = helper.InspectorRoot.Query<AlchemyPropertyField>().ToList()
                .Single(element => (element.FieldElement as PropertyField)?.bindingPath == name);
            Assert.That(field.parent, Is.TypeOf<PrefabConditionalElement>());
            return field.parent;
        }

        static void AssertState(VisualElement scope, bool visible, bool enabled)
        {
            Assert.That(scope.style.display.value, Is.EqualTo(visible ? DisplayStyle.Flex : DisplayStyle.None));
            Assert.That(scope.enabledInHierarchy, Is.EqualTo(enabled));
        }

        sealed class ContextlessTarget : ScriptableObject { }
    }
}
