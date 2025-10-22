using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Alchemy.Inspector;
using Alchemy.Editor.Elements;

namespace Alchemy.Editor.Drawers
{
    [CustomAttributeDrawer(typeof(ReadOnlyAttribute))]
    public sealed class ReadOnlyDrawer : AlchemyAttributeDrawer<ReadOnlyAttribute>
    {
        public override void OnCreateElement()
        {
            TargetElement.SetEnabled(false);
        }
    }

    [CustomAttributeDrawer(typeof(IndentAttribute))]
    public sealed class IndentDrawer : AlchemyAttributeDrawer<IndentAttribute>
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
    public sealed class HideInPlayModeDrawer : AlchemyAttributeDrawer<HideInPlayModeAttribute>
    {
        public override void OnCreateElement()
        {
            TargetElement.style.display = Application.isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    [CustomAttributeDrawer(typeof(HideInEditModeAttribute))]
    public sealed class HideInEditModeDrawer : AlchemyAttributeDrawer<HideInEditModeAttribute>
    {
        public override void OnCreateElement()
        {
            TargetElement.style.display = !Application.isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    [CustomAttributeDrawer(typeof(DisableInPlayModeAttribute))]
    public sealed class DisableInPlayModeDrawer : AlchemyAttributeDrawer<DisableInEditModeAttribute>
    {
        public override void OnCreateElement()
        {
            if (Application.isPlaying) TargetElement.SetEnabled(false);
        }
    }

    [CustomAttributeDrawer(typeof(DisableInEditModeAttribute))]
    public sealed class DisableInEditModeDrawer : AlchemyAttributeDrawer<DisableInEditModeAttribute>
    {
        public override void OnCreateElement()
        {
            if (!Application.isPlaying) TargetElement.SetEnabled(false);
        }
    }

    [CustomAttributeDrawer(typeof(HideLabelAttribute))]
    public sealed class HideLabelDrawer : AlchemyAttributeDrawer<HideLabelAttribute>
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
    public sealed class LabelTextDrawer : AlchemyAttributeDrawer<LabelTextAttribute>
    {
        public override void OnCreateElement()
        {
            var labelTextAttribute = Attribute;

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
                    labelElement.text = labelElement.text;
                    break;
            }
        }
    }

    [CustomAttributeDrawer(typeof(LabelWidthAttribute))]
    public sealed class LabelWidthDrawer : AlchemyAttributeDrawer<LabelWidthAttribute>
    {
        public override void OnCreateElement()
        {
            var width = Attribute.Width;

            if (TargetElement is AlchemyPropertyField field && field.FieldElement is PropertyField)
            {
                var executed = false;
                field.schedule.Execute(() =>
                {
                    var label = field.Q<Label>();
                    if (label == null) return;
                    GUIHelper.SetMinAndCurrentWidth(label, width);
                    executed = true;
                }).Until(() => executed);

                return;
            }

            Debug.LogWarning("The LabelWidth attribute currently only supports PropertyField and is ignored for other visual elements.");
        }
    }

    [CustomAttributeDrawer(typeof(HideIfAttribute))]
    public sealed class HideIfDrawer : TrackSerializedObjectAttributeDrawer<HideIfAttribute>
    {
        protected override void OnInspectorChanged()
        {
            var condition = ReflectionHelper.GetValueBool(Target, Attribute.Condition);
            TargetElement.style.display = condition ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    [CustomAttributeDrawer(typeof(ShowIfAttribute))]
    public sealed class ShowIfDrawer : TrackSerializedObjectAttributeDrawer<ShowIfAttribute>
    {
        protected override void OnInspectorChanged()
        {
            var condition = ReflectionHelper.GetValueBool(Target, Attribute.Condition);
            TargetElement.style.display = !condition ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    [CustomAttributeDrawer(typeof(DisableIfAttribute))]
    public sealed class DisableIfDrawer : TrackSerializedObjectAttributeDrawer<DisableIfAttribute>
    {
        protected override void OnInspectorChanged()
        {
            var condition = ReflectionHelper.GetValueBool(Target, Attribute.Condition);
            TargetElement.SetEnabled(!condition);
        }
    }

    [CustomAttributeDrawer(typeof(EnableIfAttribute))]
    public sealed class EnableIfDrawer : TrackSerializedObjectAttributeDrawer<EnableIfAttribute>
    {
        protected override void OnInspectorChanged()
        {
            var condition = ReflectionHelper.GetValueBool(Target, Attribute.Condition);
            TargetElement.SetEnabled(condition);
        }
    }

    [CustomAttributeDrawer(typeof(RequiredAttribute))]
    public sealed class RequiredDrawer : TrackSerializedObjectAttributeDrawer<RequiredAttribute>
    {
        HelpBox helpBox;

        public override void OnCreateElement()
        {
            if (SerializedProperty.propertyType != SerializedPropertyType.ObjectReference) return;

            var message = Attribute.Message ?? ObjectNames.NicifyVariableName(SerializedProperty.displayName) + " is required.";
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

    [CustomAttributeDrawer(typeof(ValidateInputAttribute))]
    public sealed class ValidateInputDrawer : TrackSerializedObjectAttributeDrawer<ValidateInputAttribute>
    {
        HelpBox helpBox;

        public override void OnCreateElement()
        {
            var message = Attribute.Message ?? ObjectNames.NicifyVariableName(SerializedProperty.displayName) + " is not valid.";
            helpBox = new HelpBox(message, HelpBoxMessageType.Error);

            var parent = TargetElement.parent;
            parent.Insert(parent.IndexOf(TargetElement), helpBox);

            base.OnCreateElement();
        }

        protected override void OnInspectorChanged()
        {
            var result = ReflectionHelper.Invoke(Target, Attribute.Condition, SerializedProperty.GetValue<object>());
            helpBox.style.display = result is bool flag && flag ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    [CustomAttributeDrawer(typeof(HelpBoxAttribute))]
    public sealed class HelpBoxDrawer : AlchemyAttributeDrawer<HelpBoxAttribute>
    {
        HelpBox helpBox;

        public override void OnCreateElement()
        {
            var att = Attribute;
            helpBox = new HelpBox(att.Message, att.MessageType);

            var parent = TargetElement.parent;
            parent.Insert(parent.IndexOf(TargetElement), helpBox);
        }
    }

    [CustomAttributeDrawer(typeof(PreviewAttribute))]
    public sealed class PreviewDrawer : TrackSerializedObjectAttributeDrawer<PreviewAttribute>
    {
        private Image image;
        private const float BorderWidth = 1f;
        private static readonly Color borderColor = new Color(0f, 0f, 0f, 0.3f);

        public override void OnCreateElement()
        {
            if (SerializedProperty == null || SerializedProperty.propertyType != SerializedPropertyType.ObjectReference) return;

            var att = Attribute;

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

            base.OnCreateElement();
        }

        protected override void OnInspectorChanged()
        {
            if (SerializedProperty.objectReferenceValue == null)
            {
                image.image = null;
                return;
            }

            TargetElement.schedule.Execute(() =>
            {
                var texture = AssetPreview.GetAssetPreview(SerializedProperty.objectReferenceValue);
                image.image = texture;
            }).Until(() => image.image != null);
        }
    }

    [CustomAttributeDrawer(typeof(HorizontalLineAttribute))]
    public sealed class HorizontalLineDrawer : AlchemyAttributeDrawer<HorizontalLineAttribute>
    {
        public override void OnCreateElement()
        {
            var att = Attribute;
            var parent = TargetElement.parent;
            var lineColor = att.Color == default ? GUIHelper.LineColor : att.Color;
            var line = GUIHelper.CreateLine(lineColor, EditorGUIUtility.standardVerticalSpacing * 4f);
            parent.Insert(parent.IndexOf(TargetElement), line);
        }
    }

    [CustomAttributeDrawer(typeof(TitleAttribute))]
    public sealed class TitleDrawer : AlchemyAttributeDrawer<TitleAttribute>
    {
        public override void OnCreateElement()
        {
            var att = Attribute;
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
    public sealed class BlockquoteDrawer : AlchemyAttributeDrawer<BlockquoteAttribute>
    {
        public BlockquoteDrawer()
        {
            textStyle = EditorStyles.label;
            textStyle.wordWrap = true;
        }

        readonly GUIStyle textStyle;

        public override void OnCreateElement()
        {
            var att = Attribute;
            var blockquote = new IMGUIContainer(() =>
            {
                var width = EditorGUIUtility.currentViewWidth;
                var labelContent = new GUIContent(att.Text);
                var labelHeight = textStyle.CalcHeight(labelContent, width - 3f);
                var position = EditorGUILayout.GetControlRect(false, labelHeight + EditorGUIUtility.standardVerticalSpacing * 2f);

                var blockRect = position;
                var backgroundColor = GUIHelper.TextColor;
                backgroundColor.a = 0.06f;
                EditorGUI.DrawRect(blockRect, backgroundColor);
                blockRect.x = position.xMin;
                blockRect.width = 3;
                EditorGUI.DrawRect(blockRect, GUIHelper.TextColor);

                var labelPosition = position;
                labelPosition.xMin += 7f;
                EditorGUI.LabelField(labelPosition, labelContent, textStyle);
            });

            var parent = TargetElement.parent;
            parent.Insert(parent.IndexOf(TargetElement), blockquote);
        }
    }

    [CustomAttributeDrawer(typeof(OnValueChangedAttribute))]
    public sealed class OnValueChangedDrawer : AlchemyAttributeDrawer<OnValueChangedAttribute>
    {
        public override void OnCreateElement()
        {
            TargetElement.TrackPropertyValue(SerializedProperty, property =>
            {
                var methodName = Attribute.MethodName;

                var methods = ReflectionHelper.GetAllMethodsIncludingBaseNonPublic(Target.GetType())
                    .Where(x => x.Name == methodName);

                foreach (var methodInfo in methods)
                {
                    if (methodInfo.Name != methodName) continue;

                    var parameters = methodInfo.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(property.GetPropertyType()))
                    {
                        methodInfo.Invoke(Target, new object[] { property.GetValue<object>() });
                    }
                    else if (parameters.Length == 0)
                    {
                        methodInfo.Invoke(Target, null);
                    }
                }
            });
        }
    }
}