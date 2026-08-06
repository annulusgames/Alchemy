using System;
using System.Reflection;
using Alchemy.Inspector;
using UnityEngine.UIElements;

namespace Alchemy.Editor.Elements
{
    public sealed class ClassField : VisualElement
    {
        public ClassField(Type type, string label) : this(TypeHelper.CreateDefaultInstance(type), type, label) { }
        public ClassField(object obj, Type type, string label)
        {
            var foldout = new Foldout
            {
                text = label,
                value = false
            };

            var toggle = foldout.Q<Toggle>();
            var clickable = InternalAPIHelper.GetClickable(toggle);
            InternalAPIHelper.SetAcceptClicksIfDisabled(clickable, true);

            var rootNode = InspectorHelper.BuildInspectorNode(type);
            rootNode.VisualElement = foldout;
            BuildNodeElements(rootNode, obj, value => OnValueChanged?.Invoke(value));

            Add(foldout);
        }

        static void BuildNodeElements(
            InspectorHelper.GroupNode node,
            object obj,
            Action<object> onValueChanged)
        {
            foreach (var (member, child) in InspectorHelper.GetOrderedSiblings(node))
            {
                if (child != null)
                {
                    if (child.Drawer == null)
                    {
                        child.VisualElement = node.VisualElement;
                    }
                    else
                    {
                        child.VisualElement = child.Drawer.CreateRootElement(child.Name);
                        node.VisualElement.Add(child.VisualElement);
                    }

                    BuildNodeElements(child, obj, onValueChanged);
                    continue;
                }

                var element = new ReflectionField(obj, member);
                element.style.width = Length.Percent(100f);
                element.OnValueChanged += _ => onValueChanged?.Invoke(obj);

                var e = node.Drawer?.GetGroupElement(member.GetCustomAttribute<PropertyGroupAttribute>());
                if (e == null) node.VisualElement.Add(element);
                else e.Add(element);
                AlchemyAttributeDrawer.ExecutePropertyDrawers(null, null, obj, member, element);
            }
        }

        public event Action<object> OnValueChanged;
    }
}
