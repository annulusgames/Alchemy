using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace Alchemy.Editor.Elements
{
    public sealed class MethodButton : VisualElement
    {
        const string ButtonLabelText = "Invoke";

        public MethodButton(object target, MethodInfo methodInfo, bool useParameters)
        {
            var parameters = methodInfo.GetParameters();
            var parameterObjects = new object[parameters.Length];
            for(int i = 0; i < parameters.Length; i++)
            {
                if(parameters[i].HasDefaultValue)
                    parameterObjects[i] = parameters[i].DefaultValue;
                else
                    parameterObjects[i] = TypeHelper.CreateDefaultInstance(parameters[i].ParameterType);
            }

            // Create parameterless button
            if (!useParameters || parameters.Length == 0)
            {
                if(parameters.Length > 0)
                    button = new Button(() => methodInfo.Invoke(target, parameterObjects));
                else
                    button = new Button(() => methodInfo.Invoke(target, null));

                button.text = ObjectNames.NicifyVariableName(methodInfo.Name);
                button.tooltip = methodInfo.ToString();
                Add(button);
                return;
            }


            var box = new HelpBox();
            Add(box);

            foldout = new Foldout()
            {
                text = methodInfo.Name,
                value = false,
                style = {
                    flexGrow = 1f
                }
            };
            InternalAPIHelper.SetAcceptClicksIfDisabled(
                InternalAPIHelper.GetClickable(foldout.Q<Toggle>()), true
            );

            button = new Button(() => methodInfo.Invoke(target, parameterObjects))
            {
                text = ButtonLabelText,
                style = {
                    position = Position.Absolute,
                    right = 1f,
                    top = 1.5f,
                    width = 100f
                }
            };

            box.Add(new VisualElement() { style = { width = 12f } });
            box.Add(foldout);
            box.Add(button);

            for (int i = 0; i < parameters.Length; i++)
            {
                var index = i;
                var parameter = parameters[index];
                var element = new GenericField(parameterObjects[index], parameter.ParameterType, ObjectNames.NicifyVariableName(parameter.Name));
                element.OnValueChanged += x => parameterObjects[index] = x;
                element.style.paddingRight = 4f;
                foldout.Add(element);
            }
        }

        readonly Foldout foldout;
        readonly Button button;
    }
}