using System;
using System.IO;
using System.Linq;
using Alchemy.Editor;
using Alchemy.Tests.EditorUI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.Editor
{
    public class DocumentationEditorSamplesTest
    {
        [Test]
        public void AlchemyEditorWindow_BuildsDocumentedFields()
        {
            var window = ScriptableObject.CreateInstance<
                DocumentationEditorWindow>();
            try
            {
                window.BuildForTest();
                Assert.That(window.rootVisualElement.childCount, Is.GreaterThan(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void SavingEditorWindowData_RoundTripsJson()
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                $"Alchemy-{Guid.NewGuid():N}.json");
            var window = ScriptableObject.CreateInstance<
                DocumentationSavingEditorWindow>();
            try
            {
                window.SetDataPath(path);
                window.value = "saved";
                window.SaveForTest();
                window.value = string.Empty;
                window.LoadForTest();

                Assert.That(window.value, Is.EqualTo("saved"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void CustomAttributeDrawer_IsRegistered()
        {
            var drawerTypes = TypeCache.GetTypesWithAttribute<
                CustomAttributeDrawerAttribute>();

            Assert.That(
                drawerTypes.Contains(typeof(DocumentationHelpBoxDrawer)),
                Is.True);
        }

        [Test]
        public void CustomGroupDrawer_BuildsFoldout()
        {
            var drawerType = AlchemyEditorUtility.FindGroupDrawerType(
                new DocumentationFoldoutGroupAttribute("Documentation"));
            var drawer = new DocumentationFoldoutGroupDrawer();
            var element = drawer.CreateRootElement("Documentation");

            Assert.That(
                drawerType,
                Is.EqualTo(typeof(DocumentationFoldoutGroupDrawer)));
            Assert.That(element, Is.TypeOf<Foldout>());
            Assert.That(((Foldout)element).text, Is.EqualTo("Documentation"));
        }

        [Test]
        public void ExtendedAlchemyEditor_ReturnsCustomInspector()
        {
            var target = ScriptableObject.CreateInstance<
                DocumentationEditorTarget>();
            var editor = UnityEditor.Editor.CreateEditor(
                target,
                typeof(DocumentationAlchemyEditor));
            try
            {
                var root = editor.CreateInspectorGUI();

                Assert.That(
                    root.Query<Label>()
                        .ToList()
                        .Any(label =>
                            label.text == "Documentation custom editor"),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

#if ALCHEMY_SUPPORT_SERIALIZATION
        [Test]
        public void SerializationExtension_RoundTripsDocumentedDictionary()
        {
            var gameObject = new GameObject("Documentation");
            try
            {
                var sample = gameObject.AddComponent<AlchemySerializationTest>();
                sample.dictionary["gameObject"] = gameObject;
                var receiver = (ISerializationCallbackReceiver)sample;

                receiver.OnBeforeSerialize();
                sample.dictionary.Clear();
                receiver.OnAfterDeserialize();

                Assert.That(
                    sample.dictionary["gameObject"],
                    Is.SameAs(gameObject));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SerializationCallback_ReceivesGeneratedCallbacks()
        {
            var gameObject = new GameObject("Documentation");
            try
            {
                var sample = gameObject.AddComponent<SerializationCallbackTest>();
                var receiver = (ISerializationCallbackReceiver)sample;

                receiver.OnBeforeSerialize();
                receiver.OnAfterDeserialize();

                Assert.That(sample.beforeSerializeCount, Is.EqualTo(1));
                Assert.That(sample.afterDeserializeCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
#endif
    }
}
