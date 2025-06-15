using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Editor.Elements
{
    public sealed class MethodButton : VisualElement
    {
        private readonly StyleSheet _styleSheet = Resources.Load<StyleSheet>("Elements/MethodButton-Styles");
        
        const string ButtonLabelText = "Invoke";

        public MethodButton(object target, MethodInfo methodInfo)
        {
            styleSheets.Add(_styleSheet);

            AddToClassList("method-button");
            
            var parameters = methodInfo.GetParameters();

            // Create parameterless button
            if (parameters.Length == 0)
            {
                button = new Button(() => methodInfo.Invoke(target, null)) { text = methodInfo.Name };
                button.AddToClassList("method-button__button");
                Add(button);
                return;
            }

            var parameterObjects = new object[parameters.Length];

            var box = new HelpBox();
            box.AddToClassList("method-button__help-box");
            
            Add(box);

            foldout = new Foldout
            {
                text = methodInfo.Name,
                value = false
            };
            foldout.AddToClassList("method-button__foldout");
            
            InternalAPIHelper.SetAcceptClicksIfDisabled(
                InternalAPIHelper.GetClickable(foldout.Q<Toggle>()), true
            );

            button = new Button(() => methodInfo.Invoke(target, parameterObjects)) { text = ButtonLabelText };
            button.AddToClassList("method-button__parameter-button");
            
            box.Add(foldout);
            box.Add(button);

            for (int i = 0; i < parameters.Length; i++)
            {
                var index = i;
                var parameter = parameters[index];
                parameterObjects[index] = TypeHelper.CreateDefaultInstance(parameter.ParameterType);
                var element = new GenericField(parameterObjects[index], parameter.ParameterType, ObjectNames.NicifyVariableName(parameter.Name));
                element.OnValueChanged += x => parameterObjects[index] = x;
                foldout.Add(element);
            }
        }

        readonly Foldout foldout;
        readonly Button button;

        public void SetLableText(string labelText) {
            var foldout = this.Q<Foldout>();
            if (foldout != null) {
                foldout.text = labelText;
            }
            else {
                this.Q<Button>().text = labelText;
            }
        }
    }
}