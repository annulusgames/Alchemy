using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace Alchemy.Editor
{
    internal static class AlchemyAttributeDrawerHelper
    {
        internal static void ExecutePropertyDrawers(SerializedObject serializedObject, SerializedProperty property, object target, MemberInfo memberInfo, VisualElement memberElement)
        {
            var attributes = memberInfo.GetCustomAttributes();
            var processorTypes = TypeCache.GetTypesWithAttribute(typeof(CustomAttributeDrawerAttribute)).Where(x => x.IsSubclassOfGeneric(typeof(AlchemyAttributeDrawer<>)));
            foreach (var attribute in attributes)
            {
                var processorType = processorTypes.FirstOrDefault(x => x.GetCustomAttribute<CustomAttributeDrawerAttribute>().targetAttributeType == attribute.GetType());
                if (processorType == null) continue;

                var processor = (IAlchemyAttributeDrawer)Activator.CreateInstance(processorType);
                processor.SetContext(serializedObject, property, target, memberInfo, attribute, memberElement);

                processor.OnCreateElement();
            }
        }
    }
}