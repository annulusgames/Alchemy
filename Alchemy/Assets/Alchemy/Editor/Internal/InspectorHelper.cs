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

            readonly List<(MemberInfo Member, int DeclaredAt, int Order, GroupLayout[] Groups)> members = new();
            readonly List<GroupNode> children = new();

            bool hasDefinedOrder;

            public string Name => name;
            /// <summary>
            /// Sibling drawing order. Defaults to 0 when no attribute specifies Order (same as members).
            /// Shares the same scale as <see cref="OrderAttribute"/> on ungrouped members.
            /// </summary>
            public int Order { get; private set; }
            /// <summary>
            /// Declaration ordinal of the first member that created this group (for stable ties).
            /// </summary>
            public int DeclaredAt { get; private set; } = int.MaxValue;
            public IEnumerable<MemberInfo> Members => members.Select(x => x.Member);
            public IEnumerable<(MemberInfo Member, int DeclaredAt)> MemberEntries => members.Select(x => (x.Member, x.DeclaredAt));
            internal IEnumerable<(MemberInfo Member, int DeclaredAt, int Order, GroupLayout[] Groups)> LayoutEntries => members;
            public IReadOnlyList<GroupNode> Children => children;
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
                node.Parent = this;
            }

            public void AddMember(MemberInfo memberInfo, int declaredAt)
            {
                AddMember(memberInfo, declaredAt, GetMemberOrder(memberInfo), GetOrderedGroupLayouts(memberInfo));
            }

            internal void AddMember(MemberInfo memberInfo, int declaredAt, int order, GroupLayout[] groups)
            {
                members.Add((memberInfo, declaredAt, order, groups));
            }

            public void NotifyDeclaredAt(int declaredAt)
            {
                DeclaredAt = Math.Min(DeclaredAt, declaredAt);
            }

            public void RegisterOrder(PropertyGroupAttribute attribute)
            {
                if (!attribute.HasDefinedOrder) return;

                Order = hasDefinedOrder ? Math.Min(Order, attribute.Order) : attribute.Order;
                hasDefinedOrder = true;
            }

            public void SortChildrenRecursive()
            {
                var sorted = children
                    .OrderBy(x => x.Order)
                    .ThenBy(x => x.DeclaredAt)
                    .ToList();

                children.Clear();
                children.AddRange(sorted);

                foreach (var child in children)
                {
                    child.SortChildrenRecursive();
                }
            }
        }

        readonly struct SiblingItem
        {
            public SiblingItem(int order, int declaredAt, MemberInfo member, GroupLayout[] groups)
            {
                Order = order;
                DeclaredAt = declaredAt;
                Member = member;
                Group = null;
                Groups = groups;
            }

            public SiblingItem(int order, int declaredAt, GroupNode group)
            {
                Order = order;
                DeclaredAt = declaredAt;
                Member = null;
                Group = group;
                Groups = null;
            }

            public int Order { get; }
            public int DeclaredAt { get; }
            public MemberInfo Member { get; }
            public GroupNode Group { get; }
            public GroupLayout[] Groups { get; }
        }

        // One reflection walk per concrete type. Group drawers are still created per build.
        static readonly Dictionary<Type, MemberLayout[]> memberLayouts = new();
        static readonly GroupLayout[] emptyGroups = Array.Empty<GroupLayout>();

        readonly struct MemberLayout
        {
            public MemberLayout(MemberInfo member, int declaredAt, int order, GroupLayout[] groups)
            {
                Member = member;
                DeclaredAt = declaredAt;
                Order = order;
                Groups = groups;
            }

            public MemberInfo Member { get; }
            public int DeclaredAt { get; }
            public int Order { get; }
            public GroupLayout[] Groups { get; }
        }

        internal readonly struct GroupLayout
        {
            public GroupLayout(PropertyGroupAttribute attribute, string[] hierarchy)
            {
                Attribute = attribute;
                Hierarchy = hierarchy;
            }

            public PropertyGroupAttribute Attribute { get; }
            public string[] Hierarchy { get; }
        }

        public static void BuildElements(SerializedObject serializedObject, VisualElement rootElement, object target, Func<string, SerializedProperty> findPropertyFunc)
        {
            if (target == null) return;

            var rootNode = BuildInspectorNode(target.GetType());
            rootNode.VisualElement = rootElement;
            BuildNodeElements(rootNode, serializedObject, target, findPropertyFunc);
            PrefabConditionalElement.SetInspectorTargets(rootElement, serializedObject, target);
        }

        // Collapsed foldouts skip child construction until the first expand.
        // Toggles raise ChangeEvent. SetValueWithoutNotify and isExpanded bindings do not;
        // those still flip content display, which changes layout.
        // ChangeEvent is not dispatched while detached; a value set off-panel is applied on attach.
        internal static void BuildFoldoutContents(Foldout foldout, bool expanded, Action build)
        {
            if (expanded)
            {
                build();
                return;
            }

            var built = false;
            EventCallback<ChangeEvent<bool>> onValueChanged = null;
            EventCallback<GeometryChangedEvent> onFoldoutGeometryChanged = null;
            EventCallback<GeometryChangedEvent> onContentGeometryChanged = null;
            EventCallback<AttachToPanelEvent> onAttachToPanel = null;

            void Unregister()
            {
                foldout.UnregisterCallback(onValueChanged);
                foldout.UnregisterCallback(onFoldoutGeometryChanged);
                foldout.contentContainer.UnregisterCallback(onContentGeometryChanged);
                foldout.UnregisterCallback(onAttachToPanel);
            }

            void BuildOnce()
            {
                if (built || !foldout.value) return;
                built = true;
                Unregister();
                build();
            }

            onValueChanged = evt =>
            {
                if (evt.target != foldout) return;
                BuildOnce();
            };
            onFoldoutGeometryChanged = _ => BuildOnce();
            onContentGeometryChanged = _ => BuildOnce();
            onAttachToPanel = evt =>
            {
                if (evt.target != foldout) return;
                BuildOnce();
            };

            foldout.RegisterCallback(onValueChanged);
            foldout.RegisterCallback(onFoldoutGeometryChanged);
            foldout.contentContainer.RegisterCallback(onContentGeometryChanged);
            foldout.RegisterCallback(onAttachToPanel);
        }

        static void BuildNodeElements(
            GroupNode node,
            SerializedObject serializedObject,
            object target,
            Func<string, SerializedProperty> findPropertyFunc)
        {
            foreach (var item in EnumerateOrderedSiblings(node))
            {
                if (item.Group != null)
                {
                    var child = item.Group;
                    if (child.Drawer == null)
                    {
                        child.VisualElement = node.VisualElement;
                    }
                    else
                    {
                        child.VisualElement = child.Drawer.CreateRootElement(child.Name);
                        node.VisualElement.Add(child.VisualElement);
                    }

                    BuildNodeElements(child, serializedObject, target, findPropertyFunc);
                    continue;
                }

                AddMemberElement(node, item.Member, item.Groups, serializedObject, target, findPropertyFunc);
            }
        }

        static void AddMemberElement(
            GroupNode node,
            MemberInfo member,
            GroupLayout[] groups,
            SerializedObject serializedObject,
            object target,
            Func<string, SerializedProperty> findPropertyFunc)
        {
            // Exclude if member has HideInInspector attribute
            // but not "m_SerializedDataModeController" on EditorWindow
            // (Unity added HideInInspector here in 2022.3.23f1)
            if (member.HasCustomAttribute<HideInInspector>() && member.Name != "m_SerializedDataModeController")
                return;

            // Add default PropertyField if member has DisableAlchemyEditorAttribute.
            // Methods are never serialized properties.
            if (member.GetCustomAttribute<DisableAlchemyEditorAttribute>() != null)
            {
                if (member is MethodInfo) return;

                var p = findPropertyFunc(member.Name);
                if (p != null)
                {
                    var propertyField = new PropertyField(p);
                    propertyField.style.width = Length.Percent(100f);
                    node.VisualElement.Add(propertyField);
                }
                return;
            }

            VisualElement element = null;
            SerializedProperty property = null;
            if (member is not MethodInfo)
            {
                property = findPropertyFunc(member.Name);
            }
            var isManagedReferenceProperty = property?.propertyType == SerializedPropertyType.ManagedReference;

            // Select the value UI before constructing a potentially expensive custom drawer.
            if (ValueDropdownSource.GetAttribute(member) != null)
            {
                element = CreateMemberElement(serializedObject, target, member, property, findPropertyFunc);
            }
            // Add default PropertyField if the property has a custom PropertyDrawer
            else if ((member is FieldInfo fieldInfo && InternalAPIHelper.GetDrawerTypeForType(fieldInfo.FieldType, isManagedReferenceProperty) != null) ||
                (member is PropertyInfo propertyInfo && InternalAPIHelper.GetDrawerTypeForType(propertyInfo.PropertyType, isManagedReferenceProperty) != null))
            {
                if (property != null)
                {
                    element = new PropertyField(property);
                }
            }
            else
            {
                element = CreateMemberElement(serializedObject, target, member, property, findPropertyFunc);
            }

            if (element == null) return;
            element.style.width = Length.Percent(100f);

            var e = node.Drawer?.GetGroupElement(GetLeafGroupAttribute(groups));

            if (e == null) node.VisualElement.Add(element);
            else e.Add(element);
            AlchemyAttributeDrawer.ExecutePropertyDrawers(serializedObject, property, target, member, element);
        }

        internal static IReadOnlyList<string> GetOrderedSiblingNames(GroupNode node) =>
            EnumerateOrderedSiblings(node)
                .Where(x => x.Group != null || IsInspectorVisibleSiblingMember(x.Member))
                .Select(x => x.Group?.Name ?? x.Member!.Name)
                .ToArray();

        internal static IEnumerable<(MemberInfo Member, GroupNode Group)> GetOrderedSiblings(GroupNode node) =>
            EnumerateOrderedSiblings(node)
                .Select(x => (x.Member, x.Group));

        static IEnumerable<SiblingItem> EnumerateOrderedSiblings(GroupNode node)
        {
            // Ordering only — visibility is decided later by AddMemberElement /
            // CreateMemberElement (Inspector) or ReflectionField (ClassField).
            var memberItems = node.LayoutEntries
                .Select(entry =>
                    new SiblingItem(
                        entry.Order,
                        entry.DeclaredAt,
                        entry.Member,
                        entry.Groups));

            var groupItems = node.Children.Select(child =>
                new SiblingItem(child.Order, child.DeclaredAt, child));

            return memberItems
                .Concat(groupItems)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.DeclaredAt);
        }

        // Narrowed visibility for inspector-order introspection (tests / sibling names).
        // Not used by the build/render path — that must not drop AlchemySerializeField.
        static bool IsInspectorVisibleSiblingMember(MemberInfo member)
        {
            if (member is MethodInfo methodInfo)
            {
                return methodInfo.HasCustomAttribute<ButtonAttribute>();
            }

            if (member.HasCustomAttribute<HideInInspector>() && member.Name != "m_SerializedDataModeController")
            {
                return false;
            }

            if (member.HasCustomAttribute<ShowInInspectorAttribute>())
            {
                return true;
            }

#if ALCHEMY_SUPPORT_SERIALIZATION
            if (member.HasCustomAttribute<AlchemySerializeFieldAttribute>())
            {
                return true;
            }
#endif

            if (member is FieldInfo fieldInfo)
            {
                return fieldInfo.IsPublic
                    || fieldInfo.HasCustomAttribute<SerializeField>()
                    || fieldInfo.HasCustomAttribute<SerializeReference>();
            }

            if (member is PropertyInfo propertyInfo)
            {
                return propertyInfo.HasCustomAttribute<SerializeField>();
            }

            return false;
        }

        static int GetMemberOrder(MemberInfo member)
        {
            var orderAttribute = member.GetCustomAttribute<OrderAttribute>();
            return orderAttribute?.Order ?? 0;
        }

        static MemberLayout[] GetMemberLayouts(Type targetType)
        {
            if (memberLayouts.TryGetValue(targetType, out var cached))
                return cached;

            var ordered = DeclarationOrderHelper.OrderMembers(
                targetType,
                ReflectionHelper.GetMembers(targetType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, true));

            var layouts = new MemberLayout[ordered.Length];
            for (var i = 0; i < ordered.Length; i++)
            {
                var (member, declaredAt) = ordered[i];
                layouts[i] = new MemberLayout(member, declaredAt, GetMemberOrder(member), GetOrderedGroupLayouts(member));
            }

            memberLayouts.Add(targetType, layouts);
            return layouts;
        }

        // Path-length order matches the previous OrderBy(GroupPath.Split('/').Length), which is stable.
        static GroupLayout[] GetOrderedGroupLayouts(MemberInfo member)
        {
            var attributes = member.GetCustomAttributes<PropertyGroupAttribute>(true).ToArray();
            if (attributes.Length == 0) return emptyGroups;

            return attributes
                .Select(attribute => new GroupLayout(attribute, attribute.GroupPath.Split('/')))
                .OrderBy(group => group.Hierarchy.Length)
                .ToArray();
        }

        // First attribute among the longest paths. Matches OrderByDescending(path length).First().
        static PropertyGroupAttribute GetLeafGroupAttribute(GroupLayout[] groups)
        {
            if (groups == null || groups.Length == 0) return null;

            var leaf = groups[groups.Length - 1];
            var depth = leaf.Hierarchy.Length;
            for (var i = groups.Length - 2; i >= 0; i--)
            {
                if (groups[i].Hierarchy.Length != depth) break;
                leaf = groups[i];
            }

            return leaf.Attribute;
        }

        internal static GroupNode BuildInspectorNode(Type targetType)
        {
            var rootNode = new GroupNode("Inspector-Group-Root", null);

            foreach (var layout in GetMemberLayouts(targetType))
            {
                if (layout.Groups.Length == 0)
                {
                    rootNode.AddMember(layout.Member, layout.DeclaredAt, layout.Order, layout.Groups);
                    continue;
                }

                var parentNode = rootNode;

                foreach (var group in layout.Groups)
                {
                    var hierarchy = group.Hierarchy;
                    parentNode = rootNode;
                    for (var i = 0; i < hierarchy.Length; i++)
                    {
                        var groupName = hierarchy[i];
                        var next = parentNode.Find(x => x.Name == groupName);
                        if (next == null)
                        {
                            var nodePath = string.Join("/", hierarchy.Take(i + 1));
                            var drawer = AlchemyEditorUtility.CreateGroupDrawer(group.Attribute, targetType, nodePath);
                            next = new GroupNode(groupName, drawer);
                            parentNode.Add(next);
                        }

                        // Earliest declaring member wins for group placement among siblings.
                        next.NotifyDeclaredAt(layout.DeclaredAt);

                        // Order on a group attribute applies to the leaf group of that path.
                        if (i == hierarchy.Length - 1)
                        {
                            next.RegisterOrder(group.Attribute);
                        }

                        parentNode = next;
                    }
                }

                parentNode.AddMember(layout.Member, layout.DeclaredAt, layout.Order, layout.Groups);
            }

            rootNode.SortChildrenRecursive();
            return rootNode;
        }

        public static VisualElement CreateMemberElement(SerializedObject serializedObject, object target, MemberInfo memberInfo, Func<string, SerializedProperty> findPropertyFunc)
        {
            var property = findPropertyFunc?.Invoke(memberInfo.Name);
            return CreateMemberElement(serializedObject, target, memberInfo, property, findPropertyFunc);
        }

        internal static VisualElement CreateMemberElement(SerializedObject serializedObject, object target, MemberInfo memberInfo, SerializedProperty property, Func<string, SerializedProperty> findPropertyFunc)
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
                    if (memberInfo is FieldInfo f) isSerializedMember = f.IsPublic | f.HasCustomAttribute<SerializeField>() | f.HasCustomAttribute<SerializeReference>();
                    else if (memberInfo is PropertyInfo p) isSerializedMember = p.HasCustomAttribute<SerializeField>();

                    if (isSerializedMember)
                    {
                        // Create property field
                        if (property != null)
                        {
                            if (memberInfo is FieldInfo fieldInfo)
                            {
                                return new AlchemyPropertyField(property, fieldInfo.FieldType, false, false, memberInfo);
                            }
                            else
                            {
                                return new AlchemyPropertyField(property, ((PropertyInfo)memberInfo).PropertyType, false, false, memberInfo);
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
                            var dataName = "__alchemySerializationData_" + declaredType.FullName.Replace("`", "").Replace(".", "_");

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

    }
}
