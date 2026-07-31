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
    private static readonly ConcurrentDictionary<
        CaptureKey,
        EditorCaptureRecord> Captures = new();
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
        string path,
        IReadOnlyList<EditorCaptureLogEntry> logs,
        int warningCount,
        int errorCount,
        int droppedLogCount)
    {
        Captures[new CaptureKey(surface, project.VersionLine, testName)] =
            new EditorCaptureRecord(
                Path.GetFullPath(path),
                logs.ToArray(),
                Math.Max(
                    warningCount,
                    logs.Count(entry =>
                        NormalizeLogKind(entry.Kind) == "warning")),
                Math.Max(
                    errorCount,
                    logs.Count(entry =>
                        NormalizeLogKind(entry.Kind) == "error")),
                Math.Max(0, droppedLogCount));
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
        IReadOnlyDictionary<CaptureKey, EditorCaptureRecord> captures)
    {
        var capturedCount = captures.Count(pair =>
            File.Exists(pair.Value.ImagePath));
        var testCaseCount = testNamesBySurface.Sum(pair => pair.Value.Length);
        var totalCount = versions.Count * testCaseCount;
        var cleanCount = testNamesBySurface.Sum(surface =>
            surface.Value.Sum(testName =>
                versions.Count(version =>
                    captures.TryGetValue(
                        new CaptureKey(surface.Key, version, testName),
                        out var capture) &&
                    File.Exists(capture.ImagePath) &&
                    capture.WarningCount == 0 &&
                    capture.ErrorCount == 0)));
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
                "{{CLEAN_COUNT}}",
                cleanCount.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal)
            .Replace(
                "{{CLEAN_STATUS}}",
                cleanCount == totalCount ? "complete" : "incomplete",
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
        IReadOnlyDictionary<CaptureKey, EditorCaptureRecord> captures)
    {
        var html = new StringBuilder(64 * 1024);
        var logTemplateIndex = 0;
        foreach (var (surface, testNames) in testNamesBySurface)
        {
            html.AppendLine("<section class=\"surface\">");
            html.Append("  <h2>")
                .Append(Encode(surface))
                .AppendLine("</h2>");
            html.Append("  <div class=\"matrix")
                .Append(versions.Count == 1 ? " single-version" : "")
                .AppendLine("\">");
            html.AppendLine(
                """
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
                var rowStatus = GetRowStatus(
                    surface,
                    versions,
                    testName,
                    captures);
                html.Append("        <tr")
                    .Append(
                        string.IsNullOrEmpty(rowStatus)
                            ? ""
                            : $" class=\"{rowStatus}\"")
                    .AppendLine(">");
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
                        captures,
                        ref logTemplateIndex);
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
        IReadOnlyDictionary<CaptureKey, EditorCaptureRecord> captures,
        ref int logTemplateIndex)
    {
        var key = new CaptureKey(surface, version, testName);
        var label = $"{surface} {testName}, Unity {version}";
        html.AppendLine("          <td>");
        if (captures.TryGetValue(key, out var capture) &&
            File.Exists(capture.ImagePath))
        {
            var issueClass = capture.ErrorCount > 0
                ? " has-errors"
                : capture.WarningCount > 0
                    ? " has-warnings"
                    : "";
            var logTemplateId =
                $"capture-logs-{logTemplateIndex++}";
            var logCount = capture.Logs.Count;
            var diagnostic =
                $"{capture.WarningCount} warnings, " +
                $"{capture.ErrorCount} errors";
            html.Append("            <button type=\"button\" class=\"capture")
                .Append(issueClass)
                .Append("\"")
                .Append(" aria-label=\"")
                .Append(Encode($"{label}, captured, {diagnostic}"))
                .Append("\" aria-pressed=\"false\" data-surface=\"")
                .Append(Encode(surface))
                .Append("\" data-test=\"")
                .Append(Encode(testName))
                .Append("\" data-version=\"")
                .Append(Encode(version))
                .Append("\" data-log-template=\"")
                .Append(logTemplateId)
                .Append("\" data-log-count=\"")
                .Append(logCount.ToString(CultureInfo.InvariantCulture))
                .Append("\" data-warning-count=\"")
                .Append(capture.WarningCount.ToString(
                    CultureInfo.InvariantCulture))
                .Append("\" data-error-count=\"")
                .Append(capture.ErrorCount.ToString(
                    CultureInfo.InvariantCulture))
                .Append("\" data-dropped-log-count=\"")
                .Append(capture.DroppedLogCount.ToString(
                    CultureInfo.InvariantCulture))
                .AppendLine("\">");
            html.Append("              <img src=\"")
                .Append(CreatePngDataUri(capture.ImagePath))
                .Append("\" alt=\"")
                .Append(Encode($"{label}, captured"))
                .AppendLine("\" loading=\"lazy\">");
            if (!string.IsNullOrEmpty(issueClass))
            {
                AppendIssueCounts(
                    html,
                    capture.WarningCount,
                    capture.ErrorCount,
                    "              ");
            }

            html.AppendLine("            </button>");
            AppendLogTemplate(
                html,
                logTemplateId,
                capture,
                "            ");
        }
        else
        {
            html.Append("            <div class=\"missing\" aria-label=\"")
                .Append(Encode($"{label}, missing"))
                .AppendLine("\">Not captured</div>");
        }

        html.AppendLine("          </td>");
    }

    private static void AppendIssueCounts(
        StringBuilder html,
        int warningCount,
        int errorCount,
        string indent)
    {
        html.Append(indent)
            .AppendLine("<span class=\"issue-counts\" aria-hidden=\"true\">");
        if (warningCount > 0)
        {
            html.Append(indent)
                .Append("  <span class=\"issue-count warning\">W ")
                .Append(warningCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("</span>");
        }

        if (errorCount > 0)
        {
            html.Append(indent)
                .Append("  <span class=\"issue-count error\">E ")
                .Append(errorCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("</span>");
        }

        html.Append(indent)
            .AppendLine("</span>");
    }

    private static string GetRowStatus(
        string surface,
        IReadOnlyList<string> versions,
        string testName,
        IReadOnlyDictionary<CaptureKey, EditorCaptureRecord> captures)
    {
        var hasWarnings = false;
        foreach (var version in versions)
        {
            if (!captures.TryGetValue(
                    new CaptureKey(surface, version, testName),
                    out var capture))
            {
                continue;
            }

            if (capture.ErrorCount > 0)
            {
                return "has-errors";
            }

            hasWarnings |= capture.WarningCount > 0;
        }

        return hasWarnings ? "has-warnings" : "";
    }

    private static void AppendLogTemplate(
        StringBuilder html,
        string id,
        EditorCaptureRecord capture,
        string indent)
    {
        html.Append(indent)
            .Append("<template id=\"")
            .Append(id)
            .AppendLine("\">");
        html.Append(indent)
            .AppendLine("  <div class=\"log-summary\">");
        html.Append(indent)
            .Append("    <span>")
            .Append(capture.Logs.Count.ToString(CultureInfo.InvariantCulture))
            .AppendLine(" logs</span>");
        if (capture.WarningCount > 0)
        {
            html.Append(indent)
                .Append("    <span class=\"warning\">")
                .Append(capture.WarningCount.ToString(
                    CultureInfo.InvariantCulture))
                .AppendLine(" warnings</span>");
        }

        if (capture.ErrorCount > 0)
        {
            html.Append(indent)
                .Append("    <span class=\"error\">")
                .Append(capture.ErrorCount.ToString(
                    CultureInfo.InvariantCulture))
                .AppendLine(" errors</span>");
        }

        if (capture.DroppedLogCount > 0)
        {
            html.Append(indent)
                .Append("    <span class=\"dropped\">")
                .Append(capture.DroppedLogCount.ToString(
                    CultureInfo.InvariantCulture))
                .AppendLine(" omitted</span>");
        }

        html.Append(indent)
            .AppendLine("  </div>");
        if (capture.Logs.Count == 0)
        {
            html.Append(indent)
                .AppendLine(
                    "  <p class=\"log-empty\">No Inspector logs captured.</p>");
        }
        else
        {
            html.Append(indent)
                .AppendLine("  <ol class=\"log-list\">");
            foreach (var entry in capture.Logs)
            {
                var kind = NormalizeLogKind(entry.Kind);
                html.Append(indent)
                    .Append("    <li class=\"log-entry ")
                    .Append(kind)
                    .AppendLine("\">");
                html.Append(indent)
                    .Append("      <span class=\"log-kind\">")
                    .Append(Encode(GetLogKindLabel(kind)))
                    .AppendLine("</span>");
                html.Append(indent)
                    .Append("      <pre class=\"log-message\">")
                    .Append(Encode(entry.Message))
                    .AppendLine("</pre>");
                if (!string.IsNullOrWhiteSpace(entry.StackTrace))
                {
                    html.Append(indent)
                        .AppendLine("      <details>");
                    html.Append(indent)
                        .AppendLine("        <summary>Stack trace</summary>");
                    html.Append(indent)
                        .Append("        <pre class=\"log-stack\">")
                        .Append(Encode(entry.StackTrace))
                        .AppendLine("</pre>");
                    html.Append(indent)
                        .AppendLine("      </details>");
                }

                html.Append(indent)
                    .AppendLine("    </li>");
            }

            html.Append(indent)
                .AppendLine("  </ol>");
        }

        if (capture.DroppedLogCount > 0)
        {
            html.Append(indent)
                .Append("  <p class=\"log-dropped\">")
                .Append(capture.DroppedLogCount.ToString(
                    CultureInfo.InvariantCulture))
                .AppendLine(" additional logs were omitted.</p>");
        }

        html.Append(indent)
            .AppendLine("</template>");
    }

    private static string NormalizeLogKind(string kind)
    {
        return kind.ToLowerInvariant() switch
        {
            "warning" => "warning",
            "error" => "error",
            _ => "info",
        };
    }

    private static string GetLogKindLabel(string kind)
    {
        return kind switch
        {
            "warning" => "Warning",
            "error" => "Error",
            _ => "Info",
        };
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

    private sealed record EditorCaptureRecord(
        string ImagePath,
        IReadOnlyList<EditorCaptureLogEntry> Logs,
        int WarningCount,
        int ErrorCount,
        int DroppedLogCount);
}

internal sealed record EditorCaptureLogEntry(
    string Kind,
    string Message,
    string StackTrace);

public static class EditorCaptureReportHooks
{
    [After(HookType.TestSession)]
    public static void Generate(TestSessionContext context)
    {
        EditorCaptureReport.GenerateAndAttach(context);
    }
}
