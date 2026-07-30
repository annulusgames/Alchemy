using System.Collections.Concurrent;
using System.Globalization;
using TUnit.Core;

namespace Alchemy.UnityTestRunner;

internal static class UnityEditorCaptureTest
{
    private const string Surface = "Inspector";
    private const string OpenCommand =
        "alchemy_editor_capture_inspector_open";
    private const string StartCommand =
        "alchemy_editor_capture_inspector_start";
    private const string StatusCommand = "alchemy_editor_capture_status";
    private const string CloseCommand =
        "alchemy_editor_capture_inspector_close";
    private const int CaptureWidth = 640;
    private const int CaptureHeight = 900;

    private static readonly TimeSpan EditorStartupTimeout =
        TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CaptureTimeout =
        TimeSpan.FromMinutes(1);
    private static readonly IProcessRunner ProcessRunner = new ProcessRunner();
    private static readonly UnityCli UnityCli = new(ProcessRunner);
    private static readonly UnityEditorLifecycle EditorLifecycle =
        new(UnityCli);
    private static readonly ConcurrentDictionary<string, InspectorRunContext>
        Runs = new(StringComparer.OrdinalIgnoreCase);

    public static async Task StartAsync(
        UnityProject project,
        CancellationToken cancellationToken)
    {
        if (project.MajorVersion < 6000)
        {
            throw new InvalidConfigurationException(
                $"Inspector capture requires Unity 6000 or later: " +
                $"{project.EditorVersion}");
        }

        WriteProgress(project, "Inspector capture: checking Unity and Pipeline...");
        var editorPath = await UnityCli.ResolveEditorPathAsync(
            project,
            cancellationToken);

        var captureDirectory = EditorCaptureReport.RegisterSurface(
            project,
            Surface,
            DiscoverInspectorTestNames(project));
        var editorLogPath = Path.Combine(
            captureDirectory,
            "EditorCapture.Editor.log");

        await EditorLifecycle.CloseRunningAsync(
            project,
            editorPath,
            message => WriteProgress(project, message),
            cancellationToken);

        WriteProgress(project, "Inspector capture: opening automated Editor...");
        ConnectedUnityEditor? editor = null;
        UnityConsoleSnapshot console;
        try
        {
            editor = await UnityCli.OpenEditorAsync(
                project,
                editorLogPath,
                cancellationToken);
            editor = await UnityCli.WaitUntilReadyAsync(
                project,
                EditorStartupTimeout,
                cancellationToken);
            await UnityCli.WaitForCommandAsync(
                project,
                OpenCommand,
                EditorStartupTimeout,
                cancellationToken);
            console = await UnityCli.ReadConsoleAsync(
                project,
                "log",
                int.MaxValue,
                1,
                cancellationToken);
            await UnityCli.FocusEditorAsync(project, cancellationToken);
            var open = UnityCli.Deserialize<EditorCaptureStatus>(
                await UnityCli.RunCommandAsync(
                    project,
                    OpenCommand,
                    [
                        "--width",
                        CaptureWidth.ToString(CultureInfo.InvariantCulture),
                        "--height",
                        CaptureHeight.ToString(CultureInfo.InvariantCulture),
                    ],
                    cancellationToken));
            if (!open.Success ||
                !string.Equals(
                    open.Status,
                    "ready",
                    StringComparison.Ordinal))
            {
                throw new UnityExecutionException(
                    $"Unity {project.EditorVersion} could not open the " +
                    $"Inspector capture session: {open.Message}");
            }
        }
        catch (Exception startupException)
        {
            if (editor is null)
            {
                throw;
            }

            try
            {
                await UnityEditorLifecycle.CloseProcessAsync(
                    editor.ProcessId,
                    message => WriteProgress(project, message),
                    CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "Inspector Editor startup and cleanup both failed.",
                    startupException,
                    cleanupException);
            }

            throw;
        }

        var context = new InspectorRunContext(
            captureDirectory,
            editor.ProcessId,
            console.Cursor);
        if (!Runs.TryAdd(project.ProjectPath, context))
        {
            throw new InvalidConfigurationException(
                $"Inspector capture was initialized more than once for " +
                $"{project.ProjectPath}.");
        }

        WriteProgress(
            project,
            $"Inspector capture: Editor {editor.ProcessId} ready");
    }

    public static async Task CaptureAsync(
        UnityProject project,
        string testName,
        CancellationToken cancellationToken)
    {
        if (!Runs.TryGetValue(project.ProjectPath, out var context))
        {
            throw new InvalidConfigurationException(
                $"Inspector capture was not initialized: {project.ProjectPath}");
        }

        var outputPath = Path.Combine(
            context.CaptureDirectory,
            $"{testName}.png");
        var prefabPath =
            $"Packages/com.annulusgames.alchemy.editor-ui-test/{testName}.prefab";
        WriteProgress(project, $"Inspector {testName}: capturing...");

        try
        {
            await UnityCli.FocusEditorAsync(project, cancellationToken);
            var start = UnityCli.Deserialize<EditorCaptureStatus>(
                await UnityCli.RunCommandAsync(
                    project,
                    StartCommand,
                    [
                        "--prefab",
                        prefabPath,
                        "--output",
                        outputPath,
                    ],
                    cancellationToken));
            if (!start.Success ||
                !string.Equals(
                    start.Status,
                    "running",
                    StringComparison.Ordinal))
            {
                throw new UnityExecutionException(
                    $"Unity {project.EditorVersion} could not start Inspector " +
                    $"{testName}: {start.Message}");
            }

            var completed = await WaitForCaptureAsync(
                project,
                start.JobId,
                cancellationToken);
            if (!completed.Success ||
                !string.Equals(
                    completed.Status,
                    "completed",
                    StringComparison.Ordinal))
            {
                throw new UnityExecutionException(
                    $"Unity {project.EditorVersion} Inspector {testName} " +
                    $"capture failed: {completed.Message}");
            }

            EnsureCaptureExists(outputPath);
            EditorCaptureReport.RecordCapture(
                project,
                Surface,
                testName,
                outputPath);
            WriteProgress(
                project,
                $"Inspector {testName}: captured {CaptureWidth}x{CaptureHeight}");
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                var testContext = TestContext.Current
                    ?? throw new InvalidOperationException(
                        "TUnit did not provide a test context.");
                testContext.Output.AttachArtifact(
                    outputPath,
                    displayName:
                        $"{project.EditorVersion} {testName} Inspector",
                    description:
                        $"{CaptureWidth}x{CaptureHeight} Inspector capture");
            }
        }
    }

    public static string[] DiscoverInspectorTestNames(UnityProject project)
    {
        var packageDirectory = Path.GetFullPath(
            Path.Combine(
                project.ProjectPath,
                "..",
                "..",
                "Alchemy.Tests",
                "Assets",
                "Alchemy.Tests.EditorUI"));
        if (!Directory.Exists(packageDirectory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(
                packageDirectory,
                "*.prefab",
                SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    public static async Task StopAsync(
        UnityProject project,
        CancellationToken cancellationToken)
    {
        if (!Runs.TryRemove(project.ProjectPath, out var context))
        {
            return;
        }

        var close = UnityCli.Deserialize<EditorCaptureStatus>(
            await UnityCli.RunCommandAsync(
                project,
                CloseCommand,
                [],
                cancellationToken));
        if (!close.Success ||
            !string.Equals(
                close.Status,
                "closed",
                StringComparison.Ordinal))
        {
            throw new UnityExecutionException(
                $"Unity {project.EditorVersion} could not close the " +
                $"Inspector capture session: {close.Message}");
        }

        var console = await UnityCli.ReadConsoleAsync(
            project,
            "warn",
            context.ConsoleCursor,
            1000,
            cancellationToken);
        WriteConsoleDiagnostics(project, console.Entries);

        WriteProgress(
            project,
            $"Inspector capture: closing Editor {context.ProcessId}...");
        await EditorLifecycle.CloseConnectedAsync(
            project,
            context.ProcessId,
            force: false,
            message => WriteProgress(project, message),
            cancellationToken);
    }

    private static async Task<EditorCaptureStatus> WaitForCaptureAsync(
        UnityProject project,
        string jobId,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + CaptureTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var status = UnityCli.Deserialize<EditorCaptureStatus>(
                await UnityCli.RunCommandAsync(
                    project,
                    StatusCommand,
                    ["--job_id", jobId],
                    cancellationToken));
            if (!string.Equals(
                    status.Status,
                    "running",
                    StringComparison.Ordinal))
            {
                return status;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
        }

        throw new UnityExecutionException(
            $"Unity {project.EditorVersion} Inspector capture {jobId} " +
            $"did not complete within {CaptureTimeout}.");
    }

    private static void EnsureCaptureExists(string path)
    {
        if (!File.Exists(path))
        {
            throw new UnityExecutionException(
                $"Unity did not produce the expected Inspector capture: {path}");
        }
    }

    private static void WriteProgress(
        UnityProject project,
        string message)
    {
        Console.Out.WriteLine($"[{project.VersionLine}] {message}");
    }

    private static void WriteConsoleDiagnostics(
        UnityProject project,
        IReadOnlyList<UnityConsoleEntry> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        Console.Out.WriteLine(
            $"[{project.VersionLine}] Captured log entries " +
            $"({entries.Count}):");
        foreach (var entry in entries)
        {
            Console.Out.WriteLine(
                $"[{project.VersionLine}]   [{entry.Level}] " +
                entry.Message.Trim());
        }
    }

    private sealed record InspectorRunContext(
        string CaptureDirectory,
        int ProcessId,
        long ConsoleCursor);

    private sealed class EditorCaptureStatus
    {
        public string JobId { get; init; } = "";
        public string Status { get; init; } = "";
        public bool Success { get; init; }
        public string Message { get; init; } = "";
        public string Path { get; init; } = "";
        public int Width { get; init; }
        public int Height { get; init; }
        public int Bytes { get; init; }
    }
}
