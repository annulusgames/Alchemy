using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alchemy.Tests.EditorUI;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Alchemy.Tests.Pipeline
{
    internal static class DocumentationPrefabCommands
    {
        const string PackagePath =
            "Packages/com.annulusgames.alchemy.editor-ui-test";
        const string DocumentationPath = PackagePath + "/Documentation";
        const string ScriptableObjectPath =
            DocumentationPath + "/DocumentationSampleScriptableObject.asset";

        [CliCommand(
            "alchemy_editor_ui_generate_documentation_prefabs",
            "Create prefabs for Inspector-facing documentation samples.",
            MainThreadRequired = true)]
        static GenerationResult Generate()
        {
            var created = new List<string>();
            var existing = new List<string>();
            var sampleTypes = TypeCache
                .GetTypesWithAttribute<DocumentationSampleAttribute>()
                .Where(type =>
                    !type.IsAbstract &&
                    typeof(MonoBehaviour).IsAssignableFrom(type))
                .OrderBy(type => type.Name, StringComparer.Ordinal)
                .ToArray();

            foreach (var type in sampleTypes)
            {
                var prefabPath = $"{PackagePath}/{type.Name}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                {
                    existing.Add(prefabPath);
                    continue;
                }

                var root = new GameObject(type.Name);
                try
                {
                    var component = root.AddComponent(type);
                    InitializeSample(component);
                    if (PrefabUtility.SaveAsPrefabAsset(root, prefabPath) == null)
                    {
                        throw new InvalidOperationException(
                            $"Unity could not create documentation prefab '{prefabPath}'.");
                    }

                    created.Add(prefabPath);
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return new GenerationResult
            {
                Success = true,
                Created = created.ToArray(),
                Existing = existing.ToArray(),
            };
        }

        static void InitializeSample(Component component)
        {
            if (!(component is InlineEditorTest inlineEditor))
            {
                return;
            }

            var sample = AssetDatabase.LoadAssetAtPath<
                DocumentationSampleScriptableObject>(ScriptableObjectPath);
            if (sample == null)
            {
                sample = ScriptableObject.CreateInstance<
                    DocumentationSampleScriptableObject>();
                sample.name = nameof(DocumentationSampleScriptableObject);
                AssetDatabase.CreateAsset(sample, ScriptableObjectPath);
            }

            inlineEditor.sample = sample;
            EditorUtility.SetDirty(inlineEditor);
        }

        sealed class GenerationResult
        {
            public bool Success { get; set; }
            public string[] Created { get; set; }
            public string[] Existing { get; set; }
        }
    }
}
