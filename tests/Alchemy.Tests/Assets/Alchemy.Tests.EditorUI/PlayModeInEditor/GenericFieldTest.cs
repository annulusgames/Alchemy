using System.Collections;
using System.Globalization;
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

        [UnityTest]
        public IEnumerator Test_DecimalInvalidInputRestoresCommittedValueOnFocusOut()
        {
            var field = new GenericField(1m, typeof(decimal), "Decimal");
            var changedValue = 1m;
            field.OnValueChanged += value => changedValue = (decimal)value;

            var window = EditorTestUtility.ShowInWindow(field);
            try
            {
                yield return null;

                var textField = EditorTestUtility.QueryRequired<TextField>(field);
                textField.Focus();
                yield return null;

                textField.value = "2";
                Assert.That(changedValue, Is.EqualTo(2m));
                Assert.That(textField.value, Is.EqualTo("2"));

                textField.value = "2x";
                Assert.That(changedValue, Is.EqualTo(2m));
                Assert.That(textField.value, Is.EqualTo("2x"));

                textField.Blur();
                yield return null;

                Assert.That(changedValue, Is.EqualTo(2m));
                Assert.That(
                    textField.value,
                    Is.EqualTo(2m.ToString(CultureInfo.InvariantCulture)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }
    }
}
