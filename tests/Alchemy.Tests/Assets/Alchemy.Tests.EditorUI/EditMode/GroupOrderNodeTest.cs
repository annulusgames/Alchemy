using System;
using System.Linq;
using Alchemy.Editor;
using Alchemy.Inspector;
using Alchemy.Tests.EditorUI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
#if ALCHEMY_SUPPORT_SERIALIZATION
using Alchemy.Serialization;
#endif

namespace Alchemy.Tests.EditorUI.EditMode
{
#if ALCHEMY_SUPPORT_SERIALIZATION
    [AlchemySerialize]
    partial class AlchemySerializeFieldVisibilityTarget : ScriptableObject
    {
        [AlchemySerializeField, NonSerialized]
        int privateValue;
    }
#endif

    public class GroupOrderNodeTest
    {
        [Test]
        public void BuildInspectorNode_OrdersSiblingGroupsByOrderThenDeclaration()
        {
            var root = InspectorHelper.BuildInspectorNode(typeof(OrderedGroups));

            Assert.That(
                root.Children.Select(x => x.Name).ToArray(),
                Is.EqualTo(new[] { "First", "Second", "Third" }));
            Assert.That(root.Children[0].Order, Is.EqualTo(10));
            Assert.That(root.Children[1].Order, Is.EqualTo(20));
            Assert.That(root.Children[2].Order, Is.EqualTo(30));
        }

        [Test]
        public void BuildInspectorNode_PlacesUnorderedGroupsAtBaselineZero()
        {
            var root = InspectorHelper.BuildInspectorNode(typeof(MixedOrderedGroups));

            Assert.That(
                root.Children.Select(x => x.Name).ToArray(),
                Is.EqualTo(new[] { "UnorderedB", "UnorderedA", "Ordered" }));
            Assert.That(root.Children[0].Order, Is.EqualTo(0));
            Assert.That(root.Children[1].Order, Is.EqualTo(0));
            Assert.That(root.Children[2].Order, Is.EqualTo(5));
        }

        [Test]
        public void BuildInspectorNode_UsesMinimumOrderOnConflict()
        {
            var root = InspectorHelper.BuildInspectorNode(typeof(ConflictingOrderGroups));
            var shared = root.Children.Single(x => x.Name == "Shared");

            Assert.That(shared.Order, Is.EqualTo(10));
        }

        [Test]
        public void BuildInspectorNode_AppliesOrderOnlyToLeafOfNestedPath()
        {
            var root = InspectorHelper.BuildInspectorNode(typeof(NestedPathOrderGroups));
            var parent = root.Children.Single(x => x.Name == "Parent");
            var child = parent.Children.Single(x => x.Name == "Child");

            Assert.That(parent.Order, Is.EqualTo(0));
            Assert.That(child.Order, Is.EqualTo(10));
            Assert.That(parent.Drawer.UniqueId, Does.EndWith("_Parent"));
            Assert.That(child.Drawer.UniqueId, Does.EndWith("_Parent/Child"));
        }

        [Test]
        public void BuildInspectorNode_PreservesDeclarationOrderForEqualOrders()
        {
            var root = InspectorHelper.BuildInspectorNode(typeof(EqualOrderGroups));

            Assert.That(
                root.Children.Select(x => x.Name).ToArray(),
                Is.EqualTo(new[] { "Alpha", "Beta" }));
        }

        [Test]
        public void BuildInspectorNode_TabGroupOrderConflictsUseMinimumSilently()
        {
            var root = InspectorHelper.BuildInspectorNode(typeof(ConflictingTabOrderGroups));
            var tabs = root.Children.Single(x => x.Name == "Tabs");

            Assert.That(tabs.Order, Is.EqualTo(10));
        }

        [Test]
        public void GetOrderedSiblingNames_InterleavesMembersAndGroupsOnSameOrderScale()
        {
            var root = InspectorHelper.BuildInspectorNode(typeof(InterleavedMembersAndGroups));

            Assert.That(
                InspectorHelper.GetOrderedSiblingNames(root),
                Is.EqualTo(new[] { "before", "Middle", "after" }));
        }

        [Test]
        public void GetOrderedSiblingNames_PlacesMaxValueMemberAfterOrderedGroups()
        {
            var root = InspectorHelper.BuildInspectorNode(typeof(CaptureStyleBounds));

            Assert.That(
                InspectorHelper.GetOrderedSiblingNames(root),
                Is.EqualTo(new[] { "start", "Content", "end" }));
        }

        [Test]
        public void GetOrderedSiblingNames_GroupAndButtonOrder_MatchesFixtureComments()
        {
            var root = InspectorHelper.BuildInspectorNode(typeof(GroupAndButtonOrderTest));
            var actual = InspectorHelper.GetOrderedSiblingNames(root);
            // Mirrors comments on GroupAndButtonOrderTest (Order, then Reflection declaration order).
            var expected = new[]
            {
                "Foldout Group A1",
                "fieldA1",
                "Tab Group A1",
                "Foldout Group A2",
                "ButtonA1",
                "ButtonA2",
                "ButtonA3",
                "fieldB1",
                "Foldout Group B1",
                "ButtonB1",
                "ButtonB2",
                "Foldout Group B2",
                "ButtonB3",
                "Tab Group B1",
            };

            Assert.That(
                actual,
                Is.EqualTo(expected),
                $"GroupAndButtonOrderTest order. Actual: [{string.Join(", ", actual)}]");
        }

        // Issue #80: buttons declared after PropertyGroups stay after them (Reflection
        // groups member kinds: fields/groups before methods/buttons).
        [Test]
        public void GetOrderedSiblingNames_Issue80_ButtonsStayAfterPropertyGroups()
        {
            var root = InspectorHelper.BuildInspectorNode(typeof(Issue80PropertyGroupButtons));
            var actual = InspectorHelper.GetOrderedSiblingNames(root);
            var expected = new[]
            {
                "issue80Before",
                "Issue80 Tabs",
                "Issue80 Foldout",
                "issue80After",
                "Issue80ButtonA",
                "Issue80ButtonB",
            };

            Assert.That(
                actual,
                Is.EqualTo(expected),
                $"Issue #80 order. Actual: [{string.Join(", ", actual)}]");
        }

        // Issue #112: unordered groups/members share baseline 0; Order(1) comes after
        // Volume Profiles / Audio / Settings by declaration order among Order 0 ties.
        [Test]
        public void GetOrderedSiblingNames_Issue112_FoldoutsThenSettingsThenOrderedButton()
        {
            var root = InspectorHelper.BuildInspectorNode(typeof(Issue112FoldoutButtonOrder));
            var actual = InspectorHelper.GetOrderedSiblingNames(root);
            var expected = new[]
            {
                "Volume Profiles",
                "Audio",
                "Settings",
                "OrderedButtonLater",
            };

            Assert.That(
                actual,
                Is.EqualTo(expected),
                $"Issue #112 order. Actual: [{string.Join(", ", actual)}]");
        }

        [Test]
        public void GetOrderedSiblingNames_ThreeLevelClassInheritance_BaseBeforeDerived()
        {
            var root = InspectorHelper.BuildInspectorNode(typeof(DeclaredAtDerived));

            Assert.That(
                InspectorHelper.GetOrderedSiblingNames(root),
                Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void GetOrderedSiblingNames_MultiLevelMonoBehaviourInheritance_BaseBeforeDerived()
        {
            var names = InspectorHelper.GetOrderedSiblingNames(
                InspectorHelper.BuildInspectorNode(typeof(DeclaredAtMonoDerived)));
            var indexA = names.ToList().IndexOf("a");
            var indexB = names.ToList().IndexOf("b");
            var indexC = names.ToList().IndexOf("c");

            Assert.That(indexA, Is.GreaterThanOrEqualTo(0));
            Assert.That(indexB, Is.GreaterThan(indexA));
            Assert.That(indexC, Is.GreaterThan(indexB));
        }

#if ALCHEMY_SUPPORT_SERIALIZATION
        [Test]
        public void GetOrderedSiblings_IncludesPrivateAlchemySerializeField()
        {
            var root = InspectorHelper.BuildInspectorNode(typeof(AlchemySerializeFieldVisibilityTarget));

            Assert.That(
                InspectorHelper.GetOrderedSiblings(root).Any(x => x.Member?.Name == "privateValue"),
                Is.True);
            Assert.That(
                InspectorHelper.GetOrderedSiblingNames(root),
                Does.Contain("privateValue"));
        }

        [Test]
        public void BuildElements_DrawsPrivateAlchemySerializeField()
        {
            var target = ScriptableObject.CreateInstance<AlchemySerializeFieldVisibilityTarget>();
            try
            {
                var serializedObject = new SerializedObject(target);
                var root = new VisualElement();
                InspectorHelper.BuildElements(
                    serializedObject,
                    root,
                    target,
                    name => serializedObject.FindProperty(name));

                Assert.That(
                    root.Query<IntegerField>().ToList().Any(field => field.label == "Private Value"),
                    Is.True,
                    "Private [AlchemySerializeField] must remain drawable after sibling ordering.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
#endif

        sealed class OrderedGroups
        {
            [Group("Third", 30)] public float c;
            [Group("First", 10)] public float a;
            [Group("Second", 20)] public float b;
        }

        sealed class MixedOrderedGroups
        {
            [Group("UnorderedB")] public float unorderedB;
            [Group("Ordered", 5)] public float ordered;
            [Group("UnorderedA")] public float unorderedA;
        }

        sealed class ConflictingOrderGroups
        {
            [Group("Shared", 20)] public float a;
            [Group("Shared", 10)] public float b;
        }

        sealed class NestedPathOrderGroups
        {
            [Group("Parent/Child", 10)] public float value;
        }

        sealed class EqualOrderGroups
        {
            [Group("Alpha", 10)] public float a;
            [Group("Beta", 10)] public float b;
        }

        sealed class ConflictingTabOrderGroups
        {
            [TabGroup("Tabs", "Tab1", 10)] public float a;
            [TabGroup("Tabs", "Tab2", 20)] public float b;
        }

        sealed class InterleavedMembersAndGroups
        {
            [Alchemy.Inspector.Order(20)] public float after;
            [Group("Middle", 10)] public float middle;
            [Alchemy.Inspector.Order(-1)] public float before;
        }

        sealed class CaptureStyleBounds
        {
            [Alchemy.Inspector.Order(-1)] public int start;
            [Group("Content", 10)] public float content;
            [Alchemy.Inspector.Order(int.MaxValue)]
            public int end;
        }

        sealed class Issue80PropertyGroupButtons
        {
            public string issue80Before;
            [TabGroup("Issue80 Tabs", "Bad Zone")] public string issue80TabBad;
            [TabGroup("Issue80 Tabs", "Another Tab")] public string issue80TabFine;
            [FoldoutGroup("Issue80 Foldout")] public string issue80Foldout;
            public string issue80After;

            [Button]
            void Issue80ButtonA() { }

            [Button]
            void Issue80ButtonB(int count = 1) { }
        }

        sealed class Issue112FoldoutButtonOrder
        {
            [FoldoutGroup("Volume Profiles")] public float volumeA;
            [FoldoutGroup("Volume Profiles")] public float volumeB;
            [FoldoutGroup("Audio")] public float audioA;
            [FoldoutGroup("Audio")] public float audioB;

            [FoldoutGroup("Settings"), Button]
            void ChangeSetting() { }

            [FoldoutGroup("Settings/Audio"), Button]
            void ChangeNestedAudio() { }

            [Button, Alchemy.Inspector.Order(1)]
            void OrderedButtonLater() { }
        }

        class DeclaredAtBaseA
        {
            public int a;
        }

        class DeclaredAtBaseB : DeclaredAtBaseA
        {
            public int b;
        }

        class DeclaredAtDerived : DeclaredAtBaseB
        {
            public int c;
        }

        class DeclaredAtMonoBaseA : MonoBehaviour
        {
            public int a;
        }

        class DeclaredAtMonoBaseB : DeclaredAtMonoBaseA
        {
            public int b;
        }

        class DeclaredAtMonoDerived : DeclaredAtMonoBaseB
        {
            public int c;
        }
    }
}
