using System;
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
            // 6000.0 polls that tracker only during a panel binding update, which this
            // window skips unless it is repainted.
            for (var i = 0; i < 5; i++)
            {
                helper.Window.Repaint();
                yield return null;
            }

            Assert.That(host.calls, Is.Empty);

            var serializedObject = helper.Editor.serializedObject;
            SetValue(host, serializedObject, 5);
            foreach (var wait in WaitForCalls(serializedObject, () => host.calls.Count >= 1))
                yield return wait;
            Assert.That(host.calls, Is.EqualTo(new[] { 5 }));

            SetValue(host, serializedObject, 9);
            foreach (var wait in WaitForCalls(serializedObject, () => host.calls.Count >= 2))
                yield return wait;
            Assert.That(host.calls, Is.EqualTo(new[] { 5, 9 }));
        }

        IEnumerable WaitForCalls(SerializedObject serializedObject, Func<bool> ready)
        {
            // Refresh the tracked object, then repaint so 6000.0's binding update can observe it.
            foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
            {
                serializedObject.UpdateIfRequiredOrScript();
                helper.Window.Repaint();
                return ready();
            }))
                yield return wait;
        }

        static void SetValue(OnValueChangedDrawerHost host, SerializedObject serializedObject, int value)
        {
            using (var editing = new SerializedObject(host))
            {
                editing.FindProperty(nameof(OnValueChangedDrawerHost.value)).intValue = value;
                editing.ApplyModifiedPropertiesWithoutUndo();
            }

            serializedObject.UpdateIfRequiredOrScript();
        }
    }
}
