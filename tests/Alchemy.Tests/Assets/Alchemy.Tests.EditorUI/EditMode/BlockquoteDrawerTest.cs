using System;
using System.Collections;
using Alchemy.Editor;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class BlockquoteDrawerTest
    {
        const string Quote = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.";

        [UnityTest]
        public IEnumerator Drawer_BuildsToolkitQuoteWithoutMutatingEditorStylesLabel()
        {
            // Batch-mode runs this early, before any IMGUI pass has initialized EditorStyles.
            var window = EditModeEditorTestUtility.ShowInWindow(new VisualElement());
            try
            {
                foreach (var wait in EditModeEditorTestUtility.WaitUntil(CanReadEditorStyles))
                    yield return wait;

                var wordWrap = EditorStyles.label.wordWrap;
                try
                {
                    EditorStyles.label.wordWrap = false;

                    var parent = new VisualElement();
                    var field = new VisualElement();
                    parent.Add(field);
                    var member = typeof(BlockquoteDrawerHost).GetField(nameof(BlockquoteDrawerHost.value));
                    AlchemyAttributeDrawer.ExecutePropertyDrawers(null, null, new BlockquoteDrawerHost(), member, field);

                    Assert.That(EditorStyles.label.wordWrap, Is.False);
                    Assert.That(parent.Q<IMGUIContainer>(), Is.Null);
                    Assert.That(parent.IndexOf(field), Is.EqualTo(1));

                    var blockquote = parent[0];
                    var textColor = GUIHelper.TextColor;
                    var backgroundColor = textColor;
                    backgroundColor.a = 0.06f;
                    Assert.That(blockquote.style.backgroundColor.value, Is.EqualTo(backgroundColor));
                    Assert.That(blockquote.style.borderLeftWidth.value, Is.EqualTo(3f));
                    Assert.That(blockquote.style.borderLeftColor.value, Is.EqualTo(textColor));
                    Assert.That(blockquote.style.paddingLeft.value, Is.EqualTo(new Length(4f)));
                    Assert.That(blockquote.style.borderLeftWidth.value + blockquote.style.paddingLeft.value.value, Is.EqualTo(7f));
                    Assert.That(blockquote.style.paddingTop.value, Is.EqualTo(new Length(EditorGUIUtility.standardVerticalSpacing)));
                    Assert.That(blockquote.style.paddingBottom.value, Is.EqualTo(new Length(EditorGUIUtility.standardVerticalSpacing)));

                    var margin = EditorStyles.layerMaskField.margin;
                    Assert.That(blockquote.style.marginTop.value, Is.EqualTo(new Length(margin.top)));
                    Assert.That(blockquote.style.marginBottom.value, Is.EqualTo(new Length(margin.bottom)));
                    Assert.That(blockquote.style.marginLeft.value, Is.EqualTo(new Length(margin.left)));
                    Assert.That(blockquote.style.marginRight.value, Is.EqualTo(new Length(margin.right)));

                    var label = blockquote.Q<Label>();
                    Assert.That(label, Is.Not.Null);
                    Assert.That(label.text, Is.EqualTo(Quote));
                    Assert.That(label.style.whiteSpace.value, Is.EqualTo(WhiteSpace.Normal));
                }
                finally
                {
                    EditorStyles.label.wordWrap = wordWrap;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        static bool CanReadEditorStyles()
        {
            try
            {
                _ = EditorStyles.label.wordWrap;
                return true;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }

        sealed class BlockquoteDrawerHost
        {
            [Blockquote(Quote)]
            public int value;
        }
    }
}
