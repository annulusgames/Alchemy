using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Editor
{
    internal static class ValueDropdownSource
    {
        sealed class Accessor
        {
            public Func<object, ValueDropdownContext, object> Get;
            public string Error;
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
            var type = attribute.SourceType ?? context.Owner?.GetType();
            if (type == null) throw new InvalidOperationException("The value provider has no owner.");
            var key = (type, attribute.ValuesGetter, attribute.SourceType != null);
            if (!accessors.TryGetValue(key, out var accessor))
            {
                accessor = Resolve(type, attribute.ValuesGetter, key.Item3);
                accessors.Add(key, accessor);
            }
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
                    return new Accessor { Get = Expression.Lambda<Func<object, ValueDropdownContext, object>>(Expression.Convert(body, typeof(object)), owner, context).Compile() };
                }
                throw new MissingMemberException(ownerType.FullName, name);
            }
            catch (Exception exception)
            {
                return new Accessor { Error = exception.Message };
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

        public ValueDropdownSnapshot(object source)
        {
            if (source is string || (source != null && source is not IEnumerable))
                throw new ArgumentException("A value provider must return an enumerable, not a scalar or string.");
            var options = source as ValueDropdownList<T>;
            comparer = options?.Comparer ?? EqualityComparer<T>.Default;
            factory = options?.ValueFactory;
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
                for (var i = 0; i < values.Count; i++)
                {
                    if (!Entries[i].Enabled) continue;
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
