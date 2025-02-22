using System;
using System.Reflection;
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

        public static Type GetDrawerTypeForType(Type classType, bool isManagedReferenceProperty)
        {
            var instance = EditorAssembly.CreateInstance(Name_ScriptAttributeUtility);
            var utilityType = instance.GetType();

            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;
            var methodInfo = utilityType.GetMethod(nameof(GetDrawerTypeForType), bindingFlags);

#if UNITY_2023_3_OR_NEWER
            return (Type)methodInfo.Invoke(instance, new object[] { classType, null, isManagedReferenceProperty });
#elif UNITY_2023_2_OR_NEWER
            // Unity 2023.2.15f1 added a new parameter to the method
            var version = UnityEditorInternal.InternalEditorUtility.GetUnityVersion();
            if (version.Build >= 15)
            {
                return (Type)methodInfo.Invoke(instance, new object[] { classType, isManagedReferenceProperty });
            }
            else
            {
                return (Type)methodInfo.Invoke(instance, new object[] { classType });
            }
#elif UNITY_2022_3_OR_NEWER
            // Unity 2022.3.23f1 added a new parameter to the method
            var version = UnityEditorInternal.InternalEditorUtility.GetUnityVersion();
            if (version.Build >= 23)
            {
                return (Type)methodInfo?.Invoke(instance, new object[] { classType, isManagedReferenceProperty });
            }
            return (Type)methodInfo?.Invoke(instance, new object[] { classType });
#else
            _ = isManagedReferenceProperty; // discard
            return (Type)methodInfo.Invoke(instance, new object[] { classType });
#endif
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
    }
}