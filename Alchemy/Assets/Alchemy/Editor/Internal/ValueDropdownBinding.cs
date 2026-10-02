using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Alchemy.Inspector;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Editor
{
    // Contains no candidate data. Instances belong to a field, not a static cache.
    internal sealed class ValueDropdownBinding
    {
        readonly ValueDropdownPath path;
        readonly object owner;
        readonly Func<object> read;
        readonly Action<object> assign;
        readonly Action beforeWrite;
        readonly Action<object> afterWrite;
        readonly UnityEngine.Object[] targets;
        readonly OnListViewChangedAttribute listEvents;
        // Display labels by index. Survives row rebinding; each entry is valid only while its value is current.
        readonly Dictionary<int, KeyValuePair<object, string>> labels = new();
        uint ownChangeHash;
        bool ownChangePending;

        public ValueDropdownBinding(SerializedProperty property, MemberInfo member, Type type)
        {
            SerializedObject = property.serializedObject;
            PropertyPath = property.propertyPath;
            targets = SerializedObject.targetObjects;
            path = new ValueDropdownPath(PropertyPath);
            Member = member;
            DeclaredType = type;
            listEvents = member.GetCustomAttribute<OnListViewChangedAttribute>();
            InitializeTypes();
        }

        // A null assign marks a member that cannot be reassigned; its live list can still be edited in place.
        public ValueDropdownBinding(object owner, MemberInfo member, Type type, Func<object> read, Action<object> assign, Action beforeWrite, Action<object> afterWrite)
        {
            this.owner = owner;
            this.read = read;
            this.assign = assign;
            this.beforeWrite = beforeWrite;
            this.afterWrite = afterWrite;
            Member = member;
            DeclaredType = type;
            listEvents = member.GetCustomAttribute<OnListViewChangedAttribute>();
            InitializeTypes();
        }

        void InitializeTypes()
        {
            IsCollection = DeclaredType.IsArray && DeclaredType.GetArrayRank() == 1 ||
                DeclaredType.IsGenericType && DeclaredType.GetGenericTypeDefinition() == typeof(List<>);
            ValueType = IsCollection ? (DeclaredType.IsArray ? DeclaredType.GetElementType() : DeclaredType.GetGenericArguments()[0]) : DeclaredType;
        }

        public SerializedObject SerializedObject { get; }
        public string PropertyPath { get; }
        public MemberInfo Member { get; }
        public Type DeclaredType { get; }
        public Type ValueType { get; private set; }
        public bool IsCollection { get; private set; }
        public int TargetCount => targets?.Length ?? 1;
        public int Revision { get; private set; }
        public event Action Changed;
        public string Key => PropertyPath ?? Member.Name;

        public bool CanWrite
        {
            get
            {
                if (targets == null)
                {
                    if (owner is UnityEngine.Object unityObject && !unityObject) return false;
                    return assign != null || HasStableCollection();
                }
                foreach (var target in targets) if (!target) return false;
                using var property = FindProperty();
                return property != null && property.editable;
            }
        }

        // Arrays change length only by assigning a new instance.
        public bool CanResize => CanWrite && (targets != null || assign != null || Collection(0) is { IsFixedSize: false });

        // A member that cannot be reassigned is editable only through a list it keeps. A getter that builds
        // a new list on each read would silently drop the edits.
        bool HasStableCollection()
        {
            if (!IsCollection) return false;
            var collection = Collection(0);
            return collection is { IsReadOnly: false } && ReferenceEquals(collection, Collection(0));
        }

        public bool IsPrefabOverride(int index)
        {
            if (SerializedObject == null) return false;
            using var property = FindProperty(index);
            return property != null && property.prefabOverride;
        }

        public SerializedProperty FindProperty(int index = -1)
        {
            if (SerializedObject == null) return null;
            var property = SerializedObject.FindProperty(PropertyPath);
            if (index < 0) return property;
            if (property == null || index >= property.arraySize) { property?.Dispose(); return null; }
            var element = property.GetArrayElementAtIndex(index);
            property.Dispose();
            return element;
        }

        public object Root(int target) => targets == null ? null : targets[target];
        public object Owner(int target) => targets == null ? owner : path.GetOwner(targets[target]);
        object ReadMember(int target) => targets == null ? read() : path.Get(targets[target]);
        public IList Collection(int target) => ReadMember(target) as IList;
        public object Read(int target, int index = -1) => index < 0 ? ReadMember(target) : Collection(target)[index];

        public int Count
        {
            get
            {
                var count = int.MaxValue;
                for (var i = 0; i < TargetCount; i++) count = Math.Min(count, Collection(i)?.Count ?? 0);
                return count == int.MaxValue ? 0 : count;
            }
        }

        public bool IsMixed(int index)
        {
            if (TargetCount == 1) return false;
            using (var property = FindProperty(index))
            {
                // Compares serialized content, so equal class and managed-reference values are not mixed.
                if (property != null) return HasMultipleDifferentValues(property);
            }
            var first = Read(0, index);
            for (var i = 1; i < TargetCount; i++) if (!Equals(first, Read(i, index))) return true;
            return false;
        }

        // A parent's own flag does not report differences in its children.
        static bool HasMultipleDifferentValues(SerializedProperty property)
        {
            if (property.hasMultipleDifferentValues) return true;
            if (property.propertyType != SerializedPropertyType.Generic && property.propertyType != SerializedPropertyType.ManagedReference) return false;
            using var child = property.Copy();
            using var end = property.GetEndProperty();
            while (child.Next(true) && !SerializedProperty.EqualContents(child, end))
                if (child.hasMultipleDifferentValues) return true;
            return false;
        }

        public string DisplayText(int index, ValueDropdownAttribute attribute)
        {
            if (IsMixed(index)) return "—";
            var value = Read(0, index);
            var known = labels.TryGetValue(index, out var label);
            if (known && Equals(label.Key, value)) return label.Value;
            var context = Context(0, index, false);
            // An index already resolved on this binding is evaluated again when its value changes.
            // New rows and later inspectors reuse labels warmed from the provider snapshot.
            if (!known && ValueDropdownLabels.TryGet(attribute, ValueType, context, value, out var cached))
            {
                labels[index] = new KeyValuePair<object, string>(value, cached);
                return cached;
            }
            string text;
            try
            {
                // Evaluated on a miss, not per repaint. A non-context snapshot warms the other values too.
                var snapshot = ValueDropdownSource.Get(attribute, ValueType, context);
                var found = snapshot.Find(value);
                text = found >= 0 ? snapshot.Entries[found].Text : ValueDropdownSnapshot.Format(value);
                ValueDropdownLabels.StoreSnapshot(attribute, ValueType, context, snapshot);
                ValueDropdownLabels.Store(attribute, ValueType, context, value, text);
            }
            catch (Exception)
            {
                // Showing the raw value is enough here; opening the picker reports provider errors.
                text = ValueDropdownSnapshot.Format(value);
            }
            labels[index] = new KeyValuePair<object, string>(value, text);
            return text;
        }

        public void RememberLabel(int index, string text)
        {
            var value = Read(0, index);
            labels[index] = new KeyValuePair<object, string>(value, text);
            var attribute = ValueDropdownSource.GetAttribute(Member);
            if (attribute != null) ValueDropdownLabels.Store(attribute, ValueType, Context(0, index, false), value, text);
        }

        public ValueDropdownContext Context(int target, int index, bool adding) =>
            new(Root(target), Owner(target), adding ? null : Read(target, index), adding ? -1 : index, adding);

        public void Track(VisualElement host)
        {
            if (SerializedObject != null)
            {
                var property = FindProperty();
                if (property != null) host.TrackPropertyValue(property, changed =>
                {
                    // The tracker also reports edits made through this binding, which were already announced.
                    var own = ownChangePending && changed.contentHash == ownChangeHash;
                    ownChangePending = false;
                    if (!own) NotifyChanged();
                });
            }
        }

        public void Flush()
        {
            if (SerializedObject == null) return;
            // Never discard pending changes from another bound field by calling Update first.
            if (SerializedObject.hasModifiedProperties) SerializedObject.ApplyModifiedProperties();
            SerializedObject.UpdateIfRequiredOrScript();
        }

        void NotifyChanged()
        {
            Revision++;
            Changed?.Invoke();
        }

        void NotifyOwnChange()
        {
            using (var property = FindProperty())
            {
                ownChangePending = property != null;
                ownChangeHash = property?.contentHash ?? 0;
            }
            NotifyChanged();
        }

        public void Set(int index, object[] values, string label = null)
        {
            if (values.Length != TargetCount || IsCollection && (index < 0 || index >= Count) || !IsCollection && index != -1)
                throw new InvalidOperationException("The editing position is no longer valid.");
            for (var i = 0; i < values.Length; i++) ValidateValue(values[i], i);
            if (label != null) labels[index] = new KeyValuePair<object, string>(values[0], label);
            Mutate((property, target) =>
            {
                if (index >= 0)
                {
                    if (index >= property.arraySize) throw new InvalidOperationException("The list changed while the picker was open.");
                    using var element = property.GetArrayElementAtIndex(index);
                    ValueDropdownWriter.Write(element, values[target], ValueType);
                }
                else ValueDropdownWriter.Write(property, values[target], ValueType);
            }, () =>
            {
                if (index < 0) return values[0];
                var list = CopyCollection(Collection(0), Collection(0).Count);
                list[index] = values[0];
                return list;
            });
            if (index >= 0 && listEvents?.OnItemChanged != null)
                for (var t = 0; t < TargetCount; t++)
                    ReflectionHelper.Invoke(Owner(t), listEvents.OnItemChanged, new object[] { index, values[t] });
        }

        public void Append(object[][] values, string[] labels = null)
        {
            if (!IsCollection || values.Length != TargetCount) throw new InvalidOperationException("Invalid list destination.");
            var starts = new int[TargetCount];
            for (var t = 0; t < TargetCount; t++)
            {
                starts[t] = Collection(t)?.Count ?? 0;
                foreach (var value in values[t]) ValidateValue(value, t);
            }
            if (labels != null)
                for (var i = 0; i < labels.Length; i++)
                    this.labels[starts[0] + i] = new KeyValuePair<object, string>(values[0][i], labels[i]);
            Mutate((property, target) =>
            {
                var start = property.arraySize;
                property.arraySize = start + values[target].Length;
                for (var i = 0; i < values[target].Length; i++)
                {
                    using var element = property.GetArrayElementAtIndex(start + i);
                    ValueDropdownWriter.Write(element, values[target][i], ValueType);
                }
            }, () =>
            {
                var list = CopyCollection(Collection(0), starts[0] + values[0].Length);
                for (var i = 0; i < values[0].Length; i++) list[starts[0] + i] = values[0][i];
                return list;
            });
            if (listEvents?.OnItemsAdded != null)
                for (var t = 0; t < TargetCount; t++)
                {
                    var indices = new int[values[t].Length];
                    for (var i = 0; i < indices.Length; i++) indices[i] = starts[t] + i;
                    ReflectionHelper.Invoke(Owner(t), listEvents.OnItemsAdded, new object[] { indices });
                }
        }

        public void AppendDefault()
        {
            if (!IsCollection) throw new InvalidOperationException("Invalid list destination.");
            var starts = new int[TargetCount];
            for (var t = 0; t < TargetCount; t++) starts[t] = Collection(t)?.Count ?? 0;
            Mutate((property, _) =>
            {
                property.arraySize++;
                // Growing a serialized array copies the last element. Start from a default value, as reflected lists do.
                using var element = property.GetArrayElementAtIndex(property.arraySize - 1);
                ValueDropdownWriter.Reset(element);
            }, () => CopyCollection(Collection(0), starts[0] + 1));
            if (listEvents?.OnItemsAdded != null)
                for (var t = 0; t < TargetCount; t++)
                    ReflectionHelper.Invoke(Owner(t), listEvents.OnItemsAdded, new object[] { new[] { starts[t] } });
        }

        public void Remove(int[] indices)
        {
            if (indices.Length == 0) return;
            Array.Sort(indices);
            if (indices[0] < 0 || indices[indices.Length - 1] >= Count) throw new InvalidOperationException("The list changed before removal.");
            labels.Clear();
            Mutate((property, _) =>
            {
                for (var i = indices.Length - 1; i >= 0; i--)
                {
                    var size = property.arraySize;
                    property.DeleteArrayElementAtIndex(indices[i]);
                    // Object-reference arrays may first clear the reference without shrinking.
                    if (property.arraySize == size) property.DeleteArrayElementAtIndex(indices[i]);
                }
            }, () =>
            {
                var old = Collection(0);
                var list = CopyCollection(null, old.Count - indices.Length);
                for (int i = 0, destination = 0, remove = 0; i < old.Count; i++)
                {
                    if (remove < indices.Length && indices[remove] == i) { remove++; continue; }
                    list[destination++] = old[i];
                }
                return list;
            });
            InvokeForTargets(listEvents?.OnItemsRemoved, new object[] { indices });
        }

        public void Move(int from, int to)
        {
            if (from == to) return;
            if (from < 0 || to < 0 || from >= Count || to >= Count) throw new InvalidOperationException("The list changed before reordering.");
            labels.Clear();
            Mutate((property, _) => property.MoveArrayElement(from, to), () =>
            {
                var list = CopyCollection(Collection(0), Collection(0).Count);
                var item = list[from];
                var step = from < to ? 1 : -1;
                for (var i = from; i != to; i += step) list[i] = list[i + step];
                list[to] = item;
                return list;
            });
            InvokeForTargets(listEvents?.OnItemIndexChanged, new object[] { from, to });
        }

        public void Revert(int index)
        {
            if (SerializedObject == null || !CanWrite) return;
            Flush();
            var path = index < 0 ? PropertyPath : PropertyPath + ".Array.data[" + index.ToString(CultureInfo.InvariantCulture) + "]";
            for (var i = 0; i < TargetCount; i++)
            {
                using var stream = new SerializedObject(targets[i], SerializedObject.context);
                using var property = stream.FindProperty(path);
                if (property != null && property.prefabOverride)
                    PrefabUtility.RevertPropertyOverride(property, InteractionMode.UserAction);
            }
            SerializedObject.Update();
            labels.Clear();
            NotifyOwnChange();
        }

        void ValidateValue(object value, int target)
        {
            if (value == null)
            {
                if (ValueType.IsValueType && Nullable.GetUnderlyingType(ValueType) == null)
                    throw new ArgumentException($"Null cannot be assigned to {ValueType.FullName}.");
            }
            else if (!ValueType.IsInstanceOfType(value)) throw new ArgumentException($"Cannot assign {value.GetType().FullName} to {ValueType.FullName}.");
            if (value is UnityEngine.Object unityObject)
            {
                if (!unityObject) throw new InvalidOperationException("The selected Unity object was destroyed.");
                if (!EditorUtility.IsPersistent(unityObject) &&
                    (Member.IsDefined(typeof(AssetsOnlyAttribute), true) || targets != null && EditorUtility.IsPersistent(targets[target])))
                    throw new InvalidOperationException("This field cannot store a scene-object reference.");
            }
        }

        IList CopyCollection(IList old, int size)
        {
            IList result = DeclaredType.IsArray ? Array.CreateInstance(ValueType, size) : (IList)Activator.CreateInstance(DeclaredType);
            if (!DeclaredType.IsArray)
                for (var i = 0; i < size; i++) result.Add(ValueType.IsValueType ? Activator.CreateInstance(ValueType) : null);
            if (old != null)
                for (var i = 0; i < Math.Min(size, old.Count); i++) result[i] = old[i];
            return result;
        }

        void Mutate(Action<SerializedProperty, int> edit, Func<object> reflectedValue)
        {
            if (!CanWrite) throw new InvalidOperationException("The target is no longer editable.");
            if (SerializedObject == null)
            {
                var value = reflectedValue();
                // Update a live list in place so readonly members and other holders of the list see the change.
                var live = IsCollection ? Collection(0) : null;
                var inPlace = live != null && value is IList items && !live.IsReadOnly && (!live.IsFixedSize || live.Count == items.Count);
                if (!inPlace && assign == null) throw new InvalidOperationException("This array cannot be resized because its member cannot be assigned.");
                beforeWrite?.Invoke();
                if (inPlace)
                {
                    CopyInto(live, (IList)value);
                    value = live;
                }
                assign?.Invoke(value);
                try { afterWrite?.Invoke(value); }
                finally { NotifyChanged(); }
                return;
            }
            Flush();
            var streams = new SerializedObject[TargetCount];
            try
            {
                // Stage every target before applying any. Failed type/shape checks leave targets untouched.
                for (var i = 0; i < TargetCount; i++)
                {
                    streams[i] = new SerializedObject(targets[i], SerializedObject.context);
                    using var property = streams[i].FindProperty(PropertyPath);
                    if (property == null || !property.editable) throw new InvalidOperationException("The target property is no longer editable.");
                    edit(property, i);
                }
                Undo.IncrementCurrentGroup();
                var undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Select " + Member.Name);
                try { foreach (var stream in streams) stream.ApplyModifiedProperties(); }
                finally { Undo.CollapseUndoOperations(undoGroup); }
                SerializedObject.Update();
            }
            finally { foreach (var stream in streams) stream?.Dispose(); }
            NotifyOwnChange();
        }

        static void CopyInto(IList destination, IList items)
        {
            while (destination.Count > items.Count) destination.RemoveAt(destination.Count - 1);
            for (var i = 0; i < items.Count; i++)
            {
                if (i < destination.Count) destination[i] = items[i];
                else destination.Add(items[i]);
            }
        }

        void InvokeForTargets(string method, object[] arguments)
        {
            if (method == null) return;
            for (var i = 0; i < TargetCount; i++) ReflectionHelper.Invoke(Owner(i), method, arguments);
        }
    }

    internal sealed class ValueDropdownPath
    {
        readonly struct Part
        {
            public Part(string name, int index) { Name = name; Index = index; }
            public readonly string Name;
            public readonly int Index;
        }

        readonly Part[] parts;
        readonly int ownerLength;

        public ValueDropdownPath(string path)
        {
            var steps = new List<Part>();
            foreach (var part in path.Replace(".Array.data[", "[").Split('.'))
            {
                var bracket = part.IndexOf('[');
                ownerLength = steps.Count;
                steps.Add(new Part(bracket < 0 ? part : part.Substring(0, bracket), -1));
                if (bracket >= 0) steps.Add(new Part(null, int.Parse(part.Substring(bracket + 1, part.Length - bracket - 2), CultureInfo.InvariantCulture)));
            }
            parts = steps.ToArray();
        }

        public object Get(object root) => Get(root, parts.Length);
        public object GetOwner(object root) => Get(root, ownerLength);

        object Get(object value, int length)
        {
            for (var i = 0; i < length; i++)
            {
                if (value == null) throw new InvalidOperationException("A parent object is null.");
                value = parts[i].Name == null ? ((IList)value)[parts[i].Index] : Field(value.GetType(), parts[i].Name).GetValue(value);
            }
            return value;
        }

        public static FieldInfo Field(Type type, string name) =>
            ReflectionHelper.GetField(type, name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, includingBaseNonPublic: true)
            ?? throw new MissingFieldException(type.FullName, name);
    }

    internal static class ValueDropdownWriter
    {
        static Action<SerializedProperty, Gradient> gradientSetter;

        static void SetGradient(SerializedProperty property, Gradient value)
        {
            // The setter is internal in older supported editors. Resolve it once, not per selection.
            if (gradientSetter == null)
            {
                var info = typeof(SerializedProperty).GetProperty("gradientValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var setter = info?.GetSetMethod(true);
                if (setter == null) throw new NotSupportedException("This editor cannot write Gradient properties.");
                gradientSetter = (Action<SerializedProperty, Gradient>)setter.CreateDelegate(typeof(Action<SerializedProperty, Gradient>));
            }
            gradientSetter(property, value);
        }

        public static void Reset(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum: property.longValue = 0; return;
                case SerializedPropertyType.Character:
                case SerializedPropertyType.LayerMask: property.intValue = 0; return;
                case SerializedPropertyType.Boolean: property.boolValue = false; return;
                case SerializedPropertyType.Float: property.doubleValue = 0; return;
                case SerializedPropertyType.String: property.stringValue = string.Empty; return;
                case SerializedPropertyType.Color: property.colorValue = default; return;
                case SerializedPropertyType.ObjectReference: property.objectReferenceValue = null; return;
                case SerializedPropertyType.ExposedReference: property.exposedReferenceValue = null; return;
                case SerializedPropertyType.Vector2: property.vector2Value = default; return;
                case SerializedPropertyType.Vector3: property.vector3Value = default; return;
                case SerializedPropertyType.Vector4: property.vector4Value = default; return;
                case SerializedPropertyType.Vector2Int: property.vector2IntValue = default; return;
                case SerializedPropertyType.Vector3Int: property.vector3IntValue = default; return;
                case SerializedPropertyType.Rect: property.rectValue = default; return;
                case SerializedPropertyType.RectInt: property.rectIntValue = default; return;
                case SerializedPropertyType.Bounds: property.boundsValue = default; return;
                case SerializedPropertyType.BoundsInt: property.boundsIntValue = default; return;
                case SerializedPropertyType.Quaternion: property.quaternionValue = default; return;
                case SerializedPropertyType.AnimationCurve: property.animationCurveValue = new AnimationCurve(); return;
                case SerializedPropertyType.Gradient: SetGradient(property, new Gradient()); return;
                case SerializedPropertyType.Hash128: property.hash128Value = default; return;
                case SerializedPropertyType.ManagedReference: property.managedReferenceValue = null; return;
                case SerializedPropertyType.Generic:
                    if (property.isArray)
                    {
                        property.arraySize = 0;
                        return;
                    }
                    using (var child = property.Copy())
                    {
                        if (!child.Next(true)) return;
                        do
                        {
                            if (child.depth <= property.depth) break;
                            Reset(child);
                        } while (child.Next(false));
                    }
                    return;
            }
        }

        public static void Write(SerializedProperty property, object value, Type type)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                    var numericType = type.IsEnum ? Enum.GetUnderlyingType(type) : type;
                    property.longValue = numericType == typeof(ulong) ? unchecked((long)Convert.ToUInt64(value)) : Convert.ToInt64(value);
                    return;
                case SerializedPropertyType.Boolean: property.boolValue = (bool)value; return;
                case SerializedPropertyType.Float: property.doubleValue = Convert.ToDouble(value); return;
                case SerializedPropertyType.String: property.stringValue = (string)value; return;
                case SerializedPropertyType.Character: property.intValue = (char)value; return;
                case SerializedPropertyType.Color: property.colorValue = (Color)value; return;
                case SerializedPropertyType.ObjectReference: property.objectReferenceValue = (UnityEngine.Object)value; return;
                case SerializedPropertyType.LayerMask: property.intValue = (LayerMask)value; return;
                case SerializedPropertyType.Vector2: property.vector2Value = (Vector2)value; return;
                case SerializedPropertyType.Vector3: property.vector3Value = (Vector3)value; return;
                case SerializedPropertyType.Vector4: property.vector4Value = (Vector4)value; return;
                case SerializedPropertyType.Vector2Int: property.vector2IntValue = (Vector2Int)value; return;
                case SerializedPropertyType.Vector3Int: property.vector3IntValue = (Vector3Int)value; return;
                case SerializedPropertyType.Rect: property.rectValue = (Rect)value; return;
                case SerializedPropertyType.RectInt: property.rectIntValue = (RectInt)value; return;
                case SerializedPropertyType.Bounds: property.boundsValue = (Bounds)value; return;
                case SerializedPropertyType.BoundsInt: property.boundsIntValue = (BoundsInt)value; return;
                case SerializedPropertyType.Quaternion: property.quaternionValue = (Quaternion)value; return;
                case SerializedPropertyType.AnimationCurve: property.animationCurveValue = (AnimationCurve)value; return;
                case SerializedPropertyType.Gradient: SetGradient(property, (Gradient)value); return;
                case SerializedPropertyType.Hash128: property.hash128Value = (Hash128)value; return;
                case SerializedPropertyType.ManagedReference: property.managedReferenceValue = value; return;
                case SerializedPropertyType.Generic:
                    if (property.isArray)
                    {
                        var values = value as IList;
                        property.arraySize = values?.Count ?? 0;
                        var elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                        for (var i = 0; i < property.arraySize; i++)
                        {
                            using var element = property.GetArrayElementAtIndex(i);
                            Write(element, values[i], elementType);
                        }
                        return;
                    }
                    if (value == null) throw new InvalidOperationException("Inline serialized objects cannot store null. Use SerializeReference for nullable reference values.");
                    using (var child = property.Copy())
                    {
                        if (!child.Next(true)) return;
                        do
                        {
                            if (child.depth <= property.depth) break;
                            var field = ValueDropdownPath.Field(value.GetType(), child.name);
                            Write(child, field.GetValue(value), field.FieldType);
                        } while (child.Next(false));
                    }
                    return;
                default: throw new NotSupportedException($"ValueDropdown does not support writing {property.propertyType}.");
            }
        }
    }
}
