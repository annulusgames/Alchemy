using System.Collections;
using System.Linq;
using Alchemy.Editor.Elements;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.PlayModeInEditor
{
    public class ClassFieldTest
    {
        EditorWindow window;

        [TearDown]
        public void TearDown()
        {
            if (window == null) return;
            UnityEngine.Object.DestroyImmediate(window);
            window = null;
        }

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

        sealed class ChildObjectsOnlyTarget
        {
            [ChildObjectsOnly]
            public GameObject value;
        }

        sealed class RequiredListLengthTarget
        {
            [RequiredListLength(1)]
            public int[] value;
        }

        sealed class RequiredInTarget
        {
            [RequiredIn(PrefabKind.InstanceInScene)]
            public GameObject value;
        }

        [UnityTest]
        public IEnumerator Test_RequiredInAttributeDoesNotRequireSerializedProperty()
        {
            var target = new RequiredInTarget();
            var field = new ClassField(target, target.GetType(), "Target");

            Assert.DoesNotThrow(() => Expand(field));
            foreach (var wait in WaitUntilBuilt(field))
                yield return wait;
        }

        sealed class PrivateFieldTarget
        {
            int privateValue;
            public int publicValue;
        }

        [UnityTest]
        public IEnumerator Test_PrivateFieldsAreDrawnViaReflectionField()
        {
            var target = new PrivateFieldTarget();
            var field = new ClassField(target, target.GetType(), "Target");
            Expand(field);
            foreach (var wait in WaitUntilBuilt(field))
                yield return wait;

            Assert.That(
                field.Query<IntegerField>().ToList().Any(x => x.label == "Private Value"),
                Is.True,
                "ClassField must still draw private fields via ReflectionField.");
            Assert.That(
                field.Query<IntegerField>().ToList().Any(x => x.label == "Public Value"),
                Is.True);
        }

        [UnityTest]
        public IEnumerator Test_ConditionalAttributesDoNotRequireSerializedObject()
        {
            var target = new ConditionalTarget();
            var field = new ClassField(target, target.GetType(), "Target");
            Expand(field);
            foreach (var wait in WaitUntilBuilt(field))
                yield return wait;

            var showIfField = EditorTestUtility.QueryRequired<ReflectionField>(
                field,
                element => element.Q<IntegerField>()?.label == "Show If");
            var hideIfField = EditorTestUtility.QueryRequired<ReflectionField>(
                field,
                element => element.Q<IntegerField>()?.label == "Hide If");

            Assert.That(showIfField.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(hideIfField.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator Test_RequiredAttributeDoesNotRequireSerializedProperty()
        {
            var target = new RequiredTarget();
            var field = new ClassField(target, target.GetType(), "Target");

            Assert.DoesNotThrow(() => Expand(field));
            foreach (var wait in WaitUntilBuilt(field))
                yield return wait;
        }

        [UnityTest]
        public IEnumerator Test_ValidateInputAttributeDoesNotRequireSerializedProperty()
        {
            var target = new ValidateInputTarget();
            var field = new ClassField(target, target.GetType(), "Target");

            Assert.DoesNotThrow(() => Expand(field));
            foreach (var wait in WaitUntilBuilt(field))
                yield return wait;
        }

        [UnityTest]
        public IEnumerator Test_ChildObjectsOnlyAttributeDoesNotRequireSerializedProperty()
        {
            var target = new ChildObjectsOnlyTarget();
            var field = new ClassField(target, target.GetType(), "Target");

            Assert.DoesNotThrow(() => Expand(field));
            foreach (var wait in WaitUntilBuilt(field))
                yield return wait;
        }

        [UnityTest]
        public IEnumerator Test_RequiredListLengthAttributeDoesNotRequireSerializedProperty()
        {
            var target = new RequiredListLengthTarget();
            var field = new ClassField(target, target.GetType(), "Target");

            Assert.DoesNotThrow(() => Expand(field));
            foreach (var wait in WaitUntilBuilt(field))
                yield return wait;
        }

        // Foldout value changes are not dispatched until the element is on a panel.
        void Expand(ClassField field)
        {
            window = EditorTestUtility.ShowInWindow(field);
            field.Q<Foldout>().value = true;
        }

        static IEnumerable WaitUntilBuilt(ClassField field)
        {
            var foldout = field.Q<Foldout>();
            var deadline = EditorApplication.timeSinceStartup + 2f;
            while (foldout.contentContainer.childCount == 0)
            {
                Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline),
                    "Timed out waiting for the ClassField foldout to build.");
                yield return null;
            }
        }
    }
}
