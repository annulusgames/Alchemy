using System;
using System.Collections.Generic;
using System.IO;
using Alchemy.Editor;
using Alchemy.Inspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.Editor
{
    public sealed class DocumentationEditorWindow : AlchemyEditorWindow
    {
        [Serializable]
        [HorizontalGroup]
        public sealed class DatabaseItem
        {
            [LabelWidth(30f)]
            public float foo;

            [LabelWidth(30f)]
            public Vector3 bar;

            [LabelWidth(30f)]
            public GameObject baz;
        }

        [ListViewSettings(
            ShowAlternatingRowBackgrounds = AlternatingRowBackground.All,
            ShowFoldoutHeader = false)]
        public List<DatabaseItem> items;

        [Button, HorizontalGroup]
        public void Button1() { }

        [Button, HorizontalGroup]
        public void Button2() { }

        [Button, HorizontalGroup]
        public void Button3() { }

        [MenuItem("Window/Alchemy Tests/Documentation Editor Window")]
        static void Open()
        {
            var window = GetWindow<DocumentationEditorWindow>(
                "Documentation");
            window.Show();
        }

        public void BuildForTest()
        {
            CreateGUI();
        }
    }

    public sealed class DocumentationSavingEditorWindow : AlchemyEditorWindow
    {
        public string value;

        string dataPath;

        public void SetDataPath(string path)
        {
            dataPath = path;
        }

        public void LoadForTest()
        {
            LoadWindowData(GetWindowDataPath());
        }

        public void SaveForTest()
        {
            SaveWindowData(GetWindowDataPath());
        }

        protected override string GetWindowDataPath()
        {
            return string.IsNullOrWhiteSpace(dataPath)
                ? base.GetWindowDataPath()
                : dataPath;
        }

        protected override void LoadWindowData(string path)
        {
            if (File.Exists(path))
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path), this);
            }
        }

        protected override void SaveWindowData(string path)
        {
            File.WriteAllText(path, JsonUtility.ToJson(this, true));
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class DocumentationHelpBoxAttribute : Attribute
    {
        public DocumentationHelpBoxAttribute(
            string message,
            HelpBoxMessageType messageType = HelpBoxMessageType.Info)
        {
            Message = message;
            MessageType = messageType;
        }

        public string Message { get; }
        public HelpBoxMessageType MessageType { get; }
    }

    [CustomAttributeDrawer(typeof(DocumentationHelpBoxAttribute))]
    public sealed class DocumentationHelpBoxDrawer : AlchemyAttributeDrawer
    {
        HelpBox helpBox;

        public override void OnCreateElement()
        {
            var attribute = (DocumentationHelpBoxAttribute)Attribute;
            helpBox = new HelpBox(
                attribute.Message,
                attribute.MessageType);

            var parent = TargetElement.parent;
            parent.Insert(parent.IndexOf(TargetElement), helpBox);
        }
    }

    public sealed class DocumentationFoldoutGroupAttribute :
        PropertyGroupAttribute
    {
        public DocumentationFoldoutGroupAttribute() { }

        public DocumentationFoldoutGroupAttribute(string groupPath) :
            base(groupPath) { }
    }

    [CustomGroupDrawer(typeof(DocumentationFoldoutGroupAttribute))]
    public sealed class DocumentationFoldoutGroupDrawer : AlchemyGroupDrawer
    {
        public override VisualElement CreateRootElement(string label)
        {
            return new Foldout
            {
                style =
                {
                    width = Length.Percent(100f),
                },
                text = label,
            };
        }
    }

    public sealed class DocumentationEditorTarget : ScriptableObject
    {
        public int value;
    }

    [CustomEditor(typeof(DocumentationEditorTarget))]
    public sealed class DocumentationAlchemyEditor : AlchemyEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = base.CreateInspectorGUI();
            root.Add(new Label("Documentation custom editor"));
            return root;
        }
    }
}
