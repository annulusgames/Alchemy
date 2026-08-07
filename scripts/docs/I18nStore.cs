using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alchemy.Docs;

internal static class I18nStore
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static LocaleFile LoadOrEmpty(RepoPaths paths, string languageCode)
    {
        languageCode = DocLanguage.Normalize(languageCode);
        if (DocLanguage.IsSource(languageCode))
        {
            return LocaleFile.Empty(languageCode);
        }

        var path = PathFor(paths, languageCode);
        if (!File.Exists(path))
        {
            return LocaleFile.Empty(languageCode);
        }

        var root = JsonSerializer.Deserialize<LocaleRootDto>(
            File.ReadAllText(path),
            JsonOptions);
        if (root?.Attributes is null)
        {
            return LocaleFile.Empty(languageCode);
        }

        var entries = new Dictionary<string, LocaleI18n>(StringComparer.Ordinal);
        foreach (var (typeName, entry) in root.Attributes)
        {
            if (entry is null || string.IsNullOrWhiteSpace(typeName))
            {
                continue;
            }

            entries[typeName] = ToLocale(entry);
        }

        return new LocaleFile(languageCode, entries);
    }

    public static LocaleI18n? TryGet(LocaleFile file, AttributeInfo attribute) =>
        file.Entries.TryGetValue(attribute.TypeName, out var locale) ? locale : null;

    public static string Serialize(IReadOnlyDictionary<string, LocaleI18n> entries)
    {
        var ordered = entries
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToDictionary(
                kv => kv.Key,
                kv => ToDto(kv.Value),
                StringComparer.Ordinal);

        var root = new LocaleRootDto { Attributes = ordered };
        return JsonSerializer.Serialize(root, JsonOptions) + Environment.NewLine;
    }

    public static LocaleI18n StubFromEnglish(AttributeInfo attribute) =>
        FromEnglish(attribute);

    public static string PathFor(RepoPaths paths, string languageCode) =>
        Path.Combine(paths.I18nRoot, DocLanguage.Normalize(languageCode) + ".json");

    public static LocaleI18n FromEnglish(AttributeInfo attribute) =>
        new(
            attribute.Summary,
            attribute.Notes,
            attribute.Parameters.ToDictionary(
                p => p.Name,
                p => p.Summary ?? string.Empty,
                StringComparer.Ordinal));

    static LocaleI18n ToLocale(LocaleI18nDto dto)
    {
        var notes = (dto.Notes ?? [])
            .Where(n => !string.IsNullOrWhiteSpace(n.Type) && !string.IsNullOrWhiteSpace(n.Body))
            .Select(n => new AttributeNote(n.Type!.Trim(), n.Body!.Trim()))
            .ToArray();

        return new LocaleI18n(
            dto.Summary,
            notes,
            dto.Params ?? new Dictionary<string, string>(StringComparer.Ordinal));
    }

    static LocaleI18nDto ToDto(LocaleI18n locale) =>
        new()
        {
            Summary = locale.Summary,
            Notes = locale.Notes.Count == 0
                ? null
                : locale.Notes.Select(n => new LocaleNoteDto { Type = n.Type, Body = n.Body }).ToList(),
            Params = locale.Params.Count == 0
                ? null
                : new Dictionary<string, string>(locale.Params, StringComparer.Ordinal),
        };

    sealed class LocaleRootDto
    {
        public Dictionary<string, LocaleI18nDto>? Attributes { get; set; }
    }

    sealed class LocaleI18nDto
    {
        public string? Summary { get; set; }
        public List<LocaleNoteDto>? Notes { get; set; }
        public Dictionary<string, string>? Params { get; set; }
    }

    sealed class LocaleNoteDto
    {
        public string? Type { get; set; }
        public string? Body { get; set; }
    }
}

internal sealed class LocaleFile
{
    public LocaleFile(string languageCode, Dictionary<string, LocaleI18n> entries)
    {
        LanguageCode = languageCode;
        Entries = entries;
    }

    public string LanguageCode { get; }
    public Dictionary<string, LocaleI18n> Entries { get; }

    public static LocaleFile Empty(string languageCode) =>
        new(languageCode, new Dictionary<string, LocaleI18n>(StringComparer.Ordinal));
}
