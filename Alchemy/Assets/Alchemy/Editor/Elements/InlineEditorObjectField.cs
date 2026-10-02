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
            boundProperty = property;

            style.minHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            foldout = new Foldout()
            {
                text = ObjectNames.NicifyVariableName(property.displayName),
                value = property.isExpanded
            };
            var toggle = foldout.Q<Toggle>();
            var clickable = InternalAPIHelper.GetClickable(toggle);
            InternalAPIHelper.SetAcceptClicksIfDisabled(clickable, true);

            foldout.BindProperty(property);

            inspectorContainer = new VisualElement();

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

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

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

        readonly SerializedProperty boundProperty;
        readonly Foldout foldout;
        readonly VisualElement inspectorContainer;
        readonly ObjectField field;
        SerializedObject inlineSerializedObject;
        bool isNull;
        bool buildWhenExpanded;

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

        void OnAttachToPanel(AttachToPanelEvent _)
        {
            if (inlineSerializedObject != null) return;
            Build(boundProperty);
        }

        // DetachFromPanelEvent reaches this element before its children, so drop the
        // inline inspector here. The next attach builds a new one.
        void OnDetachFromPanel(DetachFromPanelEvent _) => DisposeInlineSerializedObject();

        void Build(SerializedProperty property)
        {
            var reference = property.objectReferenceValue;
            if (reference != null && inlineSerializedObject != null && inlineSerializedObject.targetObject == reference)
                return;

            DisposeInlineSerializedObject();

            var toggle = foldout.Q<Toggle>();

            isNull = reference == null;
            toggle.style.display = isNull ? DisplayStyle.None : DisplayStyle.Flex;
            if (isNull) return;

            if (!property.isExpanded && !foldout.value)
            {
                if (buildWhenExpanded) return;
                buildWhenExpanded = true;
                InspectorHelper.BuildFoldoutContents(foldout, false, () =>
                {
                    buildWhenExpanded = false;
                    Build(boundProperty);
                });
                return;
            }

            foldout.Add(new VisualElement() { style = { height = EditorGUIUtility.standardVerticalSpacing } });
            var so = new SerializedObject(reference);
            inlineSerializedObject = so;
            InspectorHelper.BuildElements(so, inspectorContainer, so.targetObject, name => so.FindProperty(name));
            inspectorContainer.Bind(so);
            foldout.Add(inspectorContainer);
        }

        void DisposeInlineSerializedObject()
        {
            if (inlineSerializedObject == null) return;

            // Unbind and remove children before Dispose. Parent detach is delivered
            // first; child detach callbacks must still see a live SerializedObject.
            inspectorContainer.Unbind();
            inspectorContainer.Clear();
            foldout.Clear();
            inlineSerializedObject.Dispose();
            inlineSerializedObject = null;
        }
    }
}
