using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Alchemy.Inspector;
using UnityEditor;
using UnityEngine.UIElements;

namespace Alchemy.Editor.Elements
{
    internal sealed class PrefabConditionalElement : VisualElement
    {
        static readonly ConditionalWeakTable<VisualElement, UnityEngine.Object[]> inspectorTargets = new();
        readonly UnityEngine.Object[] targets;
        readonly PrefabKind? showIn;
        readonly PrefabKind hideIn;
        readonly PrefabKind? enableIn;
        readonly PrefabKind disableIn;
        bool subscribed;

        PrefabConditionalElement(UnityEngine.Object[] targets, PrefabKind? showIn, PrefabKind hideIn, PrefabKind? enableIn, PrefabKind disableIn)
        {
            this.targets = targets;
            this.showIn = showIn;
            this.hideIn = hideIn;
            this.enableIn = enableIn;
            this.disableIn = disableIn;
            name = "alchemy-prefab-conditional";
            pickingMode = PickingMode.Ignore;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            UpdateState();
        }

        public static void Wrap(SerializedObject serializedObject, object target, IEnumerable<Attribute> attributes, VisualElement targetElement)
        {
            PrefabKind? showIn = null;
            var hideIn = PrefabKind.None;
            PrefabKind? enableIn = null;
            var disableIn = PrefabKind.None;

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case ShowInAttribute show:
                        showIn = show.PrefabKind;
                        break;
                    case HideInAttribute hide:
                        hideIn = hide.PrefabKind;
                        break;
                    case EnableInAttribute enable:
                        enableIn = enable.PrefabKind;
                        break;
                    case DisableInAttribute disable:
                        disableIn = disable.PrefabKind;
                        break;
                }
            }

            if (!showIn.HasValue && hideIn == PrefabKind.None && !enableIn.HasValue && disableIn == PrefabKind.None) return;

            // Use the inspected roots, not a nested managed object or the value of the member.
            // Keep the owners rather than a SerializedObject that may be disposed before detachment.
            var targets = serializedObject != null ? serializedObject.targetObjects
                : target is UnityEngine.Object owner ? new[] { owner } : null;
            var wrapper = new PrefabConditionalElement(targets, showIn, hideIn, enableIn, disableIn);
            wrapper.style.width = targetElement.style.width;
            wrapper.style.flexGrow = targetElement.style.flexGrow;
            wrapper.style.flexShrink = targetElement.style.flexShrink;
            wrapper.style.alignSelf = targetElement.style.alignSelf;

            // Create the scope before other drawers run, so their decorations are included too.
            // They keep the original TargetElement and cannot override this scope's restrictions.
            var parent = targetElement.parent;
            parent.Insert(parent.IndexOf(targetElement), wrapper);
            wrapper.Add(targetElement);
        }

        void UpdateState()
        {
            var targets = this.targets;
            if (targets == null)
            {
                for (var ancestor = parent; ancestor != null; ancestor = ancestor.parent)
                {
                    if (inspectorTargets.TryGetValue(ancestor, out targets)) break;
                }
            }
            targets ??= Array.Empty<UnityEngine.Object>();
            var visible = !showIn.HasValue || targets.Length != 0;
            var enabled = !enableIn.HasValue || targets.Length != 0;

            foreach (var target in targets)
            {
                var kind = PrefabKindUtility.GetPrefabKind(target);
                visible &= (!showIn.HasValue || (showIn.Value & kind) != 0) && (hideIn & kind) == 0;
                enabled &= (!enableIn.HasValue || (enableIn.Value & kind) != 0) && (disableIn & kind) == 0;
                if (!visible && !enabled) break;
            }

            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            SetEnabled(enabled);
        }

        internal static void SetInspectorTargets(VisualElement root, SerializedObject serializedObject, object target)
        {
            var owners = serializedObject != null ? serializedObject.targetObjects
                : target is UnityEngine.Object owner ? new[] { owner } : null;
            if (owners == null) return;

            inspectorTargets.Remove(root);
            inspectorTargets.Add(root, owners);
            UpdateConditionalElements(root);
        }

        // Late-built children miss SetInspectorTargets. Targets still resolve by walking ancestors.
        internal static void UpdateConditionalElements(VisualElement root)
        {
            root.Query<PrefabConditionalElement>().ForEach(element => element.UpdateState());
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (!subscribed)
            {
                EditorApplication.hierarchyChanged += UpdateState;
                EditorApplication.projectChanged += UpdateState;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                Undo.undoRedoPerformed += UpdateState;
                PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdated;
#if UNITY_2022_2_OR_NEWER
                PrefabUtility.prefabInstanceUnpacked += OnPrefabInstanceUnpacked;
#endif
                subscribed = true;
            }

            UpdateState();
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (!subscribed) return;
            EditorApplication.hierarchyChanged -= UpdateState;
            EditorApplication.projectChanged -= UpdateState;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Undo.undoRedoPerformed -= UpdateState;
            PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdated;
#if UNITY_2022_2_OR_NEWER
            PrefabUtility.prefabInstanceUnpacked -= OnPrefabInstanceUnpacked;
#endif
            subscribed = false;
        }

        void OnPlayModeStateChanged(PlayModeStateChange state) => UpdateState();

        void OnPrefabInstanceUpdated(UnityEngine.GameObject instance) => UpdateState();

#if UNITY_2022_2_OR_NEWER
        void OnPrefabInstanceUnpacked(UnityEngine.GameObject instance, PrefabUnpackMode unpackMode) => UpdateState();
#endif
    }
}
