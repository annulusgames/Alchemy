using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Alchemy.Inspector;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    internal static class ValueDropdownSource
    {
        sealed class Accessor
        {
            public Func<object, ValueDropdownContext, object> Get;
            public string Error;
            // Instance members and context methods can return different labels per owner.
            public bool VariesByOwner;
            // ValueDropdownContext methods can also vary by root, index, and adding.
            public bool VariesByContext;
        }

        static readonly Dictionary<(Type, string, bool), Accessor> accessors = new();
        static readonly Dictionary<Type, Func<object, ValueDropdownSnapshot>> builders = new();
        static readonly Dictionary<MemberInfo, ValueDropdownAttribute> attributes = new();

        public static ValueDropdownAttribute GetAttribute(MemberInfo member)
        {
            if (member == null) return null;
            if (!attributes.TryGetValue(member, out var attribute))
            {
                attribute = member.GetCustomAttribute<ValueDropdownAttribute>();
                attributes.Add(member, attribute);
            }
            return attribute;
        }

        public static ValueDropdownSnapshot Get(ValueDropdownAttribute attribute, Type valueType, ValueDropdownContext context)
        {
            var accessor = GetAccessor(attribute, context.Owner);
            if (accessor.Error != null) throw new InvalidOperationException(accessor.Error);
            var source = accessor.Get(context.Owner, context);
            if (!builders.TryGetValue(valueType, out var build))
            {
                var method = typeof(ValueDropdownSource).GetMethod(nameof(Build), BindingFlags.NonPublic | BindingFlags.Static);
                build = (Func<object, ValueDropdownSnapshot>)method.MakeGenericMethod(valueType)
                    .CreateDelegate(typeof(Func<object, ValueDropdownSnapshot>));
                builders.Add(valueType, build);
            }
            return build(source);
        }

        internal static bool TryGetLabelScope(ValueDropdownAttribute attribute, object owner, out ValueDropdownLabelScope scope)
        {
            scope = default;
            if (attribute == null) return false;
            var type = attribute.SourceType ?? owner?.GetType();
            if (type == null) return false;
            var accessor = GetAccessor(attribute, owner);
            if (accessor.Error != null) return false;
            scope = new ValueDropdownLabelScope(type, attribute.ValuesGetter, attribute.SourceType != null, accessor.VariesByOwner, accessor.VariesByContext);
            return true;
        }

        static Accessor GetAccessor(ValueDropdownAttribute attribute, object owner)
        {
            var type = attribute.SourceType ?? owner?.GetType();
            if (type == null) throw new InvalidOperationException("The value provider has no owner.");
            var key = (type, attribute.ValuesGetter, attribute.SourceType != null);
            if (!accessors.TryGetValue(key, out var accessor))
            {
                accessor = Resolve(type, attribute.ValuesGetter, key.Item3);
                accessors.Add(key, accessor);
            }
            return accessor;
        }

        static ValueDropdownSnapshot Build<T>(object source) => new ValueDropdownSnapshot<T>(source);

        static Accessor Resolve(Type ownerType, string name, bool staticOnly)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("ValuesGetter must name a member.");
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                for (var type = ownerType; type != null; type = type.BaseType)
                {
                    var members = type.GetMember(name, flags);
                    if (members.Length == 0) continue;
                    MemberInfo selected = null;
                    foreach (var member in members)
                    {
                        bool valid = member switch
                        {
                            FieldInfo f => !staticOnly || f.IsStatic,
                            PropertyInfo p => p.GetIndexParameters().Length == 0 && p.GetGetMethod(true) != null && (!staticOnly || p.GetGetMethod(true).IsStatic),
                            MethodInfo m => !m.ContainsGenericParameters && m.ReturnType != typeof(void) && (!staticOnly || m.IsStatic) &&
                                (m.GetParameters().Length == 0 || (m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(ValueDropdownContext))),
                            _ => false,
                        };
                        if (!valid) continue;
                        if (selected != null) throw new ArgumentException($"Provider '{name}' is ambiguous on {type.FullName}.");
                        selected = member;
                    }
                    if (selected == null) throw new ArgumentException($"'{name}' must be a readable member or a non-generic provider method with zero arguments or one ValueDropdownContext argument.");
                    var owner = Expression.Parameter(typeof(object), "owner");
                    var context = Expression.Parameter(typeof(ValueDropdownContext), "context");
                    Expression instance = Expression.Convert(owner, type);
                    Expression body = selected switch
                    {
                        FieldInfo f => Expression.Field(f.IsStatic ? null : instance, f),
                        PropertyInfo p => Expression.Property(p.GetGetMethod(true).IsStatic ? null : instance, p),
                        MethodInfo m => Expression.Call(m.IsStatic ? null : instance, m, m.GetParameters().Length == 0 ? Array.Empty<Expression>() : new Expression[] { context }),
                        _ => throw new InvalidOperationException(),
                    };
                    if (body.Type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(body.Type))
                        throw new ArgumentException($"Provider '{name}' must return IEnumerable (not a string).");
                    var variesByContext = selected is MethodInfo selectedMethod && selectedMethod.GetParameters().Length == 1;
                    var variesByOwner = variesByContext || selected switch
                    {
                        FieldInfo field => !field.IsStatic,
                        PropertyInfo property => !property.GetGetMethod(true).IsStatic,
                        MethodInfo method => !method.IsStatic,
                        _ => false,
                    };
                    return new Accessor
                    {
                        Get = Expression.Lambda<Func<object, ValueDropdownContext, object>>(Expression.Convert(body, typeof(object)), owner, context).Compile(),
                        VariesByOwner = variesByOwner,
                        VariesByContext = variesByContext,
                    };
                }
                throw new MissingMemberException(ownerType.FullName, name);
            }
            catch (Exception exception)
            {
                return new Accessor { Error = exception.Message };
            }
        }
    }

    internal readonly struct ValueDropdownLabelScope
    {
        public ValueDropdownLabelScope(Type type, string member, bool staticSource, bool variesByOwner, bool variesByContext)
        {
            Type = type;
            Member = member;
            StaticSource = staticSource;
            VariesByOwner = variesByOwner;
            VariesByContext = variesByContext;
        }

        public readonly Type Type;
        public readonly string Member;
        public readonly bool StaticSource;
        public readonly bool VariesByOwner;
        public readonly bool VariesByContext;
    }

    // Cross-inspector labels. Pure static providers share by value; anything that reads the owner or
    // ValueDropdownContext also keys by that owner (and by root, current value, index, and adding when the method takes context).
    // Unity objects are keyed by id so this cache does not keep them alive. Domain reload drops the static state.
    internal static class ValueDropdownLabels
    {
        internal const int Capacity = 1024;

        static readonly Dictionary<Key, string> entries = new();

        // After domain reload the dictionary is already empty; re-subscribe once events are live again.
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.projectChanged -= Clear;
            EditorApplication.projectChanged += Clear;
        }

        internal static int Count => entries.Count;

        internal static void Clear() => entries.Clear();

        public static bool TryGet(ValueDropdownAttribute attribute, Type valueType, ValueDropdownContext context, object value, out string text)
        {
            text = null;
            return TryMakeKey(attribute, valueType, context, value, out var key) && entries.TryGetValue(key, out text);
        }

        public static void Store(ValueDropdownAttribute attribute, Type valueType, ValueDropdownContext context, object value, string text)
        {
            if (!TryMakeKey(attribute, valueType, context, value, out var key)) return;
            if (!entries.ContainsKey(key) && entries.Count >= Capacity) EvictOne();
            entries[key] = text;
        }

        public static void StoreSnapshot(ValueDropdownAttribute attribute, Type valueType, ValueDropdownContext context, ValueDropdownSnapshot snapshot)
        {
            if (snapshot == null) return;
            for (var i = 0; i < snapshot.Count; i++)
            {
                var value = snapshot.GetValue(i);
                // Prefer the entry Find would return, so a disabled duplicate does not overwrite it.
                if (snapshot.Find(value) != i) continue;
                Store(attribute, valueType, context, value, snapshot.Entries[i].Text);
            }
        }

        internal static bool Retains(object target)
        {
            if (target == null) return false;
            foreach (var key in entries.Keys)
                if (key.Retains(target)) return true;
            return false;
        }

        static bool TryMakeKey(ValueDropdownAttribute attribute, Type valueType, ValueDropdownContext context, object value, out Key key)
        {
            key = default;
            if (valueType == null || !ValueDropdownSource.TryGetLabelScope(attribute, context.Owner, out var scope)) return false;
            key = new Key(scope, valueType, context, value);
            return true;
        }

        static void EvictOne()
        {
            var found = false;
            var extra = default(Key);
            foreach (var key in entries.Keys)
            {
                extra = key;
                found = true;
                break;
            }
            if (found) entries.Remove(extra);
        }

        readonly struct Key : IEquatable<Key>
        {
            readonly Type type;
            readonly string member;
            readonly Type valueType;
            readonly bool staticSource;
            readonly OwnerId owner;
            readonly OwnerId root;
            readonly ObjectId currentValue;
            readonly int index;
            readonly bool adding;
            readonly ObjectId value;

            public Key(ValueDropdownLabelScope scope, Type valueType, ValueDropdownContext context, object value)
            {
                type = scope.Type;
                member = scope.Member;
                this.valueType = valueType;
                staticSource = scope.StaticSource;
                var contextual = scope.VariesByOwner || scope.VariesByContext;
                owner = contextual ? new OwnerId(context.Owner) : default;
                root = scope.VariesByContext ? new OwnerId(context.Root) : default;
                currentValue = scope.VariesByContext ? ObjectId.From(context.CurrentValue) : ObjectId.None;
                index = scope.VariesByContext ? context.Index : 0;
                adding = scope.VariesByContext && context.IsAdding;
                this.value = ObjectId.From(value);
            }

            public bool Retains(object target) => owner.Holds(target) || root.Holds(target) || currentValue.Holds(target) || value.Holds(target);

            public bool Equals(Key other) =>
                type == other.type && member == other.member && valueType == other.valueType && staticSource == other.staticSource &&
                owner.Equals(other.owner) && root.Equals(other.root) && currentValue.Equals(other.currentValue) &&
                index == other.index && adding == other.adding && value.Equals(other.value);

            public override bool Equals(object obj) => obj is Key other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = type != null ? type.GetHashCode() : 0;
                    hash = (hash * 397) ^ (member != null ? member.GetHashCode() : 0);
                    hash = (hash * 397) ^ (valueType != null ? valueType.GetHashCode() : 0);
                    hash = (hash * 397) ^ staticSource.GetHashCode();
                    hash = (hash * 397) ^ owner.GetHashCode();
                    hash = (hash * 397) ^ root.GetHashCode();
                    hash = (hash * 397) ^ currentValue.GetHashCode();
                    hash = (hash * 397) ^ index;
                    hash = (hash * 397) ^ adding.GetHashCode();
                    return (hash * 397) ^ value.GetHashCode();
                }
            }
        }

        readonly struct OwnerId : IEquatable<OwnerId>
        {
            readonly object target;
            readonly ObjectId unity;

            public OwnerId(object target)
            {
                if (target is UnityEngine.Object)
                {
                    this.target = null;
                    unity = ObjectId.From(target);
                }
                else
                {
                    this.target = target;
                    unity = default;
                }
            }

            public bool Holds(object value) => value != null && ReferenceEquals(target, value);
            public bool Equals(OwnerId other) => ReferenceEquals(target, other.target) && unity.Equals(other.unity);
            public override bool Equals(object obj) => obj is OwnerId other && Equals(other);
            public override int GetHashCode() => unchecked(
                ((target == null ? 0 : RuntimeHelpers.GetHashCode(target)) * 397) ^ unity.GetHashCode());
        }

        readonly struct ObjectId : IEquatable<ObjectId>
        {
            enum Kind : byte { None, Null, Unity, Value }

            readonly Kind kind;
#if UNITY_6000_4_OR_NEWER
            readonly EntityId entityId;
#else
            readonly int instanceId;
#endif
            readonly object value;

            public static ObjectId None => default;

            public static ObjectId From(object target)
            {
                if (target == null) return new ObjectId(Kind.Null, default, null);
                if (target is UnityEngine.Object unity)
                {
#if UNITY_6000_4_OR_NEWER
                    return new ObjectId(Kind.Unity, unity.GetEntityId(), null);
#else
                    return new ObjectId(Kind.Unity, unity.GetInstanceID(), null);
#endif
                }
                return new ObjectId(Kind.Value, default, target);
            }

#if UNITY_6000_4_OR_NEWER
            ObjectId(Kind kind, EntityId entityId, object value)
#else
            ObjectId(Kind kind, int instanceId, object value)
#endif
            {
                this.kind = kind;
#if UNITY_6000_4_OR_NEWER
                this.entityId = entityId;
#else
                this.instanceId = instanceId;
#endif
                this.value = value;
            }

            public bool Holds(object target) => target != null && ReferenceEquals(value, target);

            public bool Equals(ObjectId other)
            {
                if (kind != other.kind) return false;
                if (kind == Kind.Value) return Equals(value, other.value);
#if UNITY_6000_4_OR_NEWER
                if (kind == Kind.Unity) return entityId.Equals(other.entityId);
#else
                if (kind == Kind.Unity) return instanceId == other.instanceId;
#endif
                return true;
            }

            public override bool Equals(object obj) => obj is ObjectId other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = (int)kind;
#if UNITY_6000_4_OR_NEWER
                    if (kind == Kind.Unity) hash = (hash * 397) ^ entityId.GetHashCode();
#else
                    if (kind == Kind.Unity) hash = (hash * 397) ^ instanceId;
#endif
                    if (kind == Kind.Value && value != null) hash = (hash * 397) ^ value.GetHashCode();
                    return hash;
                }
            }
        }
    }

    internal readonly struct ValueDropdownEntry
    {
        public ValueDropdownEntry(string text, string tooltip, bool enabled)
        {
            Text = text;
            Tooltip = tooltip;
            Enabled = enabled;
        }
        public readonly string Text;
        public readonly string Tooltip;
        public readonly bool Enabled;
    }

    internal abstract class ValueDropdownSnapshot
    {
        public readonly List<ValueDropdownEntry> Entries = new();
        public int Count => Entries.Count;
        public abstract object GetValue(int index);
        public abstract object CreateValue(int index);
        public abstract int Find(object value);
        public abstract bool Equal(object a, object b);
        public abstract Func<object, bool> ExistingValues(IList values, int exceptIndex);
        public abstract void ValidateUnique(IList existing, int exceptIndex, object[] additions);

        public static string Format(object value)
        {
            if (value == null) return "(Null)";
            if (value is UnityEngine.Object unityObject) return unityObject ? unityObject.name : "(Missing)";
            return value.ToString() ?? string.Empty;
        }
    }

    internal sealed class ValueDropdownSnapshot<T> : ValueDropdownSnapshot
    {
        readonly List<T> values;
        readonly IEqualityComparer<T> comparer;
        readonly Func<T, T> factory;
        Dictionary<T, int> indices;
        int nullIndex = -1;
        static readonly Dictionary<Type, MethodInfo> adapters = new();

        public ValueDropdownSnapshot(object source)
        {
            if (source is string || (source != null && source is not IEnumerable))
                throw new ArgumentException("A value provider must return an enumerable, not a scalar or string.");
            if (source is ValueDropdownList<T> options)
            {
                comparer = options.Comparer;
                factory = options.ValueFactory;
            }
            else if (source != null && GetAdapter(source.GetType()) is MethodInfo adapter)
            {
                var arguments = new[] { source, null, null };
                adapter.Invoke(null, arguments);
                comparer = (IEqualityComparer<T>)arguments[1];
                factory = (Func<T, T>)arguments[2];
            }
            comparer ??= EqualityComparer<T>.Default;
            var capacity = source is ICollection collection ? collection.Count : 0;
            values = new List<T>(capacity);
            Entries.Capacity = capacity;
            if (source is IEnumerable<ValueDropdownItem<T>> named)
            {
                foreach (var item in named) Add(item.Value, item.Text, item.Tooltip, item.Enabled);
            }
            else if (source is IEnumerable<T> typed)
            {
                foreach (var value in typed) Add(value, null, null, true);
            }
            else if (source is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item is IValueDropdownItem namedItem)
                        Add(Cast(namedItem.Value), namedItem.Text, namedItem.Tooltip, namedItem.Enabled);
                    else Add(Cast(item), null, null, true);
                }
            }
        }

        // Options for a derived value type, such as ValueDropdownList<FireEffect> for an IEffect field.
        static MethodInfo GetAdapter(Type sourceType)
        {
            if (adapters.TryGetValue(sourceType, out var adapter)) return adapter;
            if (sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(ValueDropdownList<>))
            {
                var valueType = sourceType.GetGenericArguments()[0];
                if (valueType != typeof(T) && !valueType.IsValueType && typeof(T).IsAssignableFrom(valueType))
                    adapter = typeof(ValueDropdownSnapshot<T>).GetMethod(nameof(Adapt), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(valueType);
            }
            adapters.Add(sourceType, adapter);
            return adapter;
        }

        static void Adapt<TDerived>(object source, out IEqualityComparer<T> comparer, out Func<T, T> factory) where TDerived : T
        {
            var options = (ValueDropdownList<TDerived>)source;
            comparer = options.Comparer == null ? null : new DerivedComparer<TDerived>(options.Comparer);
            var create = options.ValueFactory;
            factory = create == null ? null : value => value is TDerived derived ? create(derived) : value;
        }

        sealed class DerivedComparer<TDerived> : IEqualityComparer<T> where TDerived : T
        {
            readonly IEqualityComparer<TDerived> comparer;
            public DerivedComparer(IEqualityComparer<TDerived> comparer) => this.comparer = comparer;

            public bool Equals(T x, T y) => x is TDerived a && y is TDerived b ? comparer.Equals(a, b) : EqualityComparer<T>.Default.Equals(x, y);
            public int GetHashCode(T value) => value is TDerived derived ? comparer.GetHashCode(derived) : EqualityComparer<T>.Default.GetHashCode(value);
        }

        T Cast(object value)
        {
            if (value is T typed) return typed;
            if (value == null && default(T) is null) return default;
            throw new ArgumentException($"Candidate {values.Count} has type {value?.GetType().FullName ?? "null"}; expected {typeof(T).FullName}.");
        }

        void Add(T value, string text, string tooltip, bool enabled)
        {
            if (value is UnityEngine.Object unityObject && !unityObject) enabled = false;
            values.Add(value);
            Entries.Add(new ValueDropdownEntry(text ?? Format(value), tooltip, enabled));
        }

        public override object GetValue(int index) => values[index];
        public override object CreateValue(int index) => factory == null ? values[index] : factory(values[index]);
        public override bool Equal(object a, object b) => comparer.Equals(Cast(a), Cast(b));

        public override int Find(object value)
        {
            // Built lazily: a single-object picker does not need a hash table until matching a value.
            if (indices == null)
            {
                indices = new Dictionary<T, int>(values.Count, comparer);
                // Prefer an enabled entry for a value, but still match a value whose only entry is disabled.
                for (var pass = 0; pass < 2; pass++)
                    for (var i = 0; i < values.Count; i++)
                    {
                        if (Entries[i].Enabled != (pass == 0)) continue;
                        if (values[i] is null) { if (nullIndex < 0) nullIndex = i; }
                        else if (!indices.ContainsKey(values[i])) indices.Add(values[i], i);
                    }
            }
            var typed = Cast(value);
            if (typed is null) return nullIndex;
            return indices.TryGetValue(typed, out var index) ? index : -1;
        }

        HashSet<T> MakeExisting(IList list, int exceptIndex)
        {
            var set = new HashSet<T>(comparer);
            if (list != null)
                for (var i = 0; i < list.Count; i++)
                    if (i != exceptIndex) set.Add(Cast(list[i]));
            return set;
        }

        public override Func<object, bool> ExistingValues(IList values, int exceptIndex)
        {
            var set = MakeExisting(values, exceptIndex);
            return value => set.Contains(Cast(value));
        }

        public override void ValidateUnique(IList existing, int exceptIndex, object[] additions)
        {
            var set = MakeExisting(existing, exceptIndex);
            foreach (var value in additions)
                if (!set.Add(Cast(value))) throw new InvalidOperationException("The selection would introduce a duplicate value.");
        }
    }
}
