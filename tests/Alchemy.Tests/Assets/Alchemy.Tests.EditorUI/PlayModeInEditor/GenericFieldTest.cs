using System.Collections;
using Alchemy.Editor.Elements;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.PlayModeInEditor
{
    public class GenericFieldTest
    {
        [UnityTest]
        public IEnumerator Test_CharInputCanBeCleared()
        {
            var field = new GenericField('x', typeof(char), "Character");
            var changedValue = 'x';
            field.OnValueChanged += value => changedValue = (char)value;

            var window = EditorTestUtility.ShowInWindow(field);
            try
            {
                yield return null;

                var textField = EditorTestUtility.QueryRequired<TextField>(field);
                Assert.DoesNotThrow(() => textField.value = string.Empty);
                Assert.That(changedValue, Is.EqualTo('x'));

                textField.value = "y";
                Assert.That(changedValue, Is.EqualTo('y'));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }
    }
}
