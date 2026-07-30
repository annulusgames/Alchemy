using System.Collections.Concurrent;
using System.Globalization;
using TUnit.Core;

namespace Alchemy.UnityTestRunner;

internal static class UnityTest
{
    private static readonly IProcessRunner ProcessRunner = new ProcessRunner();
    private static readonly UnityCli UnityCli = new(ProcessRunner);
    private static readonly UnityEditorLifecycle EditorLifecycle =
        new(UnityCli);
    private static readonly TextWriter LiveOutput = TextWriter.Synchronized(
        new StreamWriter(Console.OpenStandardOutput())
        {
            AutoFlush = true,
        });
    private static readonly ConcurrentDictionary<string, UnityRunContext> Runs =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ModeGates =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan RunTimeout =
        TimeSpan.FromMinutes(15);

    public static async Task RefreshAsync(
        UnityProject project,
        CancellationToken cancellationToken)
    {
        WriteProgress(project, "Preflight: checking unity and the installed editor...");
        var editorPath = await UnityCli.ResolveEditorPathAsync(
            project,
            cancellationToken);
        var executable = UnityEditorLifecycle.GetEditorExecutable(editorPath);
        if (!File.Exists(executable))
        {
            throw new UnityUnavailableException(
                $"The installed Unity editor executable does not exist: {executable}");
        }

        await EditorLifecycle.CloseRunningAsync(
            project,
            editorPath,
            message => WriteProgress(project, message),
            cancellationToken);

        var logDirectory = CreateLogDirectory(project);
        var refreshLogPath = Path.Combine(logDirectory, "Refresh.log");
        WriteProgress(project, "Library warmup: running...");
        ProcessResult result;
        try
        {
            result = await ProcessRunner.RunAsync(
                new ProcessSpec(
                    executable,
                    BuildLibraryWarmupArguments(project, refreshLogPath),
                    project.ProjectPath,
                    TerminateDescendantsOnExit: true),
                cancellationToken);
        }
        catch (ProcessExecutionException exception)
        {
            throw new UnityExecutionException(
                $"Could not start Unity {project.EditorVersion} for Library warmup.",
                exception);
        }
        finally
        {
            WriteLogDiagnostics(project, [refreshLogPath]);
        }

        if (result.ExitCode != 0)
        {
            throw new UnityExecutionException(
                $"Unity {project.EditorVersion} Library warmup exited with code " +
                $"{result.ExitCode}. See {refreshLogPath} for details.");
        }

        var context = new UnityRunContext(
            editorPath,
            logDirectory,
            refreshLogPath);
        if (!Runs.TryAdd(project.ProjectPath, context))
        {
            throw new InvalidConfigurationException(
                $"Unity project was initialized more than once in this test session: " +
                project.ProjectPath);
        }

        WriteProgress(project, "Library warmup: completed");
    }

    public static async Task RunAsync(
        UnityProject project,
        TestMode mode,
        CancellationToken cancellationToken)
    {
        if (!Runs.TryGetValue(project.ProjectPath, out var context))
        {
            throw new InvalidConfigurationException(
                $"Unity project was not initialized before {mode}: {project.ProjectPath}");
        }

        var reportPath = Path.Combine(context.LogDirectory, $"{mode}.xml");
        var editorLogPath = Path.Combine(context.LogDirectory, $"{mode}.log");
        var cliLogPath = project.MajorVersion >= 6000
            ? string.Empty
            : Path.Combine(context.LogDirectory, $"{mode}.cli.log");

        WriteProgress(project, $"{mode}: running...");

        try
        {
            if (project.MajorVersion >= 6000)
            {
                await RunUnityCliModeAsync(
                    project,
                    context,
                    mode,
                    reportPath,
                    editorLogPath,
                    cancellationToken);
                ValidateReport(project, mode, reportPath);
                return;
            }

            var result = await RunUnityAsync(
                project,
                context.EditorPath,
                mode,
                reportPath,
                editorLogPath,
                cancellationToken);
            ValidateReport(project, mode, reportPath);
            if (result.ExitCode != 0)
            {
                throw new UnityExecutionException(
                    $"Unity {project.EditorVersion} {mode} exited with code " +
                    $"{result.ExitCode} despite producing a passing report." +
                    FormatProcessDiagnostic(result));
            }
        }
        finally
        {
            var artifacts = new[]
            {
                context.RefreshLogPath,
                editorLogPath,
                cliLogPath,
                reportPath,
            };
            WriteLogDiagnostics(project, [editorLogPath, cliLogPath]);
            AttachArtifacts(artifacts);
        }
    }

    private static async Task RunUnityCliModeAsync(
        UnityProject project,
        UnityRunContext context,
        TestMode mode,
        string reportPath,
        string editorLogPath,
        CancellationToken cancellationToken)
    {
        var gate = ModeGates.GetOrAdd(
            project.ProjectPath,
            _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        ConnectedUnityEditor? editor = null;
        try
        {
            var editorArguments = BuildEditorTestArguments(
                mode,
                reportPath,
                editorLogPath);
            WriteProgress(
                project,
                $"{mode}: opening one Unity Editor...");
            editor = await UnityCli.OpenEditorWithArgumentsAsync(
                project,
                editorArguments,
                cancellationToken,
                waitForPipeline: false,
                editorExecutable: UnityEditorLifecycle.GetEditorExecutable(
                    context.EditorPath));
            WriteProgress(
                project,
                $"{mode}: Editor {editor.ProcessId} is running");

            var deadline = DateTimeOffset.UtcNow + RunTimeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var connectedEditor = await UnityCli.FindConnectedEditorAsync(
                    project,
                    cancellationToken);
                if (connectedEditor is null && File.Exists(reportPath))
                {
                    WriteProgress(
                        project,
                        $"{mode}: Editor completed the selected mode");
                    return;
                }

                await Task.Delay(
                    TimeSpan.FromMilliseconds(500),
                    cancellationToken);
            }

            throw new UnityExecutionException(
                $"Unity {project.EditorVersion} {mode} did not complete " +
                $"within {RunTimeout}. See {editorLogPath}.");
        }
        finally
        {
            var processId = editor?.ProcessId > 0
                ? editor.ProcessId
                : UnityEditorLifecycle.FindRunningEditorProcessId(
                    UnityEditorLifecycle.GetEditorExecutable(
                        context.EditorPath));
            if (processId is not null)
            {
                await UnityEditorLifecycle.CloseProcessAsync(
                    processId.Value,
                    message => WriteProgress(project, message),
                    CancellationToken.None);
            }

            gate.Release();
        }
    }

    private static string BuildEditorTestArguments(
        TestMode mode,
        string reportPath,
        string editorLogPath)
    {
        var command = mode == TestMode.EditMode
            ? "Alchemy.Tests.TestCommands.RunAllEditModeTests"
            : "Alchemy.Tests.TestCommands.RunAllPlayModeTests";
        return
            $"-batchmode -nographics -automated -projectPath . " +
            $"-executeMethod {command} " +
            $"-testResults \"{reportPath}\" --auto-quit " +
            $"-logFile \"{editorLogPath}\"";
    }

    private static void ValidateReport(
        UnityProject project,
        TestMode mode,
        string reportPath)
    {
        if (!File.Exists(reportPath))
        {
            throw new UnityExecutionException(
                $"Unity did not produce the {mode} NUnit report: {reportPath}.");
        }

        var report = NUnitReport.Load(reportPath);
        WriteModeSummary(project, mode, report.Summary);
        if (report.Summary.Total == 0)
        {
            throw new UnityExecutionException(
                $"Unity {project.EditorVersion} discovered no {mode} tests. " +
                $"See {reportPath}.");
        }

        if (report.Summary.HasFailures)
        {
            throw new UnityExecutionException(
                $"Unity {project.EditorVersion} {mode} tests failed: " +
                FormatSummary(report.Summary) +
                $". See {reportPath}.");
        }
    }

    private static async Task<ProcessResult> RunUnityAsync(
        UnityProject project,
        string editorPath,
        TestMode mode,
        string reportPath,
        string editorLogPath,
        CancellationToken cancellationToken)
    {
        var fileName = UnityEditorLifecycle.GetEditorExecutable(editorPath);
        if (!File.Exists(fileName))
        {
            throw new UnityUnavailableException(
                $"The installed Unity editor executable does not exist: {fileName}");
        }

        var arguments = BuildBatchModeArguments(
            project,
            mode,
            reportPath,
            editorLogPath);
        var workingDirectory = project.ProjectPath;

        try
        {
            var result = await ProcessRunner.RunAsync(
                new ProcessSpec(
                    fileName,
                    arguments,
                    workingDirectory,
                    TerminateDescendantsOnExit: true),
                cancellationToken);
            return result;
        }
        catch (ProcessExecutionException exception)
        {
            throw new UnityExecutionException(
                $"Could not start Unity {project.EditorVersion} for {mode}.",
                exception);
        }
    }

    private static IReadOnlyList<string> BuildLibraryWarmupArguments(
        UnityProject project,
        string logPath)
    {
        return
        [
            "-batchmode",
            "-nographics",
            "-projectPath",
            ".",
            "-executeMethod",
            "Alchemy.Tests.TestCommands.Refresh",
            "--auto-quit",
            "-logFile",
            logPath,
        ];
    }

    private static IReadOnlyList<string> BuildBatchModeArguments(
        UnityProject project,
        TestMode mode,
        string reportPath,
        string logPath)
    {
        var command = mode == TestMode.EditMode
            ? "Alchemy.Tests.TestCommands.RunAllEditModeTests"
            : "Alchemy.Tests.TestCommands.RunAllPlayModeTests";
        return
        [
            "-batchmode",
            "-nographics",
            "-projectPath",
            ".",
            "-executeMethod",
            command,
            "-testResults",
            reportPath,
            "--auto-quit",
            "-logFile",
            logPath,
        ];
    }

    private static string CreateLogDirectory(UnityProject project)
    {
        var runId =
            $"{DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}-" +
            Guid.NewGuid().ToString("N")[..8];
        var directory = Path.Combine(
            project.ProjectPath,
            "Logs",
            "UnityTestRunner",
            runId);
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteModeSummary(
        UnityProject project,
        TestMode mode,
        NUnitRunSummary summary)
    {
        WriteProgress(
            project,
            $"{mode}: {summary.Passed}/{summary.Total} passed " +
            $"({summary.Failed} failed, {summary.Inconclusive} inconclusive, " +
            $"{summary.Skipped} skipped)");
    }

    private static string FormatSummary(NUnitRunSummary summary)
    {
        return $"{summary.Passed}/{summary.Total} passed, " +
               $"{summary.Failed} failed, " +
               $"{summary.Inconclusive} inconclusive, " +
               $"{summary.Skipped} skipped";
    }

    private static string FormatProcessDiagnostic(ProcessResult result)
    {
        var diagnostic = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : result.StandardError;
        return string.IsNullOrWhiteSpace(diagnostic)
            ? string.Empty
            : $"{Environment.NewLine}{diagnostic.Trim()}";
    }

    private static void WriteProgress(UnityProject project, string message)
    {
        LiveOutput.WriteLine($"[{project.EditorVersion}] {message}");
    }

    private static void WriteLogDiagnostics(
        UnityProject project,
        IEnumerable<string> paths)
    {
        var diagnostics = new List<string>();
        foreach (var path in paths
                     .Where(File.Exists)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(value => value, StringComparer.Ordinal))
        {
            var lineNumber = 0;
            using var stream = File.Open(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            while (reader.ReadLine() is { } line)
            {
                lineNumber++;
                if (IsWarningOrHigher(line))
                {
                    diagnostics.Add(
                        $"{Path.GetFileName(path)}:{lineNumber}: {line.Trim()}");
                }
            }
        }

        if (diagnostics.Count == 0)
        {
            return;
        }

        LiveOutput.WriteLine(
            $"[{project.EditorVersion}] Captured log entries " +
            $"({diagnostics.Count}):");
        foreach (var diagnostic in diagnostics)
        {
            LiveOutput.WriteLine(
                $"[{project.EditorVersion}]   {diagnostic}");
        }
    }

    private static bool IsWarningOrHigher(string line)
    {
        return line.Contains("WARNING:", StringComparison.OrdinalIgnoreCase) ||
               line.Contains(" warning ", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("ERROR:", StringComparison.OrdinalIgnoreCase) ||
               line.Contains(" error ", StringComparison.OrdinalIgnoreCase) ||
               line.StartsWith("Error ", StringComparison.OrdinalIgnoreCase) ||
               line.StartsWith("Warning ", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Exception:", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Unhandled exception", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Fatal error", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Assertion failed", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Crash!!!", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Failed to ", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Cannot ", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Could not ", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Debug:LogWarning", StringComparison.Ordinal) ||
               line.Contains("Debug:LogError", StringComparison.Ordinal);
    }

    private static void AttachArtifacts(IEnumerable<string> paths)
    {
        var testContext = TestContext.Current
            ?? throw new InvalidOperationException(
                "TUnit did not provide a test context.");
        foreach (var path in paths
                     .Where(File.Exists)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            testContext.Output.AttachArtifact(path);
        }
    }

    private sealed record UnityRunContext(
        string EditorPath,
        string LogDirectory,
        string RefreshLogPath);
}
