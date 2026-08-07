namespace Alchemy.Docs;

/// <summary>
/// Documentation locale codes. Source prose is English XML; other languages use i18n sidecars.
/// Add a new code here (plus toc marker / table headers) when introducing another locale.
/// </summary>
internal static class DocLanguage
{
    public const string En = "en";
    public const string Ja = "ja";

    /// <summary>Source-of-truth locale (attribute /// XML). No i18n sidecar required.</summary>
    public const string Source = En;

    public static IReadOnlyList<string> All { get; } = [En, Ja];

    public static IReadOnlyList<string> Localized { get; } =
        All.Where(code => !IsSource(code)).ToArray();

    public static bool IsSource(string languageCode) =>
        string.Equals(languageCode, Source, StringComparison.OrdinalIgnoreCase);

    public static void EnsureSupported(string languageCode)
    {
        if (All.Any(code => string.Equals(code, languageCode, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported documentation language '{languageCode}'. " +
            $"Known: {string.Join(", ", All)}.");
    }

    public static string Normalize(string languageCode)
    {
        EnsureSupported(languageCode);
        return All.First(code =>
            string.Equals(code, languageCode, StringComparison.OrdinalIgnoreCase));
    }

    public static string AttributeListTocMarker(string languageCode) =>
        Normalize(languageCode) switch
        {
            Ja => "- name: 属性一覧",
            _ => "- name: Attributes",
        };

    public static string ParameterTableHeader(string languageCode) =>
        Normalize(languageCode) switch
        {
            Ja => "| パラメータ | 説明 |",
            _ => "| Parameter | Description |",
        };
}
