using System;
using Alchemy.Inspector;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Alchemy.Editor.Elements
{
    /// <summary>
    /// Visual Element that draws the ObjectField of the InlineEditor attribute
    /// </summary>
    public sealed class InlineEditorObjectField : BindableElement
    {
        public InlineEditorObjectField(SerializedProperty property, Type type)
        {
            Assert.IsTrue(property.propertyType == SerializedPropertyType.ObjectReference);

            style.minHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            foldout = new Foldout()
            {
                text = ObjectNames.NicifyVariableName(property.displayName)
            };
            var toggle = foldout.Q<Toggle>();
            var clickable = InternalAPIHelper.GetClickable(toggle);
            InternalAPIHelper.SetAcceptClicksIfDisabled(clickable, true);

            foldout.BindProperty(property);

            field = new ObjectField()
            {
                label = ObjectNames.NicifyVariableName(property.displayName),
                objectType = type,
                allowSceneObjects = !property.GetFieldInfo().HasCustomAttribute<AssetsOnlyAttribute>(),
                value = property.objectReferenceValue
            };
            field.style.position = Position.Absolute;
            field.style.width = Length.Percent(100f);
            GUIHelper.ScheduleAdjustLabelWidth(field);

            OnPropertyChanged(property);
            field.RegisterValueChangedCallback(x =>
            {
                property.objectReferenceValue = x.newValue;
                property.serializedObject.ApplyModifiedProperties();
                OnPropertyChanged(property);
            });

            Add(foldout);
            Add(field);
        }

        readonly Foldout foldout;
        readonly ObjectField field;
        bool isNull;

        public bool IsObjectNull => isNull;

        public string Label
        {
            get
            {
                if (isNull) return field.Q<Label>().text;
                else return foldout.text;
            }
            set
            {
                if (isNull) field.Q<Label>().text = value;
                else foldout.text = value;
            }
        }

        void OnPropertyChanged(SerializedProperty property)
        {
            isNull = property.objectReferenceValue == null;

            field.Q<Label>().text = isNull ? ObjectNames.NicifyVariableName(property.displayName) : string.Empty;
            field.pickingMode = PickingMode.Ignore;

            var objectField = field.Q<ObjectField>();
            objectField.pickingMode = PickingMode.Ignore;

            var label = objectField.Q<Label>();
            label.pickingMode = PickingMode.Ignore;

            Build(property);
        }

        void Build(SerializedProperty property)
        {
            foldout.Clear();
            var toggle = foldout.Q<Toggle>();

            isNull = property.objectReferenceValue == null;
            toggle.style.display = isNull ? DisplayStyle.None : DisplayStyle.Flex;
            if (!isNull)
            {
                foldout.Add(new VisualElement() { style = { height = EditorGUIUtility.standardVerticalSpacing } });
                var so = new SerializedObject(property.objectReferenceValue);
                InspectorHelper.BuildElements(so, foldout, so.targetObject, name => so.FindProperty(name));
                this.Bind(so);
            }
            else
            {
                this.Unbind();
            }
        }
    }
}
