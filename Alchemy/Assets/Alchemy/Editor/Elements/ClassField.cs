using System;
using System.Reflection;
using UnityEngine.UIElements;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Editor.Elements
{
    public sealed class ClassField : VisualElement
    {
        private readonly StyleSheet _styleSheet = Resources.Load<StyleSheet>("Elements/ClassField-Styles");
        
        public ClassField(Type type, string label) : this(TypeHelper.CreateDefaultInstance(type), type, label) { }
        public ClassField(object obj, Type type, string label)
        {
            styleSheets.Add(_styleSheet);

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
                    var element = new ReflectionField(obj, member);
                    element.AddToClassList("class-field__reflection-field");
                    element.OnValueChanged += x => OnValueChanged?.Invoke(obj);

                    var e = node.Drawer?.GetGroupElement(member.GetCustomAttribute<PropertyGroupAttribute>());
                    if (e == null) node.VisualElement.Add(element);
                    else e.Add(element);
                    AlchemyAttributeDrawer.ExecutePropertyDrawers(null, null, obj, member, element);
                }
            }

            Add(foldout);
        }

        public event Action<object> OnValueChanged;
    }
}