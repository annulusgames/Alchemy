using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text;
using TUnit.Core;

namespace Alchemy.UnityTestRunner;

internal static class EditorCaptureReport
{
    private const string TemplateResourceName =
        "Alchemy.UnityTestRunner.EditorCaptureReport.template.html";

    private static readonly object Gate = new();
    private static readonly ConcurrentDictionary<CaptureKey, string> Captures =
        new();
    private static readonly Dictionary<string, List<string>>
        ExpectedTestsBySurface = new(StringComparer.Ordinal);
    private static readonly HashSet<string> RegisteredVersions =
        new(StringComparer.Ordinal);
    private static readonly string RunId =
        $"{DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}-" +
        Guid.NewGuid().ToString("N")[..8];

    private static string? captureRoot;

    public static string RegisterSurface(
        UnityProject project,
        string surface,
        IReadOnlyList<string> expectedTestNames)
    {
        var root = Path.GetFullPath(
            Path.Combine(
                project.ProjectPath,
                "..",
                "..",
                "captures",
                RunId));
        lock (Gate)
        {
            if (captureRoot is null)
            {
                captureRoot = root;
            }
            else if (!PathsEqual(captureRoot, root))
            {
                throw new InvalidConfigurationException(
                    "Editor UI captures from one test session must share a " +
                    $"capture root. Expected '{captureRoot}', received '{root}'.");
            }

            RegisteredVersions.Add(project.VersionLine);
            if (!ExpectedTestsBySurface.TryGetValue(
                    surface,
                    out var registeredTests))
            {
                registeredTests = [];
                ExpectedTestsBySurface.Add(surface, registeredTests);
            }

            foreach (var testName in expectedTestNames)
            {
                if (!registeredTests.Contains(
                        testName,
                        StringComparer.Ordinal))
                {
                    registeredTests.Add(testName);
                }
            }
        }

        var surfaceDirectory = Path.Combine(
            root,
            project.VersionLine,
            surface);
        Directory.CreateDirectory(surfaceDirectory);
        return surfaceDirectory;
    }

    public static void RecordCapture(
        UnityProject project,
        string surface,
        string testName,
        string path)
    {
        Captures[new CaptureKey(surface, project.VersionLine, testName)] =
            Path.GetFullPath(path);
    }

    public static void GenerateAndAttach(TestSessionContext context)
    {
        string? root;
        string[] registeredVersions;
        Dictionary<string, IReadOnlyList<string>> expectedTestsBySurface;
        lock (Gate)
        {
            root = captureRoot;
            registeredVersions = RegisteredVersions.ToArray();
            expectedTestsBySurface = ExpectedTestsBySurface.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value.ToArray(),
                StringComparer.Ordinal);
        }

        if (root is null)
        {
            return;
        }

        var captures = Captures.ToDictionary();
        var versions = registeredVersions
            .Concat(captures.Keys.Select(key => key.VersionLine))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(ParseVersion)
            .ToArray();
        var surfaces = expectedTestsBySurface.Keys
            .Concat(captures.Keys.Select(key => key.Surface))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var testNamesBySurface = surfaces.ToDictionary(
            surface => surface,
            surface => expectedTestsBySurface.GetValueOrDefault(surface, [])
                .Concat(
                    captures.Keys
                        .Where(key => key.Surface == surface)
                        .Select(key => key.TestName))
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            StringComparer.Ordinal);
        var reportPath = Path.Combine(root, "index.html");
        var html = BuildHtml(versions, testNamesBySurface, captures);
        File.WriteAllText(
            reportPath,
            html,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        context.AddArtifact(
            new Artifact
            {
                File = new FileInfo(reportPath),
                DisplayName = "Editor UI capture matrix",
                Description =
                    "Interactive Unity version-by-case Editor UI captures.",
            });
        Console.Out.WriteLine($"[Editor UI] Capture matrix: {reportPath}");
    }

    private static string BuildHtml(
        IReadOnlyList<string> versions,
        IReadOnlyDictionary<string, string[]> testNamesBySurface,
        IReadOnlyDictionary<CaptureKey, string> captures)
    {
        var capturedCount = captures.Count(pair => File.Exists(pair.Value));
        var testCaseCount = testNamesBySurface.Sum(pair => pair.Value.Length);
        var totalCount = versions.Count * testCaseCount;
        return LoadTemplate()
            .Replace(
                "{{CAPTURED_COUNT}}",
                capturedCount.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal)
            .Replace(
                "{{TOTAL_COUNT}}",
                totalCount.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal)
            .Replace(
                "{{VERSION_COUNT}}",
                versions.Count.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal)
            .Replace(
                "{{TEST_CASE_COUNT}}",
                testCaseCount.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal)
            .Replace(
                "{{MATRICES}}",
                BuildMatrices(versions, testNamesBySurface, captures),
                StringComparison.Ordinal);
    }

    private static string BuildMatrices(
        IReadOnlyList<string> versions,
        IReadOnlyDictionary<string, string[]> testNamesBySurface,
        IReadOnlyDictionary<CaptureKey, string> captures)
    {
        var html = new StringBuilder(64 * 1024);
        foreach (var (surface, testNames) in testNamesBySurface)
        {
            html.AppendLine("<section class=\"surface\">");
            html.Append("  <h2>")
                .Append(Encode(surface))
                .AppendLine("</h2>");
            html.AppendLine(
                """
                  <div class="matrix">
                    <table>
                      <thead>
                        <tr>
                          <th scope="col">Test case</th>
                """);

            foreach (var version in versions)
            {
                html.Append("          <th scope=\"col\">")
                    .Append(Encode(version))
                    .AppendLine("</th>");
            }

            html.AppendLine(
                """
                        </tr>
                      </thead>
                      <tbody>
                """);

            foreach (var testName in testNames)
            {
                html.AppendLine("        <tr>");
                html.Append("          <th scope=\"row\">")
                    .Append(Encode(testName))
                    .AppendLine("</th>");
                foreach (var version in versions)
                {
                    AppendCaptureCell(
                        html,
                        surface,
                        version,
                        testName,
                        captures);
                }

                html.AppendLine("        </tr>");
            }

            html.AppendLine(
                """
                      </tbody>
                    </table>
                  </div>
                </section>
                """);
        }

        return html.ToString();
    }

    private static void AppendCaptureCell(
        StringBuilder html,
        string surface,
        string version,
        string testName,
        IReadOnlyDictionary<CaptureKey, string> captures)
    {
        var key = new CaptureKey(surface, version, testName);
        var label = $"{surface} {testName}, Unity {version}";
        html.AppendLine("          <td>");
        if (captures.TryGetValue(key, out var path) &&
            File.Exists(path))
        {
            html.Append("            <button type=\"button\" class=\"capture\"")
                .Append(" aria-label=\"")
                .Append(Encode($"{label}, captured"))
                .Append("\" aria-pressed=\"false\" data-surface=\"")
                .Append(Encode(surface))
                .Append("\" data-test=\"")
                .Append(Encode(testName))
                .Append("\" data-version=\"")
                .Append(Encode(version))
                .AppendLine("\">");
            html.Append("              <img src=\"")
                .Append(CreatePngDataUri(path))
                .Append("\" alt=\"")
                .Append(Encode($"{label}, captured"))
                .AppendLine("\" loading=\"lazy\">");
            html.AppendLine("            </button>");
        }
        else
        {
            html.Append("            <div class=\"missing\" aria-label=\"")
                .Append(Encode($"{label}, missing"))
                .AppendLine("\">Not captured</div>");
        }

        html.AppendLine("          </td>");
    }

    private static string LoadTemplate()
    {
        using var stream = typeof(EditorCaptureReport)
            .Assembly
            .GetManifestResourceStream(TemplateResourceName)
            ?? throw new InvalidConfigurationException(
                $"Embedded report template was not found: " +
                $"{TemplateResourceName}");
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string CreatePngDataUri(string path)
    {
        return "data:image/png;base64," +
            Convert.ToBase64String(File.ReadAllBytes(path));
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static Version ParseVersion(string version)
    {
        return Version.TryParse(version, out var parsed)
            ? parsed
            : new Version();
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
    }

    private readonly record struct CaptureKey(
        string Surface,
        string VersionLine,
        string TestName);
}

public static class EditorCaptureReportHooks
{
    [After(HookType.TestSession)]
    public static void Generate(TestSessionContext context)
    {
        EditorCaptureReport.GenerateAndAttach(context);
    }
}
