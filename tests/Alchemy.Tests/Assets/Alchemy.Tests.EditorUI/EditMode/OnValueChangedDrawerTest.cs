using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class OnValueChangedDrawerTest
    {
        readonly ObjectReferenceValidationTestHelper helper = new ObjectReferenceValidationTestHelper();

        [TearDown]
        public void TearDown() => helper.Dispose();

        [UnityTest]
        public IEnumerator Drawer_InvokesCallbacksResolvedWhenTheElementIsCreated()
        {
            var host = helper.CreateHost<OnValueChangedDrawerHost>();
            host.value = 1;
            helper.ShowInspector(host);
            // Let TrackPropertyValue register against the current value before editing.
            // 6000.0 does not report a change applied on the tracked SerializedObject itself.
            for (var i = 0; i < 5; i++) yield return null;

            Assert.That(host.calls, Is.Empty);

            var serializedObject = helper.Editor.serializedObject;
            SetValue(host, serializedObject, 5);
            foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => host.calls.Count >= 1))
                yield return wait;
            Assert.That(host.calls, Is.EqualTo(new[] { 5 }));

            SetValue(host, serializedObject, 9);
            foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => host.calls.Count >= 2))
                yield return wait;
            Assert.That(host.calls, Is.EqualTo(new[] { 5, 9 }));
        }

        static void SetValue(OnValueChangedDrawerHost host, SerializedObject serializedObject, int value)
        {
            using (var editing = new SerializedObject(host))
            {
                editing.FindProperty(nameof(OnValueChangedDrawerHost.value)).intValue = value;
                editing.ApplyModifiedPropertiesWithoutUndo();
            }

            serializedObject.Update();
        }
    }
}
