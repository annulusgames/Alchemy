using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Alchemy.Inspector;

#if ALCHEMY_SUPPORT_SERIALIZATION
using Alchemy.Serialization;
#endif

namespace Alchemy.Editor
{
    using Editor = UnityEditor.Editor;

    /// <summary>
    /// Editor base class for Inspector drawing in Alchemy
    /// </summary>
    public abstract class AlchemyEditor : Editor
    {
        const string ScriptFieldName = "m_Script";
#if ALCHEMY_SUPPORT_SERIALIZATION
        const string AlchemySerializationWarning = "In the current version, fields with the [AlchemySerializedField] attribute do not support editing multiple objects.";
#endif

        // One reflection walk per concrete type. Later selection changes only invoke these lists.
        static readonly Dictionary<Type, InspectorCallbackMethods> inspectorCallbackMethods = new();

        void OnEnable()
        {
            foreach (var target in targets)
            {
                foreach (var method in GetInspectorCallbackMethods(target.GetType()).Enable)
                {
                    method.Invoke(target, null);
                }
            }
        }

        void OnDisable()
        {
            foreach (var target in targets)
            {
                foreach (var method in GetInspectorCallbackMethods(target.GetType()).Disable)
                {
                    method.Invoke(target, null);
                }
            }
        }

        void OnDestroy()
        {
            foreach (var target in targets)
            {
                foreach (var method in GetInspectorCallbackMethods(target.GetType()).Destroy)
                {
                    method.Invoke(target, null);
                }
            }
        }

        static InspectorCallbackMethods GetInspectorCallbackMethods(Type type)
        {
            if (!inspectorCallbackMethods.TryGetValue(type, out var callbacks))
            {
                var enable = new List<MethodInfo>();
                var disable = new List<MethodInfo>();
                var destroy = new List<MethodInfo>();
                foreach (var method in ReflectionHelper.GetAllMethodsIncludingBaseNonPublic(type))
                {
                    if (method.HasCustomAttribute<OnInspectorEnableAttribute>())
                    {
                        enable.Add(method);
                    }

                    if (method.HasCustomAttribute<OnInspectorDisableAttribute>())
                    {
                        disable.Add(method);
                    }

                    if (method.HasCustomAttribute<OnInspectorDestroyAttribute>())
                    {
                        destroy.Add(method);
                    }
                }

                callbacks = new InspectorCallbackMethods(enable.ToArray(), disable.ToArray(), destroy.ToArray());
                inspectorCallbackMethods.Add(type, callbacks);
            }

            return callbacks;
        }

        readonly struct InspectorCallbackMethods
        {
            public readonly MethodInfo[] Enable;
            public readonly MethodInfo[] Disable;
            public readonly MethodInfo[] Destroy;

            public InspectorCallbackMethods(MethodInfo[] enable, MethodInfo[] disable, MethodInfo[] destroy)
            {
                Enable = enable;
                Disable = disable;
                Destroy = destroy;
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var targetType = target.GetType();

            if (targetType.HasCustomAttribute<DisableAlchemyEditorAttribute>())
            {
                // Create default inspector
                InspectorElement.FillDefaultInspector(root, serializedObject, this);
                return root;
            }

#if ALCHEMY_SUPPORT_SERIALIZATION
            if (targetType.HasCustomAttribute<AlchemySerializeAttribute>() && targets.Length > 1)
            {
                root.Add(new HelpBox(AlchemySerializationWarning, HelpBoxMessageType.Error));
            }
#endif

            // Add script field
            if (targetType.GetCustomAttribute<HideScriptFieldAttribute>() == null)
            {
                var scriptField = new PropertyField(serializedObject.FindProperty(ScriptFieldName));
                scriptField.SetEnabled(false);
                root.Add(scriptField);
                root.Add(new VisualElement()
                {
                    style = { height = EditorGUIUtility.standardVerticalSpacing * 0.5f }
                });
            }

            // Add elements
            InspectorHelper.BuildElements(serializedObject, root, target, name => serializedObject.FindProperty(name));

            return root;
        }
    }

#if !ALCHEMY_DISABLE_DEFAULT_EDITOR

    [CustomEditor(typeof(MonoBehaviour), editorForChildClasses: true, isFallback = true)]
    [CanEditMultipleObjects]
    internal sealed class MonoBehaviourEditor : AlchemyEditor { }

    [CustomEditor(typeof(ScriptableObject), editorForChildClasses: true, isFallback = true)]
    [CanEditMultipleObjects]
    internal sealed class ScriptableObjectEditor : AlchemyEditor { }

#endif

}
