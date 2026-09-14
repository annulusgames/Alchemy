using System;
using System.Collections;
using System.Collections.Generic;
using Alchemy.Editor;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class ValueDropdownTest
    {
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
                () => owner.Values, value => owner.Values = (int[])value, () => writes++, true);
            binding.Changed += () => changes++;
            binding.Set(0, new object[] { 3 });
            binding.Append(new[] { new object[] { 4, 5 } });
            binding.Move(3, 1);
            binding.Remove(new[] { 2 });
            Assert.That(owner.Values, Is.EqualTo(new[] { 3, 5, 4 }));
            Assert.That(original, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(writes, Is.EqualTo(4));
            Assert.That(changes, Is.EqualTo(4));
            Assert.That(binding.Revision, Is.EqualTo(4));
            Assert.Throws<ArgumentException>(() => binding.Set(0, new object[] { "wrong" }));
            Assert.Throws<InvalidOperationException>(() => binding.Set(3, new object[] { 0 }));
            Assert.That(owner.Values, Is.EqualTo(new[] { 3, 5, 4 }));
        }

        [Test]
        public void Binding_ReadOnlyRejectsMutation()
        {
            var owner = new Provider();
            var binding = new ValueDropdownBinding(owner, typeof(Provider).GetField("Values"), typeof(int[]),
                () => owner.Values, _ => Assert.Fail("Read-only binding wrote a value."), null, false);
            Assert.Throws<InvalidOperationException>(() => binding.Set(0, new object[] { 3 }));
            Assert.That(owner.Values, Is.EqualTo(new[] { 1, 2 }));
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
    }

    public class ValueDropdownTestHost : ScriptableObject
    {
        public int[] values;
        public int other;
    }
}
