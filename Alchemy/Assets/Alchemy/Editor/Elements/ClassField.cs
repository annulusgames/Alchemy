using System;
using System.Reflection;
using UnityEngine.UIElements;
using Alchemy.Inspector;

namespace Alchemy.Editor.Elements
{
    public sealed class ClassField : VisualElement
    {
       // public ClassField(Type type, string label) : this(TypeHelper.CreateDefaultInstance(type), type, label) { }
        public ClassField(IObjectAccessor accessor, Type type, string label)
        {
            var foldout = new Foldout
            {
                text = label,
                value = false
            };

            var toggle = foldout.Q<Toggle>();
            var clickable = InternalAPIHelper.GetClickable(toggle);
            InternalAPIHelper.SetAcceptClicksIfDisabled(clickable, true);

            // Build node
            var rootNode = InspectorHelper.BuildInspectorNode(type);

            // Add elements
            foreach (var node in rootNode.DescendantsAndSelf())
            {
                // Get or create group element
                if (node.Parent == null)
                {
                    node.VisualElement = foldout;
                }
                else if (node.Drawer == null)
                {
                    node.VisualElement = node.Parent.VisualElement;
                }
                else
                {
                    node.VisualElement = node.Drawer.CreateRootElement(node.Name);
                    node.Parent.VisualElement.Add(node.VisualElement);
                }

                // Add member elements
                foreach (var member in node.Members.OrderByAttributeThenByMemberType())
                {
                    var element = new ReflectionField(accessor, member);
                    element.style.width = Length.Percent(100f);
                    element.OnValueChanged += x => OnValueChanged?.Invoke(accessor.Target);

                    var e = node.Drawer?.GetGroupElement(member.GetCustomAttribute<PropertyGroupAttribute>());
                    if (e == null) node.VisualElement.Add(element);
                    else e.Add(element);
                    AlchemyAttributeDrawer.ExecutePropertyDrawers(null, null, accessor.Target, member, element);
                }
            }

            Add(foldout);
        }

        public event Action<object> OnValueChanged;
    }
}