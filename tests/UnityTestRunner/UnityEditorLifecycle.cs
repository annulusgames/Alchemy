using System.Diagnostics;

namespace Alchemy.UnityTestRunner;

internal sealed class UnityEditorLifecycle(UnityCli unityCli)
{
    private const string CloseCommand = "alchemy_inspector_capture_close";
    private static readonly TimeSpan ShutdownTimeout =
        TimeSpan.FromSeconds(30);

    internal async Task CloseRunningAsync(
        UnityProject project,
        string editorPath,
        Action<string> writeProgress,
        CancellationToken cancellationToken)
    {
        var connectedEditor = project.MajorVersion >= 6000
            ? await unityCli.FindConnectedEditorAsync(
                project,
                cancellationToken)
            : null;
        if (connectedEditor is not null)
        {
            writeProgress(
                $"Preflight: closing existing Editor " +
                $"{connectedEditor.ProcessId}...");
            await CloseProcessAsync(
                connectedEditor.ProcessId,
                writeProgress,
                cancellationToken);
            return;
        }

        var processId = FindEditorProcess(
            project,
            GetEditorExecutable(editorPath));
        if (processId is null)
        {
            return;
        }

        writeProgress($"Preflight: closing existing Editor {processId}...");
        await CloseProcessAsync(
            processId.Value,
            writeProgress,
            cancellationToken);
    }

    internal async Task CloseConnectedAsync(
        UnityProject project,
        int processId,
        bool force,
        Action<string> writeProgress,
        CancellationToken cancellationToken)
    {
        var close = UnityCli.Deserialize<EditorCloseStatus>(
            await unityCli.RunCommandAsync(
                project,
                CloseCommand,
                force ? ["--force", "true"] : [],
                cancellationToken));
        if (!close.Success ||
            !string.Equals(
                close.Status,
                "closing",
                StringComparison.Ordinal))
        {
            throw new UnityExecutionException(
                $"Unity {project.EditorVersion} refused to close: " +
                close.Message);
        }

        await WaitForExitOrTerminateAsync(
            processId,
            writeProgress,
            cancellationToken);
    }

    internal static string GetEditorExecutable(string editorPath)
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(editorPath, "Editor", "Unity.exe");
        }

        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(
                editorPath,
                "Unity.app",
                "Contents",
                "MacOS",
                "Unity");
        }

        return Path.Combine(editorPath, "Editor", "Unity");
    }

    internal static async Task CloseProcessAsync(
        int processId,
        Action<string> writeProgress,
        CancellationToken cancellationToken)
    {
        using var process = GetProcess(processId);
        if (process is null)
        {
            return;
        }

        if (!process.CloseMainWindow())
        {
            process.Kill(entireProcessTree: true);
        }

        await WaitForExitOrTerminateAsync(
            process,
            processId,
            writeProgress,
            cancellationToken);
    }

    private static async Task WaitForExitOrTerminateAsync(
        int processId,
        Action<string> writeProgress,
        CancellationToken cancellationToken)
    {
        using var process = GetProcess(processId);
        if (process is null)
        {
            writeProgress($"Preflight: Editor {processId} closed");
            return;
        }

        await WaitForExitOrTerminateAsync(
            process,
            processId,
            writeProgress,
            cancellationToken);
    }

    private static async Task WaitForExitOrTerminateAsync(
        Process process,
        int processId,
        Action<string> writeProgress,
        CancellationToken cancellationToken)
    {
        using var timeoutCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(ShutdownTimeout);
        try
        {
            await process.WaitForExitAsync(timeoutCancellation.Token);
            writeProgress($"Preflight: Editor {processId} closed");
        }
        catch (OperationCanceledException) when (
            !cancellationToken.IsCancellationRequested &&
            timeoutCancellation.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            writeProgress(
                $"Preflight: Editor {processId} did not close within " +
                $"{ShutdownTimeout} and was terminated");
        }
    }

    private static int? FindEditorProcess(
        UnityProject project,
        string executable)
    {
        var projectName = Path.GetFileName(
            Path.TrimEndingDirectorySeparator(project.ProjectPath));
        var titlePrefix = $"{projectName} -";
        foreach (var process in Process.GetProcessesByName("Unity"))
        {
            using (process)
            {
                if (process.MainWindowHandle == IntPtr.Zero ||
                    !process.MainWindowTitle.StartsWith(
                        titlePrefix,
                        StringComparison.Ordinal) ||
                    process.MainModule?.FileName is not { } processPath ||
                    !PathsEqual(executable, processPath))
                {
                    continue;
                }

                return process.Id;
            }
        }

        return null;
    }

    private static Process? GetProcess(int processId)
    {
        try
        {
            return Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
    }

    private sealed class EditorCloseStatus
    {
        public string Status { get; init; } = "";
        public bool Success { get; init; }
        public string Message { get; init; } = "";
    }
}
