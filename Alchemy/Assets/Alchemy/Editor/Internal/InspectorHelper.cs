using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Alchemy.Inspector;
using Alchemy.Editor.Elements;
#if ALCHEMY_SUPPORT_SERIALIZATION
using Alchemy.Serialization;
#endif

namespace Alchemy.Editor
{
    internal static class InspectorHelper
    {
        public sealed class GroupNode
        {
            public GroupNode(string name, AlchemyGroupDrawer drawer)
            {
                this.name = name;
                this.drawer = drawer;
            }

            readonly string name;
            readonly AlchemyGroupDrawer drawer;

            readonly List<MemberInfo> members = new();
            readonly List<GroupNode> children = new();

            public string Name => name;
            public IEnumerable<MemberInfo> Members => members;
            public AlchemyGroupDrawer Drawer => drawer;
            public VisualElement VisualElement { get; set; }
            public GroupNode Parent { get; private set; }

            public GroupNode Find(Func<GroupNode, bool> predicate)
            {
                return children.FirstOrDefault(predicate);
            }

            public void Add(GroupNode node)
            {
                children.Add(node);
                children.Sort((a, b) => a.drawer.Order.CompareTo(b.drawer.Order));
                node.Parent = this;
            }

            public void AddMember(MemberInfo memberInfo)
            {
                members.Add(memberInfo);
            }

            public IEnumerable<GroupNode> DescendantsAndSelf()
            {
                yield return this;
                foreach (var item in Descendants(children)) yield return item;
            }

            static IEnumerable<GroupNode> Descendants(IEnumerable<GroupNode> source)
            {
                foreach (var item in source)
                {
                    yield return item;
                    var e = Descendants(item.children).GetEnumerator();
                    while (e.MoveNext())
                    {
                        yield return e.Current;
                    }
                }
            }
        }

        public static void BuildElements(SerializedObject serializedObject, VisualElement rootElement, object target, Func<string, SerializedProperty> findPropertyFunc)
        {
            if (target == null) return;

            // Build node
            var rootNode = BuildInspectorNode(target.GetType());

            // Add elements
            foreach (var node in rootNode.DescendantsAndSelf())
            {
                // Get or create group element
                if (node.Parent == null)
                {
                    node.VisualElement = rootElement;
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
                    // Exclude if member has HideInInspector attribute
                    if (member.HasCustomAttribute<HideInInspector>()) continue;

                    // Add default PropertyField if member has DisableAlchemyEditorAttribute
                    if (member.GetCustomAttribute<DisableAlchemyEditorAttribute>() != null)
                    {
                        var p = findPropertyFunc(member.Name);
                        if (p != null)
                        {
                            var propertyField = new PropertyField(p);
                            propertyField.style.width = Length.Percent(100f);
                            node.VisualElement.Add(propertyField);
                        }
                        continue;
                    }

                    VisualElement element = null;
                    var property = findPropertyFunc(member.Name);
                    var isManagedReferenceProperty = property?.propertyType == SerializedPropertyType.ManagedReference;

                    // Add default PropertyField if the property has a custom PropertyDrawer
                    if ((member is FieldInfo fieldInfo && InternalAPIHelper.GetDrawerTypeForType(fieldInfo.FieldType, isManagedReferenceProperty) != null) ||
                        (member is PropertyInfo propertyInfo && InternalAPIHelper.GetDrawerTypeForType(propertyInfo.PropertyType, isManagedReferenceProperty) != null))
                    {
                        if (property != null)
                        {
                            element = new PropertyField(property);
                        }
                    }
                    else
                    {
                        element = CreateMemberElement(serializedObject, target, member, findPropertyFunc);
                    }

                    if (element == null) continue;
                    element.style.width = Length.Percent(100f);

                    var e = node.Drawer?.GetGroupElement(
                        member.GetCustomAttributes<PropertyGroupAttribute>()
                            .OrderByDescending(x => x.GroupPath.Split('/').Length)
                            .FirstOrDefault()
                    );

                    if (e == null) node.VisualElement.Add(element);
                    else e.Add(element);
                    AlchemyAttributeDrawer.ExecutePropertyDrawers(serializedObject, property, target, member, element);
                }
            }
        }

        internal static GroupNode BuildInspectorNode(Type targetType)
        {
            var rootNode = new GroupNode("Inspector-Group-Root", null);

            // Get all members
            var members = ReflectionHelper.GetMembers(targetType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, true)
                .Where(x => x is MethodInfo or FieldInfo or PropertyInfo);

            // Build member nodes
            foreach (var member in members)
            {
                var groupAttributes = member.GetCustomAttributes<PropertyGroupAttribute>(true);
                if (groupAttributes.Count() == 0)
                {
                    rootNode.AddMember(member);
                    continue;
                }

                var parentNode = rootNode;

                foreach (var (groupAttribute, hierarchy) in groupAttributes
                    .Select(x => (x, x.GroupPath.Split('/')))
                    .OrderBy(x => x.Item2.Length))
                {
                    parentNode = rootNode;
                    foreach (var groupName in hierarchy)
                    {
                        var next = parentNode.Find(x => x.Name == groupName);
                        if (next == null)
                        {
                            var drawer = AlchemyEditorUtility.CreateGroupDrawer(groupAttribute, targetType);
                            next = new GroupNode(groupName, drawer);
                            parentNode.Add(next);
                        }

                        parentNode = next;
                    }
                }

                parentNode.AddMember(member);
            }

            return rootNode;
        }

        public static VisualElement CreateMemberElement(SerializedObject serializedObject, object target, MemberInfo memberInfo, Func<string, SerializedProperty> findPropertyFunc)
        {
            switch (memberInfo)
            {
                case MethodInfo methodInfo:
                    if (methodInfo.HasCustomAttribute<ButtonAttribute>())
                    {
                        return new MethodButton(target, methodInfo);
                    }
                    break;
                case FieldInfo:
                case PropertyInfo:
                    var isSerializedMember = false;
                    if (memberInfo is FieldInfo f) isSerializedMember = f.IsPublic | f.HasCustomAttribute<SerializeField>();
                    else if (memberInfo is PropertyInfo p) isSerializedMember = p.HasCustomAttribute<SerializeField>();

                    if (isSerializedMember)
                    {
                        var property = findPropertyFunc?.Invoke(memberInfo.Name);

                        // Create property field
                        if (property != null)
                        {
                            if (memberInfo is FieldInfo fieldInfo)
                            {
                                return new AlchemyPropertyField(property, fieldInfo.FieldType);
                            }
                            else
                            {
                                return new AlchemyPropertyField(property, ((PropertyInfo)memberInfo).PropertyType);
                            }
                        }
                    }

#if ALCHEMY_SUPPORT_SERIALIZATION
                    if (serializedObject.targetObject != null &&
                        memberInfo.DeclaringType.HasCustomAttribute<AlchemySerializeAttribute>() &&
                        memberInfo.HasCustomAttribute<AlchemySerializeFieldAttribute>())
                    {
                        var element = default(VisualElement);
                        if (memberInfo is FieldInfo fieldInfo)
                        {
                            var declaredType = fieldInfo.DeclaringType;
                            if (declaredType.IsConstructedGenericType)
                            {
                                declaredType = declaredType.GetGenericTypeDefinition();
                            }
                            var dataName ="__alchemySerializationData_"+ declaredType.FullName.Replace("`","").Replace(".", "_") ;
                            
                            SerializedProperty GetProperty() => findPropertyFunc?.Invoke(dataName)
                                .FindPropertyRelative(memberInfo.Name);

                            var p = GetProperty();
                            if (p != null)
                            {
                                var field = new ReflectionField(target, fieldInfo);
                                var foldout = field.Q<Foldout>();
                                foldout?.BindProperty(p);
                                field.TrackPropertyValue(p, p =>
                                {
                                    field.Rebuild(target, memberInfo);
                                    var foldout = field.Q<Foldout>();
                                    foldout?.BindProperty(p);
                                });

                                var undoName = "Modified:" + p.displayName;
                                field.OnBeforeValueChange += x =>
                                {
                                    Undo.RegisterCompleteObjectUndo(GetProperty().serializedObject.targetObject, undoName);
                                };

                                element = field;
                            }
                        }

                        // TODO: Supports editing of multiple objects
                        if (element != null && serializedObject.targetObjects.Length > 1)
                        {
                            element.SetEnabled(false);
                        }

                        return element;
                    }
#endif

                    // Create element if member has ShowInInspector attribute
                    if (memberInfo.HasCustomAttribute<ShowInInspectorAttribute>())
                    {
                        return new ReflectionField(target, memberInfo);
                    }
                    break;
            }
            return null;
        }

        internal static IOrderedEnumerable<MemberInfo> OrderByAttributeThenByMemberType(this IEnumerable<MemberInfo> members)
        {
            return members
                .OrderBy(x =>
                {
                    var orderAttribute = x.GetCustomAttribute<OrderAttribute>();
                    if (orderAttribute == null) return 0;
                    return orderAttribute.Order;
                })
                .ThenBy(x =>
                {
                    if (x is MethodInfo) return 1;
                    return 0;
                });
        }
    }
}