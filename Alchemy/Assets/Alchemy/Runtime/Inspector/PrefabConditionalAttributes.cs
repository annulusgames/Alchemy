using System;

namespace Alchemy.Inspector
{
    /// <summary>
    /// Shows the member only when the inspected objects match the specified prefab contexts.
    /// </summary>
    /// <remarks>
    /// Every inspected object must match at least one flag. Targets without a GameObject context do not match,
    /// even with PrefabKind.All. PrefabKind.None never matches. Classification follows PrefabKind's current
    /// Unity prefab connections, including its Play Mode behavior. This attribute does not expose members;
    /// use ShowInInspector for nonserialized members or Button for methods.
    /// </remarks>
    /// <alchemy-attr-category>Conditionals</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class ShowInAttribute : Attribute
    {
        public ShowInAttribute(PrefabKind prefabKind) => PrefabKind = prefabKind;

        /// <summary>
        /// The prefab contexts in which the member can be shown. Combine flags with the bitwise OR operator.
        /// </summary>
        public PrefabKind PrefabKind { get; }
    }

    /// <summary>
    /// Hides the member when any inspected object matches the specified prefab contexts.
    /// </summary>
    /// <remarks>
    /// Targets without a GameObject context do not match. PrefabKind.None has no effect.
    /// Classification follows PrefabKind's current Unity prefab connections, including its Play Mode behavior.
    /// Other visibility conditions still apply outside the specified contexts.
    /// </remarks>
    /// <alchemy-attr-category>Conditionals</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class HideInAttribute : Attribute
    {
        public HideInAttribute(PrefabKind prefabKind) => PrefabKind = prefabKind;

        /// <summary>
        /// The prefab contexts in which the member is hidden. Combine flags with the bitwise OR operator.
        /// </summary>
        public PrefabKind PrefabKind { get; }
    }

    /// <summary>
    /// Allows editing the member only when the inspected objects match the specified prefab contexts.
    /// </summary>
    /// <remarks>
    /// Every inspected object must match at least one flag. Targets without a GameObject context do not match,
    /// even with PrefabKind.All. PrefabKind.None never matches. Classification follows PrefabKind's current
    /// Unity prefab connections, including its Play Mode behavior. ReadOnly and other disabling conditions
    /// remain effective even when the prefab context matches.
    /// </remarks>
    /// <alchemy-attr-category>Conditionals</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class EnableInAttribute : Attribute
    {
        public EnableInAttribute(PrefabKind prefabKind) => PrefabKind = prefabKind;

        /// <summary>
        /// The prefab contexts in which the member can be edited. Combine flags with the bitwise OR operator.
        /// </summary>
        public PrefabKind PrefabKind { get; }
    }

    /// <summary>
    /// Disables the member when any inspected object matches the specified prefab contexts.
    /// </summary>
    /// <remarks>
    /// Targets without a GameObject context do not match. PrefabKind.None has no effect.
    /// Classification follows PrefabKind's current Unity prefab connections, including its Play Mode behavior.
    /// ReadOnly and other disabling conditions remain effective outside the specified contexts.
    /// </remarks>
    /// <alchemy-attr-category>Conditionals</alchemy-attr-category>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class DisableInAttribute : Attribute
    {
        public DisableInAttribute(PrefabKind prefabKind) => PrefabKind = prefabKind;

        /// <summary>
        /// The prefab contexts in which the member is disabled. Combine flags with the bitwise OR operator.
        /// </summary>
        public PrefabKind PrefabKind { get; }
    }
}
