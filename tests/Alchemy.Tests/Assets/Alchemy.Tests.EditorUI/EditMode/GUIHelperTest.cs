using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Alchemy.Editor;
using NUnit.Framework;
using UnityEditor;
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

        [UnityTest]
        public IEnumerator CreateLine_DrawsMidpointStrokeWithoutImgui()
        {
            var color = new Color(0.25f, 0.5f, 0.75f, 1f);
            const float height = 10f;
            var margin = EditorStyles.layerMaskField.margin;
            var line = GUIHelper.CreateLine(color, height);
            Assert.That(line, Is.Not.InstanceOf<IMGUIContainer>());
            Assert.That(line.Q<IMGUIContainer>(), Is.Null);

            var host = new VisualElement
            {
                style =
                {
                    width = 200f,
                    height = 40f,
                }
            };
            host.Add(line);

            var window = EditModeEditorTestUtility.ShowInWindow(host);
            try
            {
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(() =>
                    line.childCount == 1 && line[0].resolvedStyle.width > 1f))
                    yield return wait;

                Assert.That(line.resolvedStyle.height, Is.EqualTo(height).Within(0.51f));
                Assert.That(line.resolvedStyle.marginTop, Is.EqualTo((float)margin.top).Within(0.51f));
                Assert.That(line.resolvedStyle.marginBottom, Is.EqualTo((float)margin.bottom).Within(0.51f));

                var stroke = line[0];
                Assert.That(stroke.resolvedStyle.height, Is.EqualTo(1f).Within(0.51f));
                Assert.That(stroke.resolvedStyle.backgroundColor, Is.EqualTo(color));
                Assert.That(stroke.resolvedStyle.position, Is.EqualTo(Position.Absolute));
                Assert.That(stroke.layout.x, Is.EqualTo(margin.left + 3f).Within(0.51f));
                Assert.That(stroke.layout.xMax, Is.EqualTo(line.contentRect.width - margin.right).Within(1f));
                Assert.That(stroke.layout.y, Is.EqualTo(height * 0.5f).Within(0.2f));
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
    }
}
