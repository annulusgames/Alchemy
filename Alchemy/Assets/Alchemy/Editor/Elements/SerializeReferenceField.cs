using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.IMGUI.Controls;
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
                        buttonLabel.text = (property.managedReferenceValue == null ? "Null" : property.managedReferenceValue.GetType().Name) +
                            $" ({property.GetManagedReferenceFieldTypeName()})";
                    }
                }
                catch (InvalidOperationException)
                {
                    // Ignoring exceptions when disposed (bad solution)
                    return;
                }

                if (GUI.Button(dropdownRect, buttonLabel, EditorStyles.objectField))
                {
                    const int MaxTypePopupLineCount = 13;

                    var baseType = property.GetManagedReferenceFieldType();
                    SerializeReferenceDropdown dropdown = new(
                        TypeCache.GetTypesDerivedFrom(baseType).Append(baseType).Where(t =>
                            (t.IsPublic || t.IsNestedPublic) &&
                            !t.IsAbstract &&
                            !t.IsGenericType &&
                            !typeof(UnityEngine.Object).IsAssignableFrom(t) &&
                            t.IsSerializable
                        ),
                        MaxTypePopupLineCount,
                        new AdvancedDropdownState()
                    );

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

            schedule.Execute(() =>
            {
                var visualTree = panel.visualTree;
                visualTree.RegisterCallback<GeometryChangedEvent>(x =>
                {
                    buttonContainer.style.width = GUIHelper.CalculateFieldWidth(buttonContainer, visualTree) -
                        (buttonContainer.GetFirstAncestorOfType<Foldout>() != null ? 18f : 0f);
                });
                buttonContainer.style.width = GUIHelper.CalculateFieldWidth(buttonContainer, visualTree) -
                    (buttonContainer.GetFirstAncestorOfType<Foldout>() != null ? 18f : 0f);
            });

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
                InspectorHelper.BuildElements(property.serializedObject, foldout, new IdentityAccessor(property.managedReferenceValue), x => property.FindPropertyRelative(x));
            }

            this.Bind(property.serializedObject);
        }
    }
}