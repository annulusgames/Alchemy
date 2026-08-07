namespace Alchemy.Docs;

internal sealed record AttributeDocMember(
    string Name,
    string? Summary);

internal sealed record AttributeNote(
    string Type,
    string Body);

internal sealed record AttributeInfo(
    string TypeName,
    string DisplayName,
    string Slug,
    string SampleTypeName,
    string Category,
    string? Summary,
    IReadOnlyList<AttributeNote> Notes,
    IReadOnlyList<AttributeDocMember> Parameters,
    string SourcePath);

internal sealed record SampleInfo(
    string TypeName,
    string FilePath,
    bool HasDocumentRegion,
    bool Capture);

internal sealed record LocaleI18n(
    string? Summary,
    IReadOnlyList<AttributeNote> Notes,
    Dictionary<string, string> Params);

internal sealed record GeneratedFile(
    string Path,
    string Content);
