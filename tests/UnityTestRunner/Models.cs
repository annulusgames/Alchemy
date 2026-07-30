using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

namespace Alchemy.UnityTestRunner;

public enum TestMode : byte
{
    EditMode,
    PlayMode,
}

public sealed record UnityProject(
    string ProjectPath,
    string EditorVersion,
    int MajorVersion,
    string VersionLine)
{
    public static UnityProject Locate(
        string relativePath,
        [CallerFilePath] string callerFilePath = "")
    {
        var sourceDirectory = Path.GetDirectoryName(callerFilePath);
        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            throw new InvalidConfigurationException(
                "Could not determine the Unity test source directory.");
        }

        var projectPath = Path.GetFullPath(
            Path.Combine(sourceDirectory, relativePath));
        if (!Directory.Exists(projectPath))
        {
            throw new InvalidConfigurationException(
                $"Unity project directory does not exist: {projectPath}");
        }

        var projectVersionPath = Path.Combine(
            projectPath,
            "ProjectSettings",
            "ProjectVersion.txt");
        if (!File.Exists(projectVersionPath))
        {
            throw new InvalidConfigurationException(
                $"Unity project has no ProjectSettings/ProjectVersion.txt: {projectPath}");
        }

        var editorVersion = ReadEditorVersion(projectVersionPath);
        var majorVersion = ParseMajorVersion(editorVersion);
        var projectName = Path.GetFileName(projectPath);
        const string prefix = "Unity";
        if (!projectName.StartsWith(prefix, StringComparison.Ordinal) ||
            projectName.Length == prefix.Length)
        {
            throw new InvalidConfigurationException(
                $"Unity version project must be named Unity<version>: {projectPath}");
        }

        return new UnityProject(
            projectPath,
            editorVersion,
            majorVersion,
            projectName[prefix.Length..]);
    }

    private static string ReadEditorVersion(string projectVersionPath)
    {
        var line = File.ReadLines(projectVersionPath)
            .FirstOrDefault(value =>
                value.StartsWith("m_EditorVersion:", StringComparison.Ordinal));
        var value = line?.Split(':', 2).ElementAtOrDefault(1)?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidConfigurationException(
                $"Could not read m_EditorVersion from '{projectVersionPath}'.");
        }

        return value;
    }

    private static int ParseMajorVersion(string editorVersion)
    {
        var value = editorVersion.Split('.', 2)[0];
        if (!int.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var majorVersion))
        {
            throw new InvalidConfigurationException(
                $"Invalid Unity editor version '{editorVersion}'.");
        }

        return majorVersion;
    }
}

public sealed record ProcessSpec(
    string FileName,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory = null,
    TimeSpan? Timeout = null,
    bool TerminateDescendantsOnExit = false);

public sealed record ProcessResult(
    int ProcessId,
    int ExitCode,
    string StandardOutput,
    string StandardError);

public sealed record NUnitRunSummary(
    int Total,
    int Passed,
    int Failed,
    int Inconclusive,
    int Skipped,
    int Warnings,
    int Asserts,
    decimal Duration)
{
    public bool HasFailures => Failed > 0 || Inconclusive > 0;

    public static NUnitRunSummary From(XElement root)
    {
        return new NUnitRunSummary(
            GetInt(root, "total"),
            GetInt(root, "passed"),
            GetInt(root, "failed"),
            GetInt(root, "inconclusive"),
            GetInt(root, "skipped"),
            GetInt(root, "warnings"),
            GetInt(root, "asserts"),
            GetDecimal(root, "duration"));
    }

    private static int GetInt(XElement element, string name)
    {
        return int.TryParse((string?)element.Attribute(name), out var value)
            ? value
            : 0;
    }

    private static decimal GetDecimal(XElement element, string name)
    {
        return decimal.TryParse(
            (string?)element.Attribute(name),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : 0m;
    }
}

public sealed record NUnitReport(XDocument Document, NUnitRunSummary Summary)
{
    public static NUnitReport Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new ReportException(
                $"Unity did not produce the expected NUnit report: {path}");
        }

        try
        {
            var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
            if (document.Root is null || document.Root.Name != "test-run")
            {
                throw new ReportException(
                    $"The Unity report is not an NUnit test-run document: {path}");
            }

            return new NUnitReport(
                document,
                NUnitRunSummary.From(document.Root));
        }
        catch (XmlException exception)
        {
            throw new ReportException(
                $"The Unity NUnit report is malformed: {path}",
                exception);
        }
    }
}
