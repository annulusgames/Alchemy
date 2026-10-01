using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Alchemy.Editor;
using NUnit.Framework;
#if !UNITY_2022_1_OR_NEWER
using UnityEditor.UIElements;
#endif
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class GUIHelperTest
    {
        [UnityTest]
        public IEnumerator ScheduleAdjustLabelWidth_ReattachStillUpdatesLabel()
        {
            var field = new IntegerField("Value") { value = 1 };
            var window = EditModeEditorTestUtility.ShowInWindow(field);
            try
            {
                GUIHelper.ScheduleAdjustLabelWidth(field);
                var visualTree = field.panel.visualTree;
                window.position = new Rect(0f, 0f, 800f, 480f);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    Mathf.Abs(visualTree.resolvedStyle.width - 800f) < 1f))
                    yield return wait;

                var label = field.Q<Label>();
                Assert.That(label, Is.Not.Null);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    Mathf.Abs(label.resolvedStyle.width - GUIHelper.CalculateLabelWidth(field, visualTree)) < 1f))
                    yield return wait;
                Assert.That(label.resolvedStyle.width, Is.GreaterThan(0f));
                var originalWidth = label.resolvedStyle.width;

                field.RemoveFromHierarchy();
                window.position = new Rect(0f, 0f, 500f, 480f);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    Mathf.Abs(visualTree.resolvedStyle.width - 500f) < 1f))
                    yield return wait;

                window.rootVisualElement.Add(field);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    label.resolvedStyle.width < originalWidth))
                    yield return wait;

                Assert.That(label.resolvedStyle.width, Is.LessThan(originalWidth));
                Assert.That(label.resolvedStyle.width,
                    Is.EqualTo(GUIHelper.CalculateLabelWidth(field, field.panel.visualTree)).Within(1f));

                window.position = new Rect(0f, 0f, 600f, 480f);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    Mathf.Abs(visualTree.resolvedStyle.width - 600f) < 1f &&
                    Mathf.Abs(label.resolvedStyle.width - GUIHelper.CalculateLabelWidth(field, visualTree)) < 1f))
                    yield return wait;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [UnityTest]
        public IEnumerator ScheduleAdjustLabelWidth_DoesNotKeepDetachedElementAlive()
        {
            var window = EditModeEditorTestUtility.ShowInWindow(new VisualElement());
            try
            {
                var weak = CreateDetachedField(window.rootVisualElement);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    return !weak.IsAlive;
                }))
                    yield return wait;

                Assert.That(weak.IsAlive, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakReference CreateDetachedField(VisualElement root)
        {
            var field = new IntegerField("Value") { value = 1 };
            root.Add(field);
            GUIHelper.ScheduleAdjustLabelWidth(field);
            field.RemoveFromHierarchy();
            return new WeakReference(field);
        }

        [Test]
        public void ScheduleSetLabelWidth_SetsExistingLabelImmediately()
        {
            var field = new IntegerField("Value");
            var label = field.Q<Label>();
            Assert.That(label, Is.Not.Null);

            GUIHelper.ScheduleSetLabelWidth(field, 64f);

            Assert.That(label.style.width.value.value, Is.EqualTo(64f));
            Assert.That(label.style.minWidth.value.value, Is.EqualTo(64f));
        }

        [UnityTest]
        public IEnumerator ScheduleSetLabelWidth_AppliesWhenLabelAppears()
        {
            var element = new VisualElement();
            GUIHelper.ScheduleSetLabelWidth(element, 80f);
            var window = EditModeEditorTestUtility.ShowInWindow(element);
            try
            {
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => element.resolvedStyle.width > 0f))
                    yield return wait;

                var label = new Label("Name");
                element.Add(label);
                element.style.width = 300f;
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    label.style.width.value.value == 80f && label.style.minWidth.value.value == 80f))
                    yield return wait;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [UnityTest]
        public IEnumerator ScheduleSetLabelWidth_AppliesLabelAddedWithoutSizeChange()
        {
            var element = new VisualElement();
            element.style.width = 300f;
            element.style.height = 20f;
            var window = EditModeEditorTestUtility.ShowInWindow(element);
            try
            {
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => element.resolvedStyle.width > 0f))
                    yield return wait;

                // Geometry would also apply the width. Block it so this covers the bounded retry.
                element.RegisterCallback<GeometryChangedEvent>(evt => evt.StopImmediatePropagation());
                GUIHelper.ScheduleSetLabelWidth(element, 80f);
                var label = new Label("Name");
                element.Add(label);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    label.style.width.value.value == 80f && label.style.minWidth.value.value == 80f))
                    yield return wait;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [UnityTest]
        public IEnumerator ScheduleSetLabelWidth_StopsRetryWhenNoLabelAppears()
        {
            var element = new VisualElement();
            element.style.width = 300f;
            element.style.height = 20f;
            var window = EditModeEditorTestUtility.ShowInWindow(element);
            try
            {
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => element.resolvedStyle.width > 0f))
                    yield return wait;

                var ticks = 0;
                element.schedule.Execute(() => ticks++).Every(0);
                element.RegisterCallback<GeometryChangedEvent>(evt => evt.StopImmediatePropagation());
                GUIHelper.ScheduleSetLabelWidth(element, 80f);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(
                    () => ticks >= GUIHelper.LabelWidthRetryLimit + 2, 10f))
                    yield return wait;

                var label = new Label("Name");
                element.Add(label);
                var seen = ticks;
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() => ticks >= seen + 3, 10f))
                    yield return wait;

                Assert.That(label.style.width.value.value, Is.Not.EqualTo(80f));
                Assert.That(label.style.minWidth.value.value, Is.Not.EqualTo(80f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [UnityTest]
        public IEnumerator ScheduleSetLabelWidth_DoesNotKeepDetachedElementAlive()
        {
            var window = EditModeEditorTestUtility.ShowInWindow(new VisualElement());
            try
            {
                var weak = CreateDetachedLabelWidthTarget(window.rootVisualElement);
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    return !weak.IsAlive;
                }))
                    yield return wait;

                Assert.That(weak.IsAlive, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakReference CreateDetachedLabelWidthTarget(VisualElement root)
        {
            var element = new VisualElement();
            root.Add(element);
            GUIHelper.ScheduleSetLabelWidth(element, 80f);
            element.RemoveFromHierarchy();
            return new WeakReference(element);
        }
    }
}
