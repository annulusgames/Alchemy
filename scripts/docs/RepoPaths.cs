namespace Alchemy.Docs;

internal sealed class RepoPaths
{
    public required string Root { get; init; }
    public string InspectorAttributes =>
        Path.Combine(Root, "Alchemy", "Assets", "Alchemy", "Runtime", "Inspector", "InspectorAttributes.cs");
    public string GroupAttributes =>
        Path.Combine(Root, "Alchemy", "Assets", "Alchemy", "Runtime", "Inspector", "GroupAttributes.cs");
    public string PropertyGroupAttribute =>
        Path.Combine(Root, "Alchemy", "Assets", "Alchemy", "Runtime", "Inspector", "PropertyGroupAttribute.cs");
    public string SamplesRoot =>
        Path.Combine(Root, "tests", "Alchemy.Tests", "Assets", "Alchemy.Tests.EditorUI");
    public string Images => Path.Combine(Root, "docs", "images", "generated");
    public string UnityProject =>
        Path.Combine(Root, "tests", "versions", "Unity6000.3");

    public string ArticlesRoot => Path.Combine(Root, "docs", "articles");
    public string I18nRoot => Path.Combine(Root, "scripts", "docs", "resources", "i18n");

    public string AttributesDir(string languageCode) =>
        Path.Combine(ArticlesRoot, DocLanguage.Normalize(languageCode), "attributes");

    public string TocPath(string languageCode) =>
        Path.Combine(ArticlesRoot, DocLanguage.Normalize(languageCode), "toc.yml");

    public string I18nFile(string languageCode) =>
        Path.Combine(I18nRoot, DocLanguage.Normalize(languageCode) + ".json");

    public static RepoPaths Locate(string? start = null)
    {
        var dir = new DirectoryInfo(start ?? Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var docs = Path.Combine(dir.FullName, "docs");
            var alchemy = Path.Combine(dir.FullName, "Alchemy");
            if (Directory.Exists(docs) && Directory.Exists(alchemy))
            {
                return new RepoPaths { Root = dir.FullName };
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the Alchemy repository root (expected docs/ and Alchemy/).");
    }
}
