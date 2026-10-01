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

            buttonContainer = new Button(() => ShowTypeDropdown(property));
            buttonContainer.RemoveFromClassList(Button.ussClassName);
            buttonContainer.AddToClassList(ObjectField.objectUssClassName);
            buttonContainer.AddToClassList(ObjectFieldDisplayClassName);
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.alignItems = Align.Center;
            buttonContainer.style.overflow = Overflow.Hidden;
            buttonContainer.style.minWidth = 0f;
            buttonContainer.style.marginLeft = 0f;
            buttonContainer.style.marginRight = 0f;
            buttonContainer.style.marginTop = 0f;
            buttonContainer.style.marginBottom = 0f;

            var typeIcon = new Image
            {
                image = EditorIcons.CsScriptIcon.image,
                scaleMode = ScaleMode.ScaleAndCrop,
                pickingMode = PickingMode.Ignore
            };
            typeIcon.AddToClassList(ObjectFieldDisplayClassName + "__icon");
            typeIcon.style.width = 16f;
            typeIcon.style.height = 16f;
            typeIcon.style.flexShrink = 0f;
            typeIcon.style.marginLeft = 2f;
            typeIcon.style.marginRight = 2f;
            typeLabel = new Label { pickingMode = PickingMode.Ignore };
            typeLabel.AddToClassList(ObjectFieldDisplayClassName + "__label");
            typeLabel.style.flexGrow = 1f;
            typeLabel.style.flexShrink = 1f;
            typeLabel.style.minWidth = 0f;
            typeLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            typeLabel.style.whiteSpace = WhiteSpace.NoWrap;
            buttonContainer.Add(typeIcon);
            buttonContainer.Add(typeLabel);

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
            buttonContainer.style.height = EditorGUIUtility.singleLineHeight;
            Add(buttonContainer);

            this.TrackPropertyValue(property, UpdateTypeLabel);
            Rebuild(property);
        }

        const string ObjectFieldDisplayClassName = "unity-object-field-display";

        public readonly Foldout foldout;
        public readonly Button buttonContainer;
        readonly Label typeLabel;
        Type displayedReferenceType;
        bool hasDisplayedReferenceType;

        void ShowTypeDropdown(SerializedProperty property)
        {
            Type baseType;
            try
            {
                if (property == null || property.serializedObject == null) return;
                baseType = property.GetManagedReferenceFieldType();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            catch (NullReferenceException)
            {
                // Unity 6: "SerializedObject of SerializedProperty has been Disposed."
                return;
            }

            const int MaxTypePopupLineCount = 13;

            var dropdown = new SerializeReferenceDropdown(MaxTypePopupLineCount, new AdvancedDropdownState());
            dropdown.SetSortedTypes(SerializeReferenceDropdown.GetCandidateTypes(baseType));

            dropdown.onItemSelected += item =>
            {
                property.SetManagedReferenceType(item.type);
                property.isExpanded = true;
                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();

                UpdateTypeLabel(property);
                Rebuild(property);
            };

            // Panel space matches the GUI rect AdvancedDropdown.Show expects from this window.
            dropdown.Show(buttonContainer.worldBound);
        }

        void UpdateTypeLabel(SerializedProperty property)
        {
            if (property == null) return;

            try
            {
                // Disposed properties throw when read.
                if (property.serializedObject == null) return;

                var value = property.managedReferenceValue;
                var valueType = value == null ? null : value.GetType();
                // Nested field edits also notify the tracker. The label only names the assigned type.
                if (hasDisplayedReferenceType && valueType == displayedReferenceType) return;

                var text = (valueType == null ? "Null" : valueType.Name) +
                    $" ({property.GetManagedReferenceFieldTypeName()})";
                hasDisplayedReferenceType = true;
                displayedReferenceType = valueType;
                typeLabel.text = text;
            }
            catch (InvalidOperationException)
            {
            }
            catch (NullReferenceException)
            {
                // Unity 6: "SerializedObject of SerializedProperty has been Disposed."
            }
        }

        /// <summary>
        /// Rebuild child elements
        /// </summary>
        void Rebuild(SerializedProperty property)
        {
            UpdateTypeLabel(property);
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
