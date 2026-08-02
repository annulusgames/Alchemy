using Alchemy.Editor.Elements;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.PlayModeInEditor
{
    public class ClassFieldTest
    {
        sealed class ConditionalTarget
        {
            public bool show;

            [ShowIf(nameof(show))]
            public int showIf;

            public bool hide = true;

            [HideIf(nameof(hide))]
            public int hideIf;
        }

        sealed class RequiredTarget
        {
            [Required]
            public GameObject value;
        }

        sealed class ValidateInputTarget
        {
            [ValidateInput(nameof(IsValid))]
            public int value;

            public bool IsValid(int input) => input >= 0;
        }

        [Test]
        public void Test_ConditionalAttributesDoNotRequireSerializedObject()
        {
            var target = new ConditionalTarget();
            var field = new ClassField(target, target.GetType(), "Target");

            var showIfField = EditorTestUtility.QueryRequired<ReflectionField>(
                field,
                element => element.Q<IntegerField>()?.label == "Show If");
            var hideIfField = EditorTestUtility.QueryRequired<ReflectionField>(
                field,
                element => element.Q<IntegerField>()?.label == "Hide If");

            Assert.That(showIfField.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(hideIfField.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void Test_RequiredAttributeDoesNotRequireSerializedProperty()
        {
            var target = new RequiredTarget();

            Assert.DoesNotThrow(() => new ClassField(target, target.GetType(), "Target"));
        }

        [Test]
        public void Test_ValidateInputAttributeDoesNotRequireSerializedProperty()
        {
            var target = new ValidateInputTarget();

            Assert.DoesNotThrow(() => new ClassField(target, target.GetType(), "Target"));
        }
    }
}
