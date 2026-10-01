using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Editor
{
    using Editor = UnityEditor.Editor;

    /// <summary>
    /// Call the Unity internal API using reflection. This may not work depending on your Unity version.
    /// </summary>
    internal static class InternalAPIHelper
    {
        static readonly Assembly EditorAssembly = Assembly.GetAssembly(typeof(Editor));

        // ScriptAttributeUtility
        // https://github.com/Unity-Technologies/UnityCsReference/blob/724ff727438a68d1bc05b342c693c1d481063fd3/Editor/Mono/Inspector/Core/ScriptAttributeGUI/ScriptAttributeUtility.cs

        const string Name_ScriptAttributeUtility = "UnityEditor.ScriptAttributeUtility";

        // Null is cached: most field types have no PropertyDrawer, and Unity's own lookup does not remember a miss.
        static readonly Dictionary<(Type, bool), Type> drawerTypeForTypeCache = new();
        static MethodInfo drawerTypeForTypeMethod;
        static DrawerTypeForTypeArguments drawerTypeForTypeArguments;
        static bool drawerTypeForTypeResolved;

        enum DrawerTypeForTypeArguments
        {
            TypeOnly,
            TypeAndManagedReference,
            TypeNullAndManagedReference,
        }

        public static Type GetDrawerTypeForType(Type classType, bool isManagedReferenceProperty)
        {
            var key = (classType, isManagedReferenceProperty);
            if (drawerTypeForTypeCache.TryGetValue(key, out var cached)) return cached;

            var drawerType = InvokeDrawerTypeForType(classType, isManagedReferenceProperty);
            drawerTypeForTypeCache[key] = drawerType;
            return drawerType;
        }

        static Type InvokeDrawerTypeForType(Type classType, bool isManagedReferenceProperty)
        {
            if (!drawerTypeForTypeResolved) ResolveDrawerTypeForType();

#if UNITY_2022_3_OR_NEWER && !UNITY_2023_2_OR_NEWER
            // 2022.3 and 2023.1 returned null when the internal method was missing.
            if (drawerTypeForTypeMethod == null) return null;
#endif
            return (Type)drawerTypeForTypeMethod.Invoke(null, CreateDrawerTypeArguments(classType, isManagedReferenceProperty));
        }

        static void ResolveDrawerTypeForType()
        {
            var utilityType = EditorAssembly.GetType(Name_ScriptAttributeUtility);
            drawerTypeForTypeMethod = utilityType.GetMethod(nameof(GetDrawerTypeForType), BindingFlags.NonPublic | BindingFlags.Static);

#if UNITY_2023_3_OR_NEWER
            drawerTypeForTypeArguments = DrawerTypeForTypeArguments.TypeNullAndManagedReference;
#elif UNITY_2023_2_OR_NEWER
            // Unity 2023.2.15f1 added a new parameter to the method
            var version = UnityEditorInternal.InternalEditorUtility.GetUnityVersion();
            drawerTypeForTypeArguments = version.Build >= 15
                ? DrawerTypeForTypeArguments.TypeAndManagedReference
                : DrawerTypeForTypeArguments.TypeOnly;
#elif UNITY_2022_3_OR_NEWER
            // Unity 2022.3.23f1 added a new parameter to the method
            var version = UnityEditorInternal.InternalEditorUtility.GetUnityVersion();
            drawerTypeForTypeArguments = version.Build >= 23
                ? DrawerTypeForTypeArguments.TypeAndManagedReference
                : DrawerTypeForTypeArguments.TypeOnly;
#else
            drawerTypeForTypeArguments = DrawerTypeForTypeArguments.TypeOnly;
#endif
            drawerTypeForTypeResolved = true;
        }

        static object[] CreateDrawerTypeArguments(Type classType, bool isManagedReferenceProperty)
        {
            switch (drawerTypeForTypeArguments)
            {
                case DrawerTypeForTypeArguments.TypeAndManagedReference:
                    return new object[] { classType, isManagedReferenceProperty };
                case DrawerTypeForTypeArguments.TypeNullAndManagedReference:
                    return new object[] { classType, null, isManagedReferenceProperty };
                default:
                    return new object[] { classType };
            }
        }

        const string Name_M_Clickable = "m_Clickable";

        // BaseBoolField
        // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/UIElements/Core/Controls/BaseBoolField.cs#L12

        public static Clickable GetClickable(BaseBoolField boolField)
        {
            var clickable = ReflectionHelper.GetField(typeof(Toggle), Name_M_Clickable).GetValue(boolField);
            return (Clickable)clickable;
        }

        // Clickable
        // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/UIElements/Core/Clickable.cs#L12

        const string Name_AcceptClicksIfDisabled = "acceptClicksIfDisabled";

        public static void SetAcceptClicksIfDisabled(Clickable clickable, bool value)
        {
            ReflectionHelper.GetProperty(typeof(Clickable), Name_AcceptClicksIfDisabled).SetValue(clickable, value);
        }

        // GUIUtility
        // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/IMGUI/GUIUtility.bindings.cs

        const string Name_CompositionString = "compositionString";

        static Func<string> compositionStringGetter;

        // IMGUI text editing reads the IME state here. Input.compositionString belongs to the legacy Input Manager,
        // which projects may disable in favor of the Input System package.
        public static string GetCompositionString()
        {
            if (compositionStringGetter == null)
            {
                var getter = typeof(GUIUtility).GetProperty(Name_CompositionString, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetGetMethod(true);
                compositionStringGetter = getter == null ? () => string.Empty : (Func<string>)getter.CreateDelegate(typeof(Func<string>));
            }
            return compositionStringGetter();
        }
    }
}
