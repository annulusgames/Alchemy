using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alchemy.Docs;

internal static class SampleExtractor
{
    public const string DocumentRegion = "document";
    public const string CaptureStart = "__docCaptureStart";
    public const string CaptureEnd = "__docCaptureEnd";

    static readonly Regex DocumentRegionRegex = new(
        @"^\s*#\s*region\s+document\s*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    public static Dictionary<string, SampleInfo> Load(RepoPaths paths)
    {
        var map = new Dictionary<string, SampleInfo>(StringComparer.Ordinal);
        if (!Directory.Exists(paths.SamplesRoot))
        {
            return map;
        }

        foreach (var file in Directory.EnumerateFiles(
                     paths.SamplesRoot,
                     "*.cs",
                     SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}Pipeline{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                file.Contains($"{Path.DirectorySeparatorChar}EditMode{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(text);
            foreach (var type in tree.GetCompilationUnitRoot()
                         .DescendantNodes()
                         .OfType<ClassDeclarationSyntax>())
            {
                if (!TryGetDocumentationSample(type, out var capture))
                {
                    continue;
                }

                map[type.Identifier.Text] = new SampleInfo(
                    type.Identifier.Text,
                    Path.GetFullPath(file),
                    HasDocumentRegion(text),
                    capture);
            }
        }

        return map;
    }

    static bool TryGetDocumentationSample(ClassDeclarationSyntax type, out bool capture)
    {
        capture = true;
        foreach (var attribute in type.AttributeLists.SelectMany(a => a.Attributes))
        {
            var name = attribute.Name.ToString();
            if (name is not ("DocumentationSample" or "DocumentationSampleAttribute"))
            {
                continue;
            }

            capture = ReadCaptureFlag(attribute);
            return true;
        }

        return false;
    }

    static bool ReadCaptureFlag(AttributeSyntax attribute)
    {
        if (attribute.ArgumentList is null)
        {
            return true;
        }

        foreach (var argument in attribute.ArgumentList.Arguments)
        {
            if (argument.NameEquals?.Name.Identifier.Text != "Capture")
            {
                continue;
            }

            return argument.Expression switch
            {
                LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.FalseLiteralExpression) => false,
                LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.TrueLiteralExpression) => true,
                _ => true,
            };
        }

        return true;
    }

    static bool HasDocumentRegion(string text) =>
        DocumentRegionRegex.IsMatch(text);
}
