using System;
using System.Linq;
using System.Reflection;
using Alchemy.Editor.Elements;
using Alchemy.Inspector;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Editor.Drawers
{
    [CustomAttributeDrawer(typeof(ReadOnlyAttribute))]
    public sealed class ReadOnlyDrawer : AlchemyAttributeDrawer
    {
        public override void OnCreateElement()
        {
            TargetElement.SetEnabled(false);
        }
    }

    [CustomAttributeDrawer(typeof(IndentAttribute))]
    public sealed class IndentDrawer : AlchemyAttributeDrawer
    {
        const float IndentPadding = 15f;

        public override void OnCreateElement()
        {
            TargetElement.RegisterCallback<GeometryChangedEvent>(x => AddPadding());
        }

        void AddPadding()
        {
            var label = TargetElement.Q<Label>();
            if (label == null) return;
            label.style.paddingLeft = ((IndentAttribute)Attribute).indent * IndentPadding;
        }
    }

    [CustomAttributeDrawer(typeof(HideInPlayModeAttribute))]
    public sealed class HideInPlayModeDrawer : AlchemyAttributeDrawer
    {
        public override void OnCreateElement()
        {
            TargetElement.style.display = Application.isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    [CustomAttributeDrawer(typeof(HideInEditModeAttribute))]
    public sealed class HideInEditModeDrawer : AlchemyAttributeDrawer
    {
        public override void OnCreateElement()
        {
            TargetElement.style.display = !Application.isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    [CustomAttributeDrawer(typeof(DisableInPlayModeAttribute))]
    public sealed class DisableInPlayModeDrawer : AlchemyAttributeDrawer
    {
        public override void OnCreateElement()
        {
            if (Application.isPlaying) TargetElement.SetEnabled(false);
        }
    }

    [CustomAttributeDrawer(typeof(DisableInEditModeAttribute))]
    public sealed class DisableInEditModeDrawer : AlchemyAttributeDrawer
    {
        public override void OnCreateElement()
        {
            if (!Application.isPlaying) TargetElement.SetEnabled(false);
        }
    }

    [CustomAttributeDrawer(typeof(HideLabelAttribute))]
    public sealed class HideLabelDrawer : AlchemyAttributeDrawer
    {
        public override void OnCreateElement()
        {
            if (TargetElement is AlchemyPropertyField field)
            {
                field.Label = string.Empty;
                return;
            }

            var labelElement = TargetElement.Q<Label>();
            if (labelElement == null) return;
            labelElement.text = string.Empty;
        }
    }

    [CustomAttributeDrawer(typeof(LabelTextAttribute))]
    public sealed class LabelTextDrawer : AlchemyAttributeDrawer
    {
        public override void OnCreateElement()
        {
            var labelTextAttribute = (LabelTextAttribute)Attribute;

            switch (TargetElement)
            {
                case AlchemyPropertyField alchemyPropertyField:
                    alchemyPropertyField.Label = labelTextAttribute.Text;
                    break;
                case MethodButton methodButton:
                    methodButton.SetLableText(labelTextAttribute.Text);
                    break;
                case Button button:
                    button.text = labelTextAttribute.Text;
                    break;
                default:
                    var labelElement = TargetElement.Q<Label>();
                    if (labelElement == null) return;
                    labelElement.text = labelTextAttribute.Text;
                    break;
            }
        }
    }

    [CustomAttributeDrawer(typeof(LabelWidthAttribute))]
    public sealed class LabelWidthDrawer : AlchemyAttributeDrawer
    {
        public override void OnCreateElement()
        {
            var width = ((LabelWidthAttribute)Attribute).Width;

            if (TargetElement is AlchemyPropertyField field && field.FieldElement is PropertyField)
            {
                GUIHelper.ScheduleSetLabelWidth(field, width);
                return;
            }

            Debug.LogWarning("The LabelWidth attribute currently only supports PropertyField and is ignored for other visual elements.");
        }
    }

    [CustomAttributeDrawer(typeof(HideIfAttribute))]
    public sealed class HideIfDrawer : TrackSerializedObjectAttributeDrawer
    {
        protected override void OnInspectorChanged()
        {
            var condition = ReflectionHelper.GetValueBool(Target, ((HideIfAttribute)Attribute).Condition);
            TargetElement.style.display = condition ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    [CustomAttributeDrawer(typeof(ShowIfAttribute))]
    public sealed class ShowIfDrawer : TrackSerializedObjectAttributeDrawer
    {
        protected override void OnInspectorChanged()
        {
            var condition = ReflectionHelper.GetValueBool(Target, ((ShowIfAttribute)Attribute).Condition);
            TargetElement.style.display = !condition ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    [CustomAttributeDrawer(typeof(DisableIfAttribute))]
    public sealed class DisableIfDrawer : TrackSerializedObjectAttributeDrawer
    {
        protected override void OnInspectorChanged()
        {
            var condition = ReflectionHelper.GetValueBool(Target, ((DisableIfAttribute)Attribute).Condition);
            TargetElement.SetEnabled(!condition);
        }
    }

    [CustomAttributeDrawer(typeof(EnableIfAttribute))]
    public sealed class EnableIfDrawer : TrackSerializedObjectAttributeDrawer
    {
        protected override void OnInspectorChanged()
        {
            var condition = ReflectionHelper.GetValueBool(Target, ((EnableIfAttribute)Attribute).Condition);
            TargetElement.SetEnabled(condition);
        }
    }

    [CustomAttributeDrawer(typeof(ChildObjectsOnlyAttribute))]
    public sealed class ChildObjectsOnlyDrawer : ObjectReferenceValidationDrawer
    {
        protected override string UnsupportedMessage =>
            "ChildObjectsOnly can only be used on GameObject, Component, or UnityEngine.Object references, including arrays and lists of those types, when the Inspector target is a Component or GameObject.";

        protected override bool IsSupportedProperty() =>
            ChildObjectsOnlyValidation.GetOwnerTransform(SerializedObject) != null &&
            ChildObjectsOnlyValidation.IsSupportedProperty(SerializedProperty);

        protected override string GetErrorMessage()
        {
            var attribute = (ChildObjectsOnlyAttribute)Attribute;
            return attribute.Message ?? ChildObjectsOnlyValidation.DefaultErrorMessage(
                SerializedProperty.displayName, attribute.IncludeSelf);
        }

        protected override bool IsPropertyValid() =>
            ChildObjectsOnlyValidation.IsSerializedPropertyValid(
                SerializedProperty, ((ChildObjectsOnlyAttribute)Attribute).IncludeSelf);
    }

    [CustomAttributeDrawer(typeof(SceneObjectsOnlyAttribute))]
    public sealed class SceneObjectsOnlyDrawer : ObjectReferenceValidationDrawer
    {
        protected override string UnsupportedMessage =>
            "SceneObjectsOnly can only be used on UnityEngine.Object references, including arrays and lists of those types.";

        protected override bool IsSupportedProperty() =>
            SceneObjectsOnlyValidation.IsSupportedProperty(SerializedProperty);

        protected override string GetErrorMessage() =>
            ((SceneObjectsOnlyAttribute)Attribute).Message ??
            SceneObjectsOnlyValidation.DefaultErrorMessage(SerializedProperty.displayName);

        protected override bool IsPropertyValid() =>
            SceneObjectsOnlyValidation.IsSerializedPropertyValid(SerializedProperty);
    }

    public abstract class ObjectReferenceValidationDrawer : TrackSerializedObjectAttributeDrawer
    {
        protected abstract string UnsupportedMessage { get; }
        protected abstract bool IsSupportedProperty();
        protected abstract string GetErrorMessage();
        protected abstract bool IsPropertyValid();

        HelpBox helpBox;
        bool subscribed;

        public override void OnCreateElement()
        {
            if (SerializedProperty == null) return;

            if (!IsSupportedProperty())
            {
                helpBox = new HelpBox(UnsupportedMessage, HelpBoxMessageType.Warning);
                InsertHelpBox();
                return;
            }

            helpBox = new HelpBox(GetErrorMessage(), HelpBoxMessageType.Error);
            InsertHelpBox();

            TargetElement.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            TargetElement.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            if (TargetElement.panel != null)
            {
                Subscribe();
            }

            TargetElement.TrackPropertyValue(SerializedProperty, _ => OnInspectorChanged());
            base.OnCreateElement();
        }

        protected override void OnInspectorChanged()
        {
            if (helpBox == null) return;
            if (!SerializedObjectReferenceValidation.TryAccessProperty(SerializedProperty, out _, out _))
            {
                return;
            }

            var valid = IsPropertyValid();
            helpBox.style.display = valid ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void InsertHelpBox()
        {
            var parent = TargetElement.parent;
            parent.Insert(parent.IndexOf(TargetElement), helpBox);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Subscribe();
            OnInspectorChanged();
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt) => Unsubscribe();

        void OnExternalChange()
        {
            if (!SerializedObjectReferenceValidation.TryAccessProperty(SerializedProperty, out var serializedObject, out _))
            {
                return;
            }

            try
            {
                if (!serializedObject.hasModifiedProperties)
                {
                    serializedObject.UpdateIfRequiredOrScript();
                }
            }
            catch (Exception)
            {
                return;
            }

            OnInspectorChanged();
        }

        void Subscribe()
        {
            if (subscribed) return;
            EditorApplication.hierarchyChanged += OnExternalChange;
            Undo.undoRedoPerformed += OnExternalChange;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed) return;
            EditorApplication.hierarchyChanged -= OnExternalChange;
            Undo.undoRedoPerformed -= OnExternalChange;
            subscribed = false;
        }
    }

    [CustomAttributeDrawer(typeof(RequiredAttribute))]
    public sealed class RequiredDrawer : TrackSerializedObjectAttributeDrawer
    {
        HelpBox helpBox;

        public override void OnCreateElement()
        {
            if (SerializedProperty == null || SerializedProperty.propertyType != SerializedPropertyType.ObjectReference) return;

            var message = ((RequiredAttribute)Attribute).Message ?? ObjectNames.NicifyVariableName(SerializedProperty.displayName) + " is required.";
            helpBox = new HelpBox(message, HelpBoxMessageType.Error);

            var parent = TargetElement.parent;
            parent.Insert(parent.IndexOf(TargetElement), helpBox);

            base.OnCreateElement();
        }

        protected override void OnInspectorChanged()
        {
            helpBox.style.display = SerializedProperty.objectReferenceValue != null ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    [CustomAttributeDrawer(typeof(RequiredListLengthAttribute))]
    public sealed class RequiredListLengthDrawer : TrackSerializedObjectAttributeDrawer
    {
        HelpBox helpBox;

        public override void OnCreateElement()
        {
            if (SerializedProperty == null) return;

            var attribute = (RequiredListLengthAttribute)Attribute;
            if (!attribute.Min.HasValue && !attribute.Max.HasValue)
            {
                helpBox = new HelpBox(
                    RequiredListLengthValidation.InvalidBoundsMessage,
                    HelpBoxMessageType.Warning);
                InsertHelpBox();
                return;
            }

            if (!RequiredListLengthValidation.IsSupportedProperty(SerializedProperty))
            {
                helpBox = new HelpBox(
                    RequiredListLengthValidation.UnsupportedMessage,
                    HelpBoxMessageType.Warning);
                InsertHelpBox();
                return;
            }

            helpBox = new HelpBox(
                attribute.Message ?? RequiredListLengthValidation.DefaultMessage(
                    SerializedProperty.displayName,
                    attribute.Min,
                    attribute.Max),
                HelpBoxMessageType.Error);
            InsertHelpBox();
            TargetElement.TrackPropertyValue(SerializedProperty, _ => OnInspectorChanged());
            base.OnCreateElement();
        }

        protected override void OnInspectorChanged()
        {
            if (helpBox == null) return;
            if (!SerializedObjectReferenceValidation.TryAccessProperty(SerializedProperty, out _, out _))
            {
                return;
            }

            var attribute = (RequiredListLengthAttribute)Attribute;
            var valid = RequiredListLengthValidation.IsSerializedPropertyValid(
                SerializedProperty,
                attribute.Min,
                attribute.Max);
            helpBox.style.display = valid ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void InsertHelpBox()
        {
            var parent = TargetElement.parent;
            parent.Insert(parent.IndexOf(TargetElement), helpBox);
        }
    }

    [CustomAttributeDrawer(typeof(RequiredInAttribute))]
    public sealed class RequiredInDrawer : TrackSerializedObjectAttributeDrawer
    {
        HelpBox helpBox;

        public override void OnCreateElement()
        {
            if (SerializedProperty == null || SerializedProperty.propertyType != SerializedPropertyType.ObjectReference) return;

            var attribute = (RequiredInAttribute)Attribute;
            var message = attribute.Message ?? ObjectNames.NicifyVariableName(SerializedProperty.displayName) + " is required.";
            helpBox = new HelpBox(message, HelpBoxMessageType.Error);

            var parent = TargetElement.parent;
            parent.Insert(parent.IndexOf(TargetElement), helpBox);

            base.OnCreateElement();
        }

        protected override void OnInspectorChanged()
        {
            var attribute = (RequiredInAttribute)Attribute;
            var valid = SerializedObjectReferenceValidation.IsSerializedPropertyValid(
                SerializedProperty,
                target =>
                {
                    var isRequired = (attribute.PrefabKind & PrefabKindUtility.GetPrefabKind(target)) != 0;
                    return value => !isRequired || value != null;
                });

            helpBox.style.display = valid ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    [CustomAttributeDrawer(typeof(ValidateInputAttribute))]
    public sealed class ValidateInputDrawer : TrackSerializedObjectAttributeDrawer
    {
        HelpBox helpBox;

        public override void OnCreateElement()
        {
            if (SerializedProperty == null) return;

            var message = ((ValidateInputAttribute)Attribute).Message ?? ObjectNames.NicifyVariableName(SerializedProperty.displayName) + " is not valid.";
            helpBox = new HelpBox(message, HelpBoxMessageType.Error);

            var parent = TargetElement.parent;
            parent.Insert(parent.IndexOf(TargetElement), helpBox);

            base.OnCreateElement();
        }

        protected override void OnInspectorChanged()
        {
            var result = ReflectionHelper.Invoke(Target, ((ValidateInputAttribute)Attribute).Condition, SerializedProperty.GetValue<object>());
            helpBox.style.display = result is bool flag && flag ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    [CustomAttributeDrawer(typeof(HelpBoxAttribute))]
    public sealed class HelpBoxDrawer : AlchemyAttributeDrawer
    {
        HelpBox helpBox;

        public override void OnCreateElement()
        {
            var att = (HelpBoxAttribute)Attribute;
            helpBox = new HelpBox(att.Message, att.MessageType);

            var parent = TargetElement.parent;
            parent.Insert(parent.IndexOf(TargetElement), helpBox);
        }
    }

    [CustomAttributeDrawer(typeof(PreviewAttribute))]
    public sealed class PreviewDrawer : TrackSerializedObjectAttributeDrawer
    {
        private Image image;
        private PreviewImageUpdater previewUpdater;
        private const float BorderWidth = 1f;
        private static readonly Color borderColor = new Color(0f, 0f, 0f, 0.3f);

        public override void OnCreateElement()
        {
            if (SerializedProperty == null || SerializedProperty.propertyType != SerializedPropertyType.ObjectReference) return;

            var att = (PreviewAttribute)Attribute;

            image = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                style = {
                    width = att.Size,
                    height = att.Size,
                    marginTop = EditorGUIUtility.standardVerticalSpacing,
                    marginBottom = EditorGUIUtility.standardVerticalSpacing * 4f,
                    alignSelf = att.AlignStyle,
                    borderTopWidth = BorderWidth,
                    borderBottomWidth = BorderWidth,
                    borderLeftWidth = BorderWidth,
                    borderRightWidth = BorderWidth,
                    borderBottomColor = borderColor,
                    borderTopColor = borderColor,
                    borderLeftColor = borderColor,
                    borderRightColor = borderColor,
                }
            };

            image.RegisterCallback<MouseDownEvent>(x =>
            {
                using var mouseDownEvent = MouseDownEvent.GetPooled(x);
                var objectFieldSelector = TargetElement.Q(className: "unity-object-field__selector");
                mouseDownEvent.target = objectFieldSelector;
                objectFieldSelector.SendEvent(mouseDownEvent);
            });

            var parent = TargetElement.parent;
            parent.Insert(parent.IndexOf(TargetElement) + 1, image);
            previewUpdater = new PreviewImageUpdater(TargetElement, image);

            base.OnCreateElement();
        }

        protected override void OnInspectorChanged()
        {
            previewUpdater?.Update(SerializedProperty.objectReferenceValue);
        }
    }

    [CustomAttributeDrawer(typeof(HorizontalLineAttribute))]
    public sealed class HorizontalLineDrawer : AlchemyAttributeDrawer
    {
        public override void OnCreateElement()
        {
            var att = (HorizontalLineAttribute)Attribute;
            var parent = TargetElement.parent;
            var lineColor = att.Color == default ? GUIHelper.LineColor : att.Color;
            var line = GUIHelper.CreateLine(lineColor, EditorGUIUtility.standardVerticalSpacing * 4f);
            parent.Insert(parent.IndexOf(TargetElement), line);
        }
    }

    [CustomAttributeDrawer(typeof(TitleAttribute))]
    public sealed class TitleDrawer : AlchemyAttributeDrawer
    {
        public override void OnCreateElement()
        {
            var att = (TitleAttribute)Attribute;
            var parent = TargetElement.parent;

            var title = new Label(att.TitleText)
            {
                style = {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    paddingLeft = 3f,
                    marginTop = 4f,
                    marginBottom = -2f
                }
            };
            parent.Insert(parent.IndexOf(TargetElement), title);

            if (att.SubtitleText != null)
            {
                var subtitle = new Label(att.SubtitleText)
                {
                    style = {
                        fontSize = 10f,
                        paddingLeft = 4.5f,
                        marginTop = 1.5f,
                        color = GUIHelper.SubtitleColor,
                        unityTextAlign = TextAnchor.MiddleLeft
                    }
                };
                parent.Insert(parent.IndexOf(TargetElement), subtitle);
            }

            var line = GUIHelper.CreateLine(GUIHelper.LineColor, EditorGUIUtility.standardVerticalSpacing * 3f);
            parent.Insert(parent.IndexOf(TargetElement), line);
        }
    }

    [CustomAttributeDrawer(typeof(BlockquoteAttribute))]
    public sealed class BlockquoteDrawer : AlchemyAttributeDrawer
    {
        const float BarWidth = 3f;
        const float TextInset = 7f;

        public override void OnCreateElement()
        {
            var att = (BlockquoteAttribute)Attribute;
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            var textColor = GUIHelper.TextColor;
            var backgroundColor = textColor;
            backgroundColor.a = 0.06f;
            // Match EditorGUILayout.GetControlRect: layerMaskField margins inset the block.
            var margin = EditorStyles.layerMaskField.margin;

            // The border is the bar, so the remaining padding keeps the text 7px from the block's left edge.
            var blockquote = new VisualElement
            {
                style = {
                    backgroundColor = backgroundColor,
                    borderLeftWidth = BarWidth,
                    borderLeftColor = textColor,
                    paddingTop = spacing,
                    paddingBottom = spacing,
                    paddingLeft = TextInset - BarWidth,
                    marginTop = margin.top,
                    marginBottom = margin.bottom,
                    marginLeft = margin.left,
                    marginRight = margin.right
                }
            };
            blockquote.Add(new Label(att.Text)
            {
                style = {
                    whiteSpace = WhiteSpace.Normal
                }
            });

            var parent = TargetElement.parent;
            parent.Insert(parent.IndexOf(TargetElement), blockquote);
        }
    }

    [CustomAttributeDrawer(typeof(OnValueChangedAttribute))]
    public sealed class OnValueChangedDrawer : AlchemyAttributeDrawer
    {
        public override void OnCreateElement()
        {
            var property = SerializedProperty;
            var callbacks = property == null
                ? null
                : FindCallbacks(Target.GetType(), ((OnValueChangedAttribute)Attribute).MethodName, property.GetPropertyType());
            TargetElement.TrackPropertyValue(property, changed =>
            {
                InvokeCallbacks(Target, callbacks, () => changed.GetValue<object>());
            });
        }

        // Methods named methodName that take no arguments or one argument accepting valueType.
        internal static MethodInfo[] FindCallbacks(Type targetType, string methodName, Type valueType)
        {
            return ReflectionHelper.GetAllMethodsIncludingBaseNonPublic(targetType)
                .Where(x => x.Name == methodName && !x.ContainsGenericParameters)
                .Where(x =>
                {
                    var parameters = x.GetParameters();
                    return parameters.Length == 0 || (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(valueType));
                })
                .ToArray();
        }

        internal static void InvokeCallbacks(object target, MethodInfo[] callbacks, Func<object> getValue)
        {
            object[] arguments = null;
            foreach (var methodInfo in callbacks)
            {
                if (methodInfo.GetParameters().Length == 0)
                {
                    methodInfo.Invoke(methodInfo.IsStatic ? null : target, null);
                    continue;
                }
                arguments ??= new[] { getValue() };
                methodInfo.Invoke(methodInfo.IsStatic ? null : target, arguments);
            }
        }
    }
}
