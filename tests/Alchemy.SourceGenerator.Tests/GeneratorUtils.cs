using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Alchemy.SourceGenerator.Tests;

/// <summary>
/// The outcome of running <see cref="AlchemySerializeGenerator"/> over a piece of source text.
/// </summary>
public sealed record GeneratorResult(
    ImmutableArray<Diagnostic> GeneratorDiagnostics,
    ImmutableArray<GeneratedSource> GeneratedSources,
    Compilation OutputCompilation)
{
    /// <summary>Diagnostics the generator itself reported (ALCHEMY001-003, crash reports).</summary>
    public IEnumerable<Diagnostic> Errors =>
        GeneratorDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);

    public IEnumerable<Diagnostic> Warnings =>
        GeneratorDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);

    public bool HasDiagnostic(string id) => GeneratorDiagnostics.Any(d => d.Id == id);

    /// <summary>All generated code concatenated, convenient for substring assertions.</summary>
    public string AllGeneratedText =>
        string.Join("\n", GeneratedSources.Select(s => s.Text));

    /// <summary>
    /// Compile errors produced by the *final* compilation (original source + generated code).
    /// This is what proves the generator emitted valid C#.
    /// </summary>
    public ImmutableArray<Diagnostic> CompilationErrors =>
        OutputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

    public string DescribeCompilationErrors() =>
        CompilationErrors.Length == 0
            ? "<none>"
            : string.Join("\n", CompilationErrors.Select(d => $"{d.Id}: {d.GetMessage()} @ {d.Location.GetLineSpan()}"));
}

public sealed record GeneratedSource(string HintName, string Text);

/// <summary>
/// Compiles source text and runs the Alchemy source generator over it.
/// </summary>
public static class GeneratorUtils
{
    static readonly ImmutableArray<MetadataReference> FrameworkReferences = LoadFrameworkReferences();

    static ImmutableArray<MetadataReference> LoadFrameworkReferences()
    {
        // Reference every assembly in the running framework so the test source can use
        // anything from the BCL without pulling in a reference-assembly package.
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "";
        return tpa.Split(Path.PathSeparator)
            .Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && File.Exists(p))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToImmutableArray();
    }

    /// <summary>
    /// Minimal stand-ins for the Unity and Alchemy types the generated code references.
    /// Prepended to every test compilation so that "does the generated code compile?"
    /// is a meaningful question without a Unity install.
    /// </summary>
    public const string Stubs = """
        namespace UnityEngine
        {
            public class Object { }
            public class GameObject : Object { }
            public class MonoBehaviour : Object { }
            public interface ISerializationCallbackReceiver
            {
                void OnBeforeSerialize();
                void OnAfterDeserialize();
            }
            [System.AttributeUsage(System.AttributeTargets.Field)]
            public sealed class SerializeField : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Field)]
            public sealed class HideInInspector : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Field)]
            public sealed class TextArea : System.Attribute
            {
                public TextArea() { }
                public TextArea(int minLines, int maxLines) { }
            }
            public static class Debug
            {
                public static void LogException(System.Exception ex) { }
            }
        }

        namespace Alchemy.Serialization
        {
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
            public sealed class AlchemySerializeAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.Field)]
            public sealed class AlchemySerializeFieldAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
            public sealed class ShowAlchemySerializationDataAttribute : System.Attribute { }

            public interface IAlchemySerializationCallbackReceiver
            {
                void OnBeforeSerialize();
                void OnAfterDeserialize();
            }
        }

        namespace Alchemy.Serialization.Internal
        {
            public static class SerializationHelper
            {
                public static string ToJson<T>(T target, System.Collections.Generic.IList<UnityEngine.Object> unityObjectReferences) => "";
                public static T FromJson<T>(string json, System.Collections.Generic.IList<UnityEngine.Object> unityObjectReferences) => default!;
            }
        }

        namespace Alchemy.Inspector
        {
            [System.AttributeUsage(System.AttributeTargets.All)]
            public sealed class LabelTextAttribute : System.Attribute
            {
                public LabelTextAttribute(string text) { }
            }
            [System.AttributeUsage(System.AttributeTargets.All)]
            public sealed class ReadOnlyAttribute : System.Attribute { }
        }
        """;

    /// <summary>Runs the generator over <paramref name="sources"/>, with the Unity/Alchemy stubs included.</summary>
    public static GeneratorResult Run(params string[] sources) =>
        RunCore(new AlchemySerializeGenerator(), includeStubs: true, sources);

    /// <summary>Runs the generator over <paramref name="sources"/> only, without the stub types.</summary>
    public static GeneratorResult RunWithoutStubs(params string[] sources) =>
        RunCore(new AlchemySerializeGenerator(), includeStubs: false, sources);

    static GeneratorResult RunCore(ISourceGenerator generator, bool includeStubs, string[] sources)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

        var trees = new List<SyntaxTree>();
        if (includeStubs) trees.Add(CSharpSyntaxTree.ParseText(Stubs, parseOptions, path: "Stubs.cs"));
        for (var i = 0; i < sources.Length; i++)
        {
            trees.Add(CSharpSyntaxTree.ParseText(sources[i], parseOptions, path: $"Source{i}.cs"));
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: "AlchemyGeneratorTestAssembly",
            syntaxTrees: trees,
            references: FrameworkReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // AlchemySerializeGenerator implements the v1 ISourceGenerator interface, so it is
        // passed straight through (AsSourceGenerator() is the IIncrementalGenerator adapter).
        var driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator },
            additionalTexts: null,
            parseOptions: parseOptions,
            optionsProvider: null);

        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();

        var generated = runResult.Results
            .SelectMany(r => r.GeneratedSources)
            .Select(s => new GeneratedSource(s.HintName, s.SourceText.ToString()))
            .ToImmutableArray();

        return new GeneratorResult(diagnostics, generated, outputCompilation);
    }
}
