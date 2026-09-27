using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alchemy.Editor;
using Alchemy.Editor.Drawers;
using Alchemy.Editor.Elements;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class ValueDropdownTest
    {
        readonly ObjectReferenceValidationTestHelper helper = new ObjectReferenceValidationTestHelper();
        EditorWindow window;

        [TearDown]
        public void TearDown()
        {
            if (window != null) window.Close();
            window = null;
            helper.Dispose();
        }

        class BaseProvider
        {
            int[] Inherited => new[] { 7, 8 };
        }

        class Provider : BaseProvider
        {
            public int[] Values = { 1, 2 };
            public int[] Property => Values;
            public int[] Method() => Values;
            public static int[] StaticValues => new[] { 3, 4 };
            public int[] Contextual(ValueDropdownContext context) =>
                new[] { (int)context.CurrentValue, context.Index, context.IsAdding ? 1 : 0 };
            public string Invalid => "invalid";
            public int[] InvalidMethod(int value) => new[] { value };
            public int[] Ambiguous() => Values;
            public int[] Ambiguous(ValueDropdownContext context) => Values;
            public int[] Throws() => throw new InvalidOperationException("provider failed");
        }

        [TestCase("Values", 1)]
        [TestCase("Property", 1)]
        [TestCase("Method", 1)]
        [TestCase("Inherited", 7)]
        [TestCase("StaticValues", 3)]
        public void Source_ResolvesFieldsPropertiesMethodsAndPrivateBaseMembers(string member, int first)
        {
            var owner = new Provider();
            var snapshot = ValueDropdownSource.Get(new ValueDropdownAttribute(member), typeof(int),
                new ValueDropdownContext(null, owner, 0, -1, false));
            Assert.That(snapshot.Count, Is.EqualTo(2));
            Assert.That(snapshot.GetValue(0), Is.EqualTo(first));
        }

        [Test]
        public void Source_PassesContextAndReevaluatesCachedProvider()
        {
            var owner = new Provider();
            var context = new ValueDropdownContext(new object(), owner, 9, 2, true);
            var snapshot = ValueDropdownSource.Get(new ValueDropdownAttribute("Contextual"), typeof(int), context);
            Assert.That(new[] { snapshot.GetValue(0), snapshot.GetValue(1), snapshot.GetValue(2) },
                Is.EqualTo(new[] { 9, 2, 1 }));
            var attribute = new ValueDropdownAttribute("Values");
            Assert.That(ValueDropdownSource.Get(attribute, typeof(int), context).GetValue(0), Is.EqualTo(1));
            owner.Values = new[] { 42 };
            Assert.That(ValueDropdownSource.Get(attribute, typeof(int), context).GetValue(0), Is.EqualTo(42));
            var other = new Provider();
            Assert.That(ValueDropdownSource.Get(attribute, typeof(int),
                new ValueDropdownContext(null, other, null, -1, false)).GetValue(0), Is.EqualTo(1));
        }

        [Test]
        public void Source_ExplicitSourceTypeWorksWithoutOwnerAndRequiresStaticMember()
        {
            var attribute = new ValueDropdownAttribute(typeof(Provider), "StaticValues");
            Assert.That(ValueDropdownSource.Get(attribute, typeof(int), default).GetValue(0), Is.EqualTo(3));
            Assert.Throws<InvalidOperationException>(() => ValueDropdownSource.Get(
                new ValueDropdownAttribute(typeof(Provider), "Values"), typeof(int), default));
        }

        [TestCase("")]
        [TestCase("Missing")]
        [TestCase("Invalid")]
        [TestCase("InvalidMethod")]
        [TestCase("Ambiguous")]
        [TestCase("Throws")]
        public void Source_RejectsInvalidProviders(string member)
        {
            Assert.Throws<InvalidOperationException>(() => ValueDropdownSource.Get(
                new ValueDropdownAttribute(member), typeof(int),
                new ValueDropdownContext(null, new Provider(), null, -1, false)));
        }

        [Test]
        public void Snapshot_PreservesLabelsTooltipsDisabledChoicesAndNull()
        {
            var items = new ValueDropdownList<string>
            {
                new ValueDropdownItem<string>("Disabled", "a", "Unavailable", false),
                new ValueDropdownItem<string>("Group/Available", "a", "Choose this"),
                new ValueDropdownItem<string>(null, null),
            };
            var snapshot = new ValueDropdownSnapshot<string>(items);
            Assert.That(snapshot.Entries[0].Enabled, Is.False);
            Assert.That(snapshot.Entries[0].Tooltip, Is.EqualTo("Unavailable"));
            Assert.That(snapshot.Entries[1].Text, Is.EqualTo("Group/Available"));
            Assert.That(snapshot.Find("a"), Is.EqualTo(1));
            Assert.That(snapshot.Find(null), Is.EqualTo(2));
            Assert.That(snapshot.Entries[2].Text, Is.EqualTo("(Null)"));
            Assert.That(snapshot.Find("missing"), Is.EqualTo(-1));
        }

        [Test]
        public void Snapshot_CustomComparerControlsMatchingAndUniqueness()
        {
            var items = new ValueDropdownList<string> { Comparer = StringComparer.OrdinalIgnoreCase };
            items.Add("First", "one");
            items.Add("Second", "two");
            var snapshot = new ValueDropdownSnapshot<string>(items);
            Assert.That(snapshot.Find("ONE"), Is.EqualTo(0));
            Assert.That(snapshot.Equal("two", "TWO"), Is.True);
            Assert.That(snapshot.ExistingValues(new[] { "ONE", "TWO" }, 0)("one"), Is.False);
            Assert.That(snapshot.ExistingValues(new[] { "ONE", "TWO" }, 0)("two"), Is.True);
            Assert.DoesNotThrow(() => snapshot.ValidateUnique(new[] { "ONE" }, 0, new object[] { "one" }));
            Assert.Throws<InvalidOperationException>(() => snapshot.ValidateUnique(new[] { "ONE" }, -1, new object[] { "one" }));
            Assert.Throws<InvalidOperationException>(() => snapshot.ValidateUnique(null, -1, new object[] { "two", "TWO" }));
        }

        [Test]
        public void Snapshot_FactoryRunsOnlyWhenCreatingSelectionAndCreatesIndependentValues()
        {
            var calls = 0;
            var candidate = new List<int> { 1 };
            var items = new ValueDropdownList<List<int>> { ValueFactory = value => { calls++; return new List<int>(value); } };
            items.Add("Candidate", candidate);
            var snapshot = new ValueDropdownSnapshot<List<int>>(items);
            Assert.That(snapshot.GetValue(0), Is.SameAs(candidate));
            Assert.That(snapshot.Find(candidate), Is.EqualTo(0));
            Assert.That(calls, Is.Zero);
            var first = (List<int>)snapshot.CreateValue(0);
            var second = (List<int>)snapshot.CreateValue(0);
            first.Add(2);
            Assert.That(second, Is.EqualTo(new[] { 1 }));
            Assert.That(candidate, Is.EqualTo(new[] { 1 }));
            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void Snapshot_UntypedItemsValidateCandidateTypes()
        {
            var snapshot = new ValueDropdownSnapshot<int>(new ArrayList { 1, new ValueDropdownItem<int>("Two", 2) });
            Assert.That(snapshot.GetValue(1), Is.EqualTo(2));
            Assert.That(snapshot.Entries[1].Text, Is.EqualTo("Two"));
            Assert.That(new ValueDropdownSnapshot<int>(null).Count, Is.Zero);
            Assert.Throws<ArgumentException>(() => new ValueDropdownSnapshot<int>("text"));
            Assert.Throws<ArgumentException>(() => new ValueDropdownSnapshot<int>(new ArrayList { 1, "wrong" }));
            Assert.Throws<ArgumentException>(() => new ValueDropdownSnapshot<int>(new ArrayList { null }));
        }

        [Test]
        public void Binding_ReflectedCollectionSupportsSetAppendMoveAndRemove()
        {
            var owner = new Provider();
            var original = owner.Values;
            var writes = 0;
            var changes = 0;
            var binding = new ValueDropdownBinding(owner, typeof(Provider).GetField("Values"), typeof(int[]),
                () => owner.Values, value => owner.Values = (int[])value, () => writes++, null);
            binding.Changed += () => changes++;
            binding.Set(0, new object[] { 3 });
            // Element writes keep the array; resizing assigns a new one.
            Assert.That(owner.Values, Is.SameAs(original));
            binding.Append(new[] { new object[] { 4, 5 } });
            binding.Move(3, 1);
            binding.Remove(new[] { 2 });
            Assert.That(owner.Values, Is.EqualTo(new[] { 3, 5, 4 }));
            Assert.That(original, Is.EqualTo(new[] { 3, 2 }));
            Assert.That(writes, Is.EqualTo(4));
            Assert.That(changes, Is.EqualTo(4));
            Assert.That(binding.Revision, Is.EqualTo(4));
            Assert.Throws<ArgumentException>(() => binding.Set(0, new object[] { "wrong" }));
            Assert.Throws<InvalidOperationException>(() => binding.Set(3, new object[] { 0 }));
            Assert.That(owner.Values, Is.EqualTo(new[] { 3, 5, 4 }));
        }

        [Test]
        public void Binding_ReadOnlyScalarRejectsMutation()
        {
            var owner = new ScalarOwner();
            var binding = new ValueDropdownBinding(owner, typeof(ScalarOwner).GetField("value"), typeof(int),
                () => owner.value, null, null, null);
            Assert.That(binding.CanWrite, Is.False);
            Assert.Throws<InvalidOperationException>(() => binding.Set(-1, new object[] { 3 }));
            Assert.That(owner.value, Is.EqualTo(1));
        }

        [Test]
        public void Binding_ReadOnlyCollectionsAreEditedInPlace()
        {
            var owner = new ReadOnlyCollections();
            var list = owner.Tags;
            var changes = new List<object>();
            var binding = new ValueDropdownBinding(owner, typeof(ReadOnlyCollections).GetProperty("Tags"), typeof(List<string>),
                () => owner.Tags, null, null, changes.Add);
            Assert.That(binding.CanWrite, Is.True);
            Assert.That(binding.CanResize, Is.True);
            binding.Set(0, new object[] { "b" });
            binding.Append(new[] { new object[] { "c" } });
            binding.AppendDefault();
            binding.Move(2, 0);
            binding.Remove(new[] { 1 });
            Assert.That(owner.Tags, Is.SameAs(list));
            Assert.That(list, Is.EqualTo(new[] { null, "c" }));
            Assert.That(changes, Has.Count.EqualTo(5));
            Assert.That(changes, Has.All.SameAs(list));

            var array = owner.fixedTags;
            var arrayBinding = new ValueDropdownBinding(owner, typeof(ReadOnlyCollections).GetField("fixedTags"), typeof(string[]),
                () => owner.fixedTags, null, null, null);
            Assert.That(arrayBinding.CanWrite, Is.True);
            Assert.That(arrayBinding.CanResize, Is.False);
            arrayBinding.Set(1, new object[] { "c" });
            arrayBinding.Move(1, 0);
            Assert.That(array, Is.EqualTo(new[] { "c", "a" }));
            Assert.Throws<InvalidOperationException>(() => arrayBinding.Append(new[] { new object[] { "d" } }));
            Assert.Throws<InvalidOperationException>(() => arrayBinding.Remove(new[] { 0 }));
            Assert.That(array, Is.EqualTo(new[] { "c", "a" }));

            // A getter that builds a new list on each read would drop the edits.
            var computed = new ValueDropdownBinding(owner, typeof(ReadOnlyCollections).GetProperty("Computed"), typeof(List<string>),
                () => owner.Computed, null, null, null);
            Assert.That(computed.CanWrite, Is.False);
            Assert.Throws<InvalidOperationException>(() => computed.Set(0, new object[] { "b" }));
        }

        [Test]
        public void Binding_DisplayTextUsesProviderLabelsOncePerValue()
        {
            var owner = new LabelOwner();
            var attribute = new ValueDropdownAttribute(nameof(LabelOwner.Weapons));
            var binding = new ValueDropdownBinding(owner, typeof(LabelOwner).GetField("weapon"), typeof(int),
                () => owner.weapon, value => owner.weapon = (int)value, null, null);
            Assert.That(binding.DisplayText(-1, attribute), Is.EqualTo("Melee/Sword"));
            Assert.That(binding.DisplayText(-1, attribute), Is.EqualTo("Melee/Sword"));
            Assert.That(owner.evaluations, Is.EqualTo(1));
            owner.weapon = 2;
            Assert.That(binding.DisplayText(-1, attribute), Is.EqualTo("Melee/Axe"));
            Assert.That(owner.evaluations, Is.EqualTo(2));
            binding.Set(-1, new object[] { 1 }, "Chosen");
            Assert.That(binding.DisplayText(-1, attribute), Is.EqualTo("Chosen"));
            owner.weapon = 7;
            Assert.That(binding.DisplayText(-1, attribute), Is.EqualTo("7"));
            owner.fail = true;
            owner.weapon = 2;
            Assert.That(binding.DisplayText(-1, attribute), Is.EqualTo("2"));
        }

        [Test]
        public void Snapshot_FindsCurrentValueWhoseOnlyEntryIsDisabled()
        {
            var items = new ValueDropdownList<int>();
            items.Add("Bow", 3, "Locked", false);
            items.Add("Sword", 1);
            items.Add("Sword (locked)", 1, null, false);
            var snapshot = new ValueDropdownSnapshot<int>(items);
            Assert.That(snapshot.Find(3), Is.EqualTo(0));
            Assert.That(snapshot.Find(1), Is.EqualTo(1));
            Assert.That(snapshot.Find(4), Is.EqualTo(-1));
            var nulls = new ValueDropdownSnapshot<string>(new ValueDropdownList<string> { { "None", null, null, false } });
            Assert.That(nulls.Find(null), Is.EqualTo(0));
        }

        [Test]
        public void Snapshot_DerivedOptionsKeepComparerAndFactory()
        {
            var candidate = new FireEffect { power = 3 };
            var options = new ValueDropdownList<FireEffect>
            {
                Comparer = new PowerComparer(),
                ValueFactory = effect => new FireEffect { power = effect.power },
            };
            options.Add("Fire", candidate);
            var snapshot = new ValueDropdownSnapshot<Effect>(options);
            var created = (Effect)snapshot.CreateValue(0);
            Assert.That(created, Is.Not.SameAs(candidate));
            Assert.That(created, Is.TypeOf<FireEffect>());
            Assert.That(created.power, Is.EqualTo(3));
            Assert.That(snapshot.Find(new FireEffect { power = 3 }), Is.EqualTo(0));
            Assert.That(snapshot.Find(new Effect { power = 3 }), Is.EqualTo(-1));
            Assert.That(snapshot.Entries[0].Text, Is.EqualTo("Fire"));
        }

        [Test]
        public void Callbacks_MatchParameterlessAndCompatibleSingleArgumentMethods()
        {
            var callbacks = OnValueChangedDrawer.FindCallbacks(typeof(CallbackOwner), nameof(CallbackOwner.Changed), typeof(int));
            Assert.That(callbacks.Select(x => x.GetParameters().Length), Is.EquivalentTo(new[] { 0, 1, 1 }));
            var owner = new CallbackOwner();
            var reads = 0;
            OnValueChangedDrawer.InvokeCallbacks(owner, callbacks, () => { reads++; return 5; });
            Assert.That(owner.calls, Is.EquivalentTo(new object[] { "none", 5, 5 }));
            Assert.That(reads, Is.EqualTo(1));
        }

        [Test]
        public void InternalAPI_ReadsCompositionStringWithoutLegacyInput()
        {
            Assert.That(typeof(GUIUtility).GetProperty("compositionString", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static), Is.Not.Null);
            Assert.That(InternalAPIHelper.GetCompositionString(), Is.Not.Null);
        }

        [Test]
        public void PropertyField_SkipsPropertiesWithoutManagedFields()
        {
            var gameObject = new GameObject("Native");
            try
            {
                using var serialized = new SerializedObject(gameObject);
                using var property = serialized.FindProperty("m_IsActive");
                Assert.That(property.GetFieldInfo(), Is.Null);
                Assert.DoesNotThrow(() => new AlchemyPropertyField(property, typeof(bool)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Binding_SerializedMultiObjectEditsValidateAllTargetsAndPreservePendingChanges()
        {
            var first = ScriptableObject.CreateInstance<ValueDropdownTestHost>();
            var second = ScriptableObject.CreateInstance<ValueDropdownTestHost>();
            try
            {
                first.values = new[] { 1 };
                second.values = new[] { 2, 3 };
                using var serialized = new SerializedObject(new UnityEngine.Object[] { first, second });
                var binding = new ValueDropdownBinding(serialized.FindProperty("values"),
                    typeof(ValueDropdownTestHost).GetField("values"), typeof(int[]));
                Assert.That(binding.Count, Is.EqualTo(1));
                Assert.That(binding.IsMixed(0), Is.True);
                Assert.Throws<ArgumentException>(() => binding.Set(0, new object[] { 7, "wrong" }));
                Assert.That(first.values, Is.EqualTo(new[] { 1 }));
                Assert.That(second.values, Is.EqualTo(new[] { 2, 3 }));
                serialized.FindProperty("other").intValue = 9;
                binding.Set(0, new object[] { 7, 8 });
                binding.Append(new[] { new object[] { 4 }, new object[] { 5, 6 } });
                Assert.That(first.values, Is.EqualTo(new[] { 7, 4 }));
                Assert.That(second.values, Is.EqualTo(new[] { 8, 3, 5, 6 }));
                Assert.That(first.other, Is.EqualTo(9));
                Assert.That(second.other, Is.EqualTo(9));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void Binding_SerializedAppendDefaultDoesNotCopyTheLastElement()
        {
            var host = ScriptableObject.CreateInstance<ValueDropdownTestHost>();
            try
            {
                host.tags = new List<string> { "A" };
                host.items = new List<ValueDropdownTestHost.Item> { new ValueDropdownTestHost.Item { number = 5, name = "x" } };
                using var serialized = new SerializedObject(host);
                new ValueDropdownBinding(serialized.FindProperty("tags"), typeof(ValueDropdownTestHost).GetField("tags"), typeof(List<string>)).AppendDefault();
                new ValueDropdownBinding(serialized.FindProperty("items"), typeof(ValueDropdownTestHost).GetField("items"), typeof(List<ValueDropdownTestHost.Item>)).AppendDefault();
                Assert.That(host.tags, Is.EqualTo(new[] { "A", "" }));
                Assert.That(host.items, Has.Count.EqualTo(2));
                Assert.That(host.items[1].number, Is.Zero);
                Assert.That(host.items[1].name, Is.Empty);
                Assert.That(host.items[0].number, Is.EqualTo(5));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Binding_MixedValuesCompareSerializedContentAndProviderComparer()
        {
            var first = ScriptableObject.CreateInstance<ValueDropdownTestHost>();
            var second = ScriptableObject.CreateInstance<ValueDropdownTestHost>();
            try
            {
                first.payload = new ValueDropdownTestHost.Item { number = 1, name = "same" };
                second.payload = new ValueDropdownTestHost.Item { number = 1, name = "same" };
                first.title = "one";
                second.title = "ONE";
                using var serialized = new SerializedObject(new UnityEngine.Object[] { first, second });
                var payload = new ValueDropdownBinding(serialized.FindProperty("payload"),
                    typeof(ValueDropdownTestHost).GetField("payload"), typeof(ValueDropdownTestHost.Item));
                Assert.That(payload.IsMixed(-1), Is.False);
                second.payload.number = 2;
                using (var different = new SerializedObject(new UnityEngine.Object[] { first, second }))
                {
                    var changed = new ValueDropdownBinding(different.FindProperty("payload"),
                        typeof(ValueDropdownTestHost).GetField("payload"), typeof(ValueDropdownTestHost.Item));
                    Assert.That(changed.IsMixed(-1), Is.True);
                }

                var title = new ValueDropdownBinding(serialized.FindProperty("title"),
                    typeof(ValueDropdownTestHost).GetField("title"), typeof(string));
                Assert.That(title.IsMixed(-1), Is.True);
                var session = new ValueDropdownSession(title, new ValueDropdownAttribute(nameof(ValueDropdownTestHost.Titles)), -1, false);
                Assert.That(session.CurrentChoice, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void Session_KeepsChoicesDisabledForAnyTargetVisible()
        {
            var first = ScriptableObject.CreateInstance<ValueDropdownTestHost>();
            var second = ScriptableObject.CreateInstance<ValueDropdownTestHost>();
            try
            {
                first.weapon = "bow";
                second.weapon = "bow";
                second.bowEnabled = false;
                using var serialized = new SerializedObject(new UnityEngine.Object[] { first, second });
                var binding = new ValueDropdownBinding(serialized.FindProperty("weapon"),
                    typeof(ValueDropdownTestHost).GetField("weapon"), typeof(string));
                var session = new ValueDropdownSession(binding, new ValueDropdownAttribute(nameof(ValueDropdownTestHost.Weapons)), -1, false);
                Assert.That(session.Choices, Is.EqualTo(new[] { 0, 1 }));
                Assert.That(session.Enabled, Is.EqualTo(new[] { true, false }));
                Assert.That(session.CurrentChoice, Is.EqualTo(1));
                Assert.Throws<InvalidOperationException>(() => session.Commit(new List<int> { 1 }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void Binding_PrefabOverridesAndRevertAreScopedToTheElement()
        {
            var source = helper.CreateHost<ValueDropdownHost>();
            var asset = helper.CreatePrefabAsset(source.gameObject, "_AlchemyValueDropdown");
            var instance = helper.InstantiatePrefab(asset).GetComponent<ValueDropdownHost>();
            using var serialized = new SerializedObject(instance);
            serialized.FindProperty("numbers").GetArrayElementAtIndex(2).intValue = 30;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
            Assert.That(PrefabUtility.GetPropertyModifications(instance).Select(x => x.propertyPath), Does.Contain("numbers.Array.data[2]"));
            var binding = new ValueDropdownBinding(serialized.FindProperty("numbers"),
                typeof(ValueDropdownHost).GetField("numbers"), typeof(List<int>));
            Assert.That(binding.IsPrefabOverride(2), Is.True);
            Assert.That(binding.IsPrefabOverride(0), Is.False);
            binding.Revert(0);
            Assert.That(instance.numbers[2], Is.EqualTo(30));
            binding.Revert(2);
            Assert.That(instance.numbers[2], Is.EqualTo(3));
            Assert.That(binding.IsPrefabOverride(2), Is.False);
        }

        [UnityTest]
        public IEnumerator Binding_TrackerDoesNotRepeatItsOwnChanges()
        {
            var host = ScriptableObject.CreateInstance<ValueDropdownTestHost>();
            try
            {
                host.values = new[] { 1 };
                using var serialized = new SerializedObject(host);
                var binding = new ValueDropdownBinding(serialized.FindProperty("values"),
                    typeof(ValueDropdownTestHost).GetField("values"), typeof(int[]));
                var element = new VisualElement();
                binding.Track(element);
                window = EditModeEditorTestUtility.ShowInWindow(element);
                for (var i = 0; i < 5; i++) yield return null;
                binding.Set(0, new object[] { 2 });
                Assert.That(binding.Revision, Is.EqualTo(1));
                var deadline = EditorApplication.timeSinceStartup + 0.5;
                while (EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(binding.Revision, Is.EqualTo(1));
                using (var external = new SerializedObject(host))
                {
                    external.FindProperty("values").GetArrayElementAtIndex(0).intValue = 3;
                    external.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => binding.Revision == 2)) yield return wait;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [UnityTest]
        public IEnumerator Element_ReflectedAppendFieldShowsPickerSelections()
        {
            var owner = new ScalarOwner();
            var field = typeof(ScalarOwner).GetField("value");
            var binding = new ValueDropdownBinding(owner, field, typeof(int), () => owner.value, value => owner.value = (int)value, null, null);
            var element = new ValueDropdownElement(binding, new ValueDropdownAttribute(nameof(ScalarOwner.Choices)) { Mode = ValueDropdownMode.Append }, () => -1, "Value");
            window = EditModeEditorTestUtility.ShowInWindow(element);
            yield return null;
            binding.Set(-1, new object[] { 3 });
            Assert.That(element.Q<IntegerField>().value, Is.EqualTo(3));
            // Reflected fields are delayed and commit when they lose focus.
            var edited = element.Q<IntegerField>();
            edited.value = 2;
            using (var focusOut = FocusOutEvent.GetPooled(edited, null, FocusChangeDirection.unspecified, edited.focusController))
                edited.SendEvent(focusOut);
            yield return null;
            Assert.That(owner.value, Is.EqualTo(2));
            Assert.That(element.Q<IntegerField>(), Is.SameAs(edited));
        }

        [UnityTest]
        public IEnumerator Element_ErrorsStayVisibleUntilRefreshSucceeds()
        {
            var owner = new FaultyOwner();
            var attribute = new ValueDropdownAttribute(nameof(FaultyOwner.None));
            var scalar = new ValueDropdownBinding(owner, typeof(FaultyOwner).GetField("value"), typeof(Faulty),
                () => owner.value, value => owner.value = (Faulty)value, null, null);
            var field = new ValueDropdownElement(scalar, attribute, () => -1, "Value");
            var items = new ValueDropdownBinding(owner, typeof(FaultyOwner).GetField("items"), typeof(List<Faulty>),
                () => owner.items, value => owner.items = (List<Faulty>)value, null, null);
            var collection = new ValueDropdownCollectionElement(items, attribute, "Items");
            var root = new VisualElement();
            root.Add(field);
            root.Add(collection);
            owner.value.fail = true;
            window = EditModeEditorTestUtility.ShowInWindow(root);
            HelpBox fieldError = null;
            foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => (fieldError = field.Q<HelpBox>()) != null)) yield return wait;
            var listError = collection.Q<HelpBox>();
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(fieldError, DisplayStyle.Flex)) yield return wait;
            foreach (var wait in ObjectReferenceValidationTestHelper.WaitUntilDisplay(listError, DisplayStyle.Flex)) yield return wait;
            Assert.That(listError.text, Does.Contain("broken"));

            // A later collection refresh rebinds the failing row and must keep its error.
            var revision = items.Revision;
            items.Append(new[] { new object[] { new Faulty() } });
            Assert.That(items.Revision, Is.EqualTo(revision + 1));
            foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => collection.Q<ListView>().itemsSource.Count == 2)) yield return wait;
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(listError.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            owner.value.fail = false;
            field.Refresh();
            Assert.That(fieldError.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator Inspector_RemoveSelectedClearsSelectionAndAppendHonorsLabelWidth()
        {
            var host = helper.CreateHost<ValueDropdownHost>();
            helper.ShowInspector(host);
            ValueDropdownCollectionElement collection = null;
            foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => (collection = helper.InspectorRoot.Q<ValueDropdownCollectionElement>()) != null)) yield return wait;
            var list = collection.Q<ListView>();
            foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => list.itemsSource.Count == 3)) yield return wait;
            list.SetSelection(1);
            var remove = collection.Query<Button>().ToList().Single(x => x.text == "Remove selected");
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = remove;
                remove.SendEvent(submit);
            }
            foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => host.numbers.Count == 2)) yield return wait;
            Assert.That(host.numbers, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(list.selectedIndices, Is.Empty);

            var appended = helper.InspectorRoot.Query<ValueDropdownElement>().ToList().Single(x => x.Label == "Appended");
            foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => appended.Q<Label>()?.style.width.value.value == 200f)) yield return wait;
        }

        class ScalarOwner
        {
            public int value = 1;
            public int[] Choices => new[] { 1, 2, 3 };
        }

        class ReadOnlyCollections
        {
            readonly List<string> tags = new List<string> { "a" };
            public readonly string[] fixedTags = { "a", "b" };
            [ShowInInspector] public List<string> Tags => tags;
            [ShowInInspector] public List<string> Computed => new List<string>(tags);
        }

        class LabelOwner
        {
            public int weapon = 1;
            public int evaluations;
            public bool fail;

            public ValueDropdownList<int> Weapons()
            {
                evaluations++;
                if (fail) throw new InvalidOperationException("provider failed");
                return new ValueDropdownList<int> { { "Melee/Sword", 1 }, { "Melee/Axe", 2 } };
            }
        }

        class Effect
        {
            public int power;
        }

        class FireEffect : Effect { }

        class PowerComparer : IEqualityComparer<FireEffect>
        {
            public bool Equals(FireEffect x, FireEffect y) => x?.power == y?.power;
            public int GetHashCode(FireEffect effect) => effect.power;
        }

        class CallbackOwner
        {
            public readonly List<object> calls = new List<object>();
            public void Changed() => calls.Add("none");
            public void Changed(int value) => calls.Add(value);
            void Changed(object value) => calls.Add(value);
            public void Changed(string value) => calls.Add(value);
            public void Changed<T>(T value) => calls.Add(value);
        }

        class Faulty
        {
            public bool fail;
            public override string ToString() => fail ? throw new InvalidOperationException("broken") : "fine";
        }

        class FaultyOwner
        {
            public Faulty value = new Faulty();
            public List<Faulty> items = new List<Faulty> { new Faulty { fail = true } };
            public Faulty[] None => Array.Empty<Faulty>();
        }
    }

    public class ValueDropdownTestHost : ScriptableObject
    {
        public int[] values;
        public int other;
        public List<string> tags;
        public List<Item> items;
        public Item payload;
        public string title;
        public string weapon;
        [NonSerialized] public bool bowEnabled = true;

        public static ValueDropdownList<string> Titles
        {
            get
            {
                var titles = new ValueDropdownList<string> { Comparer = StringComparer.OrdinalIgnoreCase };
                titles.Add("One", "one");
                return titles;
            }
        }

        public ValueDropdownList<string> Weapons => new ValueDropdownList<string> { { "Sword", "sword" }, { "Bow", "bow", null, bowEnabled } };

        [Serializable]
        public class Item
        {
            public int number;
            public string name;
        }
    }
}
