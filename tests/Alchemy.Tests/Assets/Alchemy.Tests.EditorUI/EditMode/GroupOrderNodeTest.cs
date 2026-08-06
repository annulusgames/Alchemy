using System.Linq;
using Alchemy.Editor;
using Alchemy.Inspector;
using NUnit.Framework;

namespace Alchemy.Tests.EditorUI.EditMode
{
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
        public void BuildInspectorNode_PlacesUnorderedGroupsAfterOrderedGroups()
        {
            var root = InspectorHelper.BuildInspectorNode(typeof(MixedOrderedGroups));

            Assert.That(root.Children[0].Name, Is.EqualTo("Ordered"));
            Assert.That(root.Children[0].Order, Is.EqualTo(5));
            Assert.That(
                root.Children.Skip(1).Select(x => x.Name),
                Is.EquivalentTo(new[] { "UnorderedA", "UnorderedB" }));
            Assert.That(root.Children.Skip(1).All(x => x.Order == int.MaxValue), Is.True);
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

            Assert.That(parent.Order, Is.EqualTo(int.MaxValue));
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
    }
}
