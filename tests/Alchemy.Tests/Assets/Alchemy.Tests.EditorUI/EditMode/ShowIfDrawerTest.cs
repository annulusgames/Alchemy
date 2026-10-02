using System.Collections;
using System.Linq;
using Alchemy.Editor.Elements;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class ShowIfDrawerTest
    {
        readonly ObjectReferenceValidationTestHelper helper = new ObjectReferenceValidationTestHelper();

        [TearDown]
        public void TearDown() => helper.Dispose();

        [UnityTest]
        public IEnumerator Drawer_UpdatesWhenSerializedBoolChanges()
        {
            var host = helper.CreateHost<ShowIfDrawerHost>();
            host.show = false;
            host.nested ??= new ShowIfDrawerHost.Nested();
            host.nested.show = false;
            helper.ShowInspector(host);

            var root = Field("value");
            var nested = Field("nested.value");
            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(nested.style.display.value, Is.EqualTo(DisplayStyle.None));

            // Let TrackPropertyValue register against the current value before editing.
            // 6000.0 does not report a change applied on the tracked SerializedObject itself.
            for (var i = 0; i < 5; i++) yield return null;

            SetBool(host, nameof(ShowIfDrawerHost.show), true);
            foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => root.style.display.value == DisplayStyle.Flex))
                yield return wait;
            Assert.That(nested.style.display.value, Is.EqualTo(DisplayStyle.None));

            SetBool(host, nameof(ShowIfDrawerHost.show), false);
            foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => root.style.display.value == DisplayStyle.None))
                yield return wait;

            SetBool(host, "nested.show", true);
            foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => nested.style.display.value == DisplayStyle.Flex))
                yield return wait;
        }

        void SetBool(ShowIfDrawerHost host, string propertyPath, bool value)
        {
            var serializedObject = helper.Editor.serializedObject;
            using (var editing = new SerializedObject(host))
            {
                editing.FindProperty(propertyPath).boolValue = value;
                editing.ApplyModifiedPropertiesWithoutUndo();
            }

            serializedObject.Update();
        }

        AlchemyPropertyField Field(string bindingPath)
        {
            return helper.InspectorRoot.Query<AlchemyPropertyField>().ToList()
                .Single(element => (element.FieldElement as PropertyField)?.bindingPath == bindingPath);
        }
    }
}
