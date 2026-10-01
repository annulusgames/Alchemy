using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Alchemy.Editor.Elements
{
    /// <summary>
    /// Draw properties marked with SerializeReference attribute
    /// </summary>
    public sealed class SerializeReferenceField : VisualElement
    {
        public SerializeReferenceField(SerializedProperty property)
        {
            Assert.IsTrue(property.propertyType == SerializedPropertyType.ManagedReference);

            style.flexDirection = FlexDirection.Row;
            style.minHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            foldout = new Foldout()
            {
                text = ObjectNames.NicifyVariableName(property.displayName)
            };
            foldout.style.flexGrow = 1f;
            foldout.BindProperty(property);
            Add(foldout);

            buttonContainer = new IMGUIContainer(() =>
            {
                var position = EditorGUILayout.GetControlRect();

                var dropdownRect = position;
                dropdownRect.height = EditorGUIUtility.singleLineHeight;

                var buttonLabel = EditorIcons.CsScriptIcon;

                try
                {
                    if (property != null)
                    {
                        // Disposed properties throw when read. Skip the label until the field is gone.
                        if (property.serializedObject == null) return;

                        buttonLabel.text = (property.managedReferenceValue == null ? "Null" : property.managedReferenceValue.GetType().Name) +
                            $" ({property.GetManagedReferenceFieldTypeName()})";
                    }
                }
                catch (InvalidOperationException)
                {
                    // Ignoring exceptions when disposed (bad solution)
                    return;
                }
                catch (NullReferenceException)
                {
                    // Unity 6: "SerializedObject of SerializedProperty has been Disposed."
                    return;
                }

                if (GUI.Button(dropdownRect, buttonLabel, EditorStyles.objectField))
                {
                    const int MaxTypePopupLineCount = 13;

                    var baseType = property.GetManagedReferenceFieldType();
                    SerializeReferenceDropdown dropdown = new(MaxTypePopupLineCount, new AdvancedDropdownState());
                    dropdown.SetSortedTypes(SerializeReferenceDropdown.GetCandidateTypes(baseType));

                    dropdown.onItemSelected += item =>
                    {
                        property.SetManagedReferenceType(item.type);
                        property.isExpanded = true;
                        property.serializedObject.ApplyModifiedProperties();
                        property.serializedObject.Update();

                        Rebuild(property);
                    };

                    dropdown.Show(position);
                }
            });

            EventCallback<GeometryChangedEvent> onGeometryChanged = null;
            VisualElement registeredTree = null;

            void AdjustWidth(VisualElement visualTree)
            {
                buttonContainer.style.width = GUIHelper.CalculateFieldWidth(buttonContainer, visualTree) -
                    (buttonContainer.GetFirstAncestorOfType<Foldout>() != null ? 18f : 0f);
            }

            void UnregisterWidth()
            {
                if (registeredTree != null && onGeometryChanged != null)
                {
                    registeredTree.UnregisterCallback(onGeometryChanged);
                }

                registeredTree = null;
                onGeometryChanged = null;
            }

            void RegisterWidth(IPanel targetPanel)
            {
                var visualTree = targetPanel?.visualTree;
                if (visualTree == null) return;
                if (registeredTree == visualTree) return;

                UnregisterWidth();
                registeredTree = visualTree;
                onGeometryChanged = _ => AdjustWidth(visualTree);
                visualTree.RegisterCallback(onGeometryChanged);
                AdjustWidth(visualTree);
            }

            RegisterCallback<AttachToPanelEvent>(evt => RegisterWidth(evt.destinationPanel));
            RegisterCallback<DetachFromPanelEvent>(_ => UnregisterWidth());

            if (panel != null)
            {
                RegisterWidth(panel);
            }

            buttonContainer.style.position = Position.Absolute;
            buttonContainer.style.top = EditorGUIUtility.standardVerticalSpacing * 0.5f;
            buttonContainer.style.right = 0f;
            Add(buttonContainer);

            Rebuild(property);
        }

        public readonly Foldout foldout;
        public readonly IMGUIContainer buttonContainer;

        /// <summary>
        /// Rebuild child elements
        /// </summary>
        void Rebuild(SerializedProperty property)
        {
            foldout.Clear();

            if (property.managedReferenceValue == null)
            {
                var helpbox = new HelpBox("No type assigned.", HelpBoxMessageType.Info);
                foldout.Add(helpbox);
            }
            else
            {
                InspectorHelper.BuildElements(property.serializedObject, foldout, property.managedReferenceValue, x => property.FindPropertyRelative(x));
            }

            this.Bind(property.serializedObject);
        }
    }
}
