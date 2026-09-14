using System;
using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Editor
{
    internal sealed class ValueDropdownSession
    {
        readonly ValueDropdownBinding binding;
        readonly ValueDropdownAttribute attribute;
        readonly ValueDropdownSnapshot[] snapshots;
        readonly object[] originalValues;
        readonly object[] owners;
        readonly int[] counts;
        readonly int revision;
        readonly int index;
        readonly bool adding;

        public ValueDropdownSession(ValueDropdownBinding binding, ValueDropdownAttribute attribute, int index, bool adding)
        {
            this.binding = binding;
            this.attribute = attribute;
            this.index = index;
            this.adding = adding;
            binding.Flush();
            if (!binding.CanWrite) throw new InvalidOperationException("The target is not editable.");
            if (adding && !binding.IsCollection || binding.IsCollection && !adding && (index < 0 || index >= binding.Count))
                throw new InvalidOperationException("The editing position is no longer valid.");
            revision = binding.Revision;
            snapshots = new ValueDropdownSnapshot[binding.TargetCount];
            originalValues = new object[binding.TargetCount];
            owners = new object[binding.TargetCount];
            counts = new int[binding.TargetCount];
            var existing = new Func<object, bool>[binding.TargetCount];
            for (var t = 0; t < snapshots.Length; t++)
            {
                var context = binding.Context(t, index, adding);
                owners[t] = context.Owner;
                originalValues[t] = context.CurrentValue;
                counts[t] = binding.IsCollection ? binding.Collection(t)?.Count ?? 0 : 0;
                snapshots[t] = ValueDropdownSource.Get(attribute, binding.ValueType, context);
                if (attribute.IsUniqueList && binding.IsCollection)
                    existing[t] = snapshots[t].ExistingValues(binding.Collection(t), adding ? -1 : index);
            }
            var primary = snapshots[0];
            Enabled = new bool[primary.Count];
            Choices.Capacity = primary.Count;
            for (var i = 0; i < primary.Count; i++)
            {
                if (snapshots.Length == 1 && existing[0] == null)
                {
                    Choices.Add(i);
                    Enabled[i] = primary.Entries[i].Enabled;
                    continue;
                }
                var value = primary.GetValue(i);
                var common = true;
                var enabled = primary.Entries[i].Enabled;
                for (var t = 0; t < snapshots.Length; t++)
                {
                    if (t != 0 && snapshots[t].Find(value) < 0) { common = false; break; }
                    if (existing[t] != null && existing[t](value)) enabled = false;
                }
                if (!common) continue;
                Choices.Add(i);
                Enabled[i] = enabled;
            }
        }

        public ValueDropdownSnapshot Snapshot => snapshots[0];
        public readonly List<int> Choices = new();
        public readonly bool[] Enabled;
        public bool Multiple => adding;
        public int CurrentChoice => adding || binding.IsMixed(index) ? -1 : Snapshot.Find(originalValues[0]);

        public void Validate()
        {
            binding.Flush();
            if (!binding.CanWrite || binding.Revision != revision) throw new InvalidOperationException("The target changed. Reopen the picker.");
            for (var t = 0; t < snapshots.Length; t++)
            {
                var owner = binding.Owner(t);
                if (owner != null && !owner.GetType().IsValueType && !ReferenceEquals(owner, owners[t]))
                    throw new InvalidOperationException("The provider owner changed. Reopen the picker.");
                if (binding.IsCollection && (binding.Collection(t)?.Count ?? 0) != counts[t])
                    throw new InvalidOperationException("The list changed. Reopen the picker.");
                if (!adding && !Equals(originalValues[t], binding.Read(t, index)))
                    throw new InvalidOperationException("The current value changed. Reopen the picker.");
            }
        }

        public void Commit(List<int> selected)
        {
            if (selected.Count == 0) return;
            Validate();
            selected.Sort();
            var values = new object[snapshots.Length][];
            for (var t = 0; t < snapshots.Length; t++)
            {
                values[t] = new object[selected.Count];
                for (var i = 0; i < selected.Count; i++)
                {
                    var choice = selected[i];
                    if (!Enabled[choice]) throw new InvalidOperationException("This candidate is unavailable.");
                    var local = t == 0 ? choice : snapshots[t].Find(Snapshot.GetValue(choice));
                    if (local < 0) throw new InvalidOperationException("The candidate is not available for every target.");
                    values[t][i] = snapshots[t].CreateValue(local);
                }
                if (attribute.IsUniqueList && binding.IsCollection)
                    snapshots[t].ValidateUnique(binding.Collection(t), adding ? -1 : index, values[t]);
            }
            // Factories may execute user code; check that they did not invalidate the editing position.
            Validate();
            if (adding) binding.Append(values);
            else
            {
                var scalar = new object[values.Length];
                for (var t = 0; t < scalar.Length; t++) scalar[t] = values[t][0];
                binding.Set(index, scalar);
            }
        }
    }

    internal sealed class ValueDropdownPopup : PopupWindowContent
    {
        sealed class Node
        {
            public string Name;
            public Node Parent;
            public int Choice = -1;
            public readonly List<Node> Children = new();
            public Dictionary<string, Node> Groups;
        }

        sealed class Row : VisualElement
        {
            public int Index;
            public readonly Label Mark = new();
            public readonly Label Text = new();
            public Row(Action<Row> clicked)
            {
                style.flexDirection = FlexDirection.Row;
                style.height = RowHeight;
                Mark.style.width = 20;
                Text.style.flexGrow = 1;
                Text.style.overflow = Overflow.Hidden;
                Text.style.textOverflow = TextOverflow.Ellipsis;
                Text.style.whiteSpace = WhiteSpace.NoWrap;
                Add(Mark);
                Add(Text);
                RegisterCallback<ClickEvent>(evt => { if (evt.button == 0) clicked(this); });
            }
        }

        const float RowHeight = 22;
        readonly VisualElement anchor;
        readonly ValueDropdownSession session;
        readonly ValueDropdownAttribute attribute;
        readonly Action<Exception> onError;
        readonly Action<int> onSelected;
        readonly Action onClosed;
        readonly int currentChoice;
        readonly List<Node> visible = new();
        readonly HashSet<int> selected = new();
        readonly Node root = new() { Name = "Select Value" };
        readonly List<Node> leaves = new();
        Node current;
        ListView list;
        ToolbarSearchField search;
        Button back;
        Button apply;
        Label empty;
        string query = string.Empty;
        bool closed;

        public ValueDropdownPopup(VisualElement anchor, ValueDropdownSession session, ValueDropdownAttribute attribute, Action<int> onSelected, Action<Exception> onError, Action onClosed)
        {
            this.anchor = anchor;
            this.session = session;
            this.attribute = attribute;
            this.onSelected = onSelected;
            this.onError = onError;
            this.onClosed = onClosed;
            currentChoice = session.CurrentChoice;
            root.Name = attribute.DropdownTitle ?? "Select Value";
            current = root;
            BuildTree();
        }

        void BuildTree()
        {
            foreach (var choice in session.Choices)
            {
                var text = session.Snapshot.Entries[choice].Text;
                var parent = root;
                var start = 0;
                if (!attribute.FlattenTreeView)
                {
                    for (var slash = text.IndexOf('/'); slash >= 0; slash = text.IndexOf('/', start))
                    {
                        var segment = text.Substring(start, slash - start);
                        start = slash + 1;
                        if (segment.Length == 0) continue;
                        parent.Groups ??= new Dictionary<string, Node>(StringComparer.Ordinal);
                        if (!parent.Groups.TryGetValue(segment, out var group))
                        {
                            group = new Node { Name = segment, Parent = parent };
                            parent.Groups.Add(segment, group);
                            parent.Children.Add(group);
                        }
                        parent = group;
                    }
                }
                var leaf = new Node { Name = text.Substring(start), Parent = parent, Choice = choice };
                parent.Children.Add(leaf);
                leaves.Add(leaf);
            }
        }

        public override Vector2 GetWindowSize() => new(Mathf.Clamp(anchor.worldBound.width, 280, 640), 360);
        public override void OnGUI(Rect rect) { }

        public override void OnOpen()
        {
            var content = editorWindow.rootVisualElement;
            content.style.paddingLeft = content.style.paddingRight = 4;
            content.style.paddingTop = content.style.paddingBottom = 4;
            back = new Button(() => { if (current.Parent != null) { current = current.Parent; Refresh(); } });
            content.Add(back);
            if (session.Choices.Count >= Math.Max(0, attribute.SearchThreshold))
            {
                search = new ToolbarSearchField();
                search.RegisterValueChangedCallback(evt =>
                {
                    query = evt.newValue ?? string.Empty;
                    Refresh();
                });
                content.Add(search);
            }
            list = new ListView
            {
                itemsSource = visible,
                fixedItemHeight = RowHeight,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                selectionType = SelectionType.Single,
                makeItem = () => new Row(Clicked),
                bindItem = (element, index) => Bind((Row)element, index),
            };
            list.style.flexGrow = 1;
            content.Add(list);
            empty = new Label("No available choices.");
            content.Add(empty);
            if (session.Multiple)
            {
                apply = new Button(Apply) { text = "Add selected" };
                content.Add(apply);
            }
            content.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            anchor.RegisterCallback<DetachFromPanelEvent>(OnDetached);
            Undo.undoRedoPerformed += Close;
            EditorApplication.hierarchyChanged += Close;
            EditorApplication.projectChanged += Close;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Refresh();
            if (search != null) search.Focus(); else list.Focus();
        }

        void Bind(Row row, int index)
        {
            row.Index = index;
            var node = visible[index];
            var isGroup = node.Choice < 0;
            row.Text.text = query.Length > 0 && !isGroup ? session.Snapshot.Entries[node.Choice].Text : node.Name;
            row.Mark.text = isGroup ? ">" : (selected.Contains(node.Choice) || !session.Multiple && node.Choice == currentChoice) ? "✓" : string.Empty;
            row.tooltip = isGroup ? node.Name : session.Snapshot.Entries[node.Choice].Tooltip;
            row.SetEnabled(isGroup || session.Enabled[node.Choice]);
        }

        void Refresh()
        {
            visible.Clear();
            if (query.Length == 0) visible.AddRange(current.Children);
            else
                foreach (var leaf in leaves)
                    if (session.Snapshot.Entries[leaf.Choice].Text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                        visible.Add(leaf);
            list.ClearSelection();
            list.RefreshItems();
            back.text = current.Parent == null ? root.Name : "< " + current.Name;
            back.SetEnabled(current.Parent != null && query.Length == 0);
            empty.style.display = visible.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            apply?.SetEnabled(selected.Count > 0);
        }

        void Clicked(Row row)
        {
            if (row.Index >= 0 && row.Index < visible.Count) Choose(visible[row.Index]);
        }

        void Choose(Node node)
        {
            if (node.Choice < 0) { current = node; Refresh(); return; }
            if (!session.Enabled[node.Choice]) return;
            if (!session.Multiple) { Commit(new List<int> { node.Choice }); return; }
            if (!selected.Remove(node.Choice))
            {
                if (attribute.IsUniqueList)
                {
                    // Alias labels for the same value must not create duplicate additions.
                    var equivalent = -1;
                    foreach (var other in selected)
                        if (session.Snapshot.Equal(session.Snapshot.GetValue(other), session.Snapshot.GetValue(node.Choice))) { equivalent = other; break; }
                    if (equivalent >= 0) selected.Remove(equivalent);
                }
                selected.Add(node.Choice);
            }
            list.RefreshItems();
            apply.SetEnabled(selected.Count > 0);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (!string.IsNullOrEmpty(Input.compositionString)) return;
            if (evt.keyCode == KeyCode.Escape) { Close(); evt.StopPropagation(); }
            else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                evt.PreventDefault();
                evt.StopPropagation();
                if (session.Multiple && (evt.ctrlKey || evt.commandKey)) Apply();
                else if (list.selectedIndex >= 0 && list.selectedIndex < visible.Count) Choose(visible[list.selectedIndex]);
            }
            else if (evt.keyCode == KeyCode.DownArrow && search != null && search.Contains(evt.target as VisualElement))
            {
                evt.PreventDefault();
                evt.StopPropagation();
                list.Focus();
                if (visible.Count > 0) list.SetSelection(0);
            }
            else if (evt.keyCode == KeyCode.LeftArrow && query.Length == 0 && current.Parent != null && list.Contains(evt.target as VisualElement))
            {
                current = current.Parent;
                Refresh();
                evt.StopPropagation();
            }
        }

        void Apply() => Commit(new List<int>(selected));
        void Commit(List<int> choices)
        {
            if (!anchor.enabledInHierarchy) { Close(); return; }
            try
            {
                session.Commit(choices);
                if (!session.Multiple && choices.Count != 0) onSelected?.Invoke(choices[0]);
            }
            catch (Exception exception) { onError?.Invoke(exception); }
            finally { Close(); }
        }

        void OnDetached(DetachFromPanelEvent _) => Close();
        void OnPlayModeChanged(PlayModeStateChange _) => Close();
        public void Close() { if (!closed && editorWindow != null) editorWindow.Close(); }
        public override void OnClose()
        {
            closed = true;
            anchor.UnregisterCallback<DetachFromPanelEvent>(OnDetached);
            Undo.undoRedoPerformed -= Close;
            EditorApplication.hierarchyChanged -= Close;
            EditorApplication.projectChanged -= Close;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            onClosed?.Invoke();
        }
    }
}
