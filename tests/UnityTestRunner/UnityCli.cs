using System.Text.Json;

namespace Alchemy.UnityTestRunner;

public sealed class UnityCli
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IProcessRunner processRunner;

    public UnityCli(IProcessRunner processRunner)
    {
        this.processRunner = processRunner;
    }

    public async Task<string> ResolveEditorPathAsync(UnityProject project, CancellationToken cancellationToken)
    {
        ProcessResult cliVersion;
        try
        {
            cliVersion = await processRunner.RunAsync(
                new ProcessSpec("unity", ["--no-banner", "--version"]),
                cancellationToken);
        }
        catch (ProcessExecutionException exception)
        {
            throw new UnityUnavailableException(
                "The 'unity' command is required but was not found on PATH.",
                exception);
        }

        if (cliVersion.ExitCode != 0)
        {
            throw new UnityUnavailableException(
                $"The 'unity' command failed during preflight with exit code {cliVersion.ExitCode}: " +
                GetDiagnostic(cliVersion));
        }

        ProcessResult editorPathResult;
        try
        {
            editorPathResult = await processRunner.RunAsync(
                new ProcessSpec(
                    "unity",
                    [
                        "--no-banner",
                        "editors",
                        "path",
                        project.EditorVersion,
                        "--format",
                        "json"
                    ]),
                cancellationToken);
        }
        catch (ProcessExecutionException exception)
        {
            throw new UnityUnavailableException(
                $"Could not locate the installed Unity editor {project.EditorVersion}.",
                exception);
        }

        if (editorPathResult.ExitCode != 0)
        {
            throw new UnityUnavailableException(
                $"Unity editor {project.EditorVersion} is not installed: {GetDiagnostic(editorPathResult)}");
        }

        var editorPath = ReadEditorPath(editorPathResult.StandardOutput, project.EditorVersion);
        if (!Directory.Exists(editorPath))
        {
            throw new UnityUnavailableException(
                $"Unity reported an editor path that does not exist: {editorPath}");
        }

        return editorPath;
    }

    internal async Task<ConnectedUnityEditor?> FindConnectedEditorAsync(
        UnityProject project,
        CancellationToken cancellationToken)
    {
        var result = await RunCliAsync(
            [
                "--no-banner",
                "status",
                "--project-path",
                project.ProjectPath,
                "--format",
                "json",
            ],
            cancellationToken);
        if (result.ExitCode != 0)
        {
            if (HasErrorCode(
                    result.StandardOutput,
                    "STATUS_NO_INSTANCES") ||
                HasErrorCode(
                    result.StandardOutput,
                    "STATUS_ALL_UNREACHABLE"))
            {
                return null;
            }

            throw new UnityExecutionException(
                $"Could not query connected Unity Editors for " +
                $"{project.EditorVersion}: {GetDiagnostic(result)}");
        }

        using var document = JsonDocument.Parse(result.StandardOutput);
        var instances = document.RootElement
            .GetProperty("data")
            .GetProperty("instances");
        foreach (var instance in instances.EnumerateArray())
        {
            var projectPath = instance.GetProperty("project").GetString();
            if (!PathsEqual(project.ProjectPath, projectPath))
            {
                continue;
            }

            return new ConnectedUnityEditor(
                instance.GetProperty("pid").GetInt32(),
                instance.GetProperty("state").GetString() ?? "",
                instance.GetProperty("version").GetString() ?? "");
        }

        return null;
    }

    internal async Task<ConnectedUnityEditor> OpenEditorAsync(
        UnityProject project,
        string logPath,
        CancellationToken cancellationToken)
    {
        return await OpenEditorWithArgumentsAsync(
            project,
            $"-automated --auto-quit -logFile \"{logPath}\"",
            cancellationToken);
    }

    internal async Task<ConnectedUnityEditor> OpenEditorWithArgumentsAsync(
        UnityProject project,
        string editorArguments,
        CancellationToken cancellationToken)
    {
        using var launcher = processRunner.Start(
            new ProcessSpec(
                "unity",
                [
                    "--no-banner",
                    "--non-interactive",
                    "open",
                    project.ProjectPath,
                    "--editor-version",
                    project.EditorVersion,
                    "--args",
                    editorArguments,
                    "--format",
                    "json",
                ],
                project.ProjectPath));
        try
        {
            var deadline = DateTimeOffset.UtcNow + CommandTimeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var editor = await FindConnectedEditorAsync(
                    project,
                    cancellationToken);
                if (editor is not null)
                {
                    return editor;
                }

                if (launcher.HasExited && launcher.ExitCode != 0)
                {
                    throw new UnityExecutionException(
                        $"Could not open Unity {project.EditorVersion}; " +
                        $"the Unity CLI exited with code {launcher.ExitCode}.");
                }

                await Task.Delay(
                    TimeSpan.FromMilliseconds(500),
                    cancellationToken);
            }

            throw new UnityExecutionException(
                $"Unity {project.EditorVersion} did not start within " +
                $"{CommandTimeout}.");
        }
        finally
        {
            if (!launcher.HasExited)
            {
                launcher.Kill(entireProcessTree: false);
                await launcher.WaitForExitAsync(CancellationToken.None);
            }
        }
    }

    internal async Task<ConnectedUnityEditor> WaitUntilReadyAsync(
        UnityProject project,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var editor = await FindConnectedEditorAsync(
                project,
                cancellationToken);
            if (editor is not null &&
                string.Equals(
                    editor.State,
                    "ready",
                    StringComparison.OrdinalIgnoreCase))
            {
                return editor;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        throw new UnityExecutionException(
            $"Unity {project.EditorVersion} did not become ready within " +
            $"{timeout}.");
    }

    internal async Task WaitForCommandAsync(
        UnityProject project,
        string command,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await HasCommandAsync(
                    project,
                    command,
                    cancellationToken))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        throw new UnityExecutionException(
            $"Unity {project.EditorVersion} did not register Pipeline command " +
            $"'{command}' within {timeout}.");
    }

    internal async Task<JsonElement> RunCommandAsync(
        UnityProject project,
        string command,
        IReadOnlyList<string> commandArguments,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "--no-banner",
            "command",
            command,
            "--project-path",
            project.ProjectPath,
            "--timeout",
            ((int)CommandTimeout.TotalSeconds).ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            "--format",
            "json",
        };
        arguments.AddRange(commandArguments);

        var result = await RunCliAsync(arguments, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new UnityExecutionException(
                $"Unity Pipeline command '{command}' failed for " +
                $"{project.EditorVersion}: {GetDiagnostic(result)}");
        }

        using var document = JsonDocument.Parse(result.StandardOutput);
        var root = document.RootElement;
        if (!root.GetProperty("success").GetBoolean())
        {
            throw new UnityExecutionException(
                $"Unity Pipeline command '{command}' failed for " +
                $"{project.EditorVersion}: {GetErrors(root)}");
        }

        var data = root.GetProperty("data");
        if (!data.GetProperty("success").GetBoolean())
        {
            throw new UnityExecutionException(
                $"Unity Pipeline command '{command}' failed for " +
                $"{project.EditorVersion}.");
        }

        return data.GetProperty("result").Clone();
    }

    internal async Task<UnityConsoleSnapshot> ReadConsoleAsync(
        UnityProject project,
        string minimumLevel,
        long since,
        int tail,
        CancellationToken cancellationToken)
    {
        var result = await RunCommandAsync(
            project,
            "console",
            [
                "--level",
                minimumLevel,
                "--since",
                since.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                "--tail",
                tail.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
            ],
            cancellationToken);
        return Deserialize<UnityConsoleSnapshot>(result);
    }

    internal static T Deserialize<T>(JsonElement element)
    {
        var value = element.Deserialize<T>(JsonOptions);
        if (value is null)
        {
            throw new UnityExecutionException(
                $"Unity CLI returned an empty {typeof(T).Name}.");
        }

        return value;
    }

    private async Task<bool> HasCommandAsync(
        UnityProject project,
        string command,
        CancellationToken cancellationToken)
    {
        var result = await RunCliAsync(
            [
                "--no-banner",
                "list",
                "--project-path",
                project.ProjectPath,
                "--format",
                "json",
            ],
            cancellationToken);
        if (result.ExitCode != 0)
        {
            return false;
        }

        using var document = JsonDocument.Parse(result.StandardOutput);
        var tools = document.RootElement
            .GetProperty("data")
            .GetProperty("tools");
        return tools.EnumerateArray().Any(element =>
            string.Equals(
                element.GetProperty("name").GetString(),
                command,
                StringComparison.Ordinal));
    }

    private async Task<ProcessResult> RunCliAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            return await processRunner.RunAsync(
                new ProcessSpec(
                    "unity",
                    arguments,
                    Timeout: CommandTimeout),
                cancellationToken);
        }
        catch (ProcessStartException exception)
        {
            throw new UnityUnavailableException(
                "The 'unity' command is required but was not found on PATH.",
                exception);
        }
    }

    private static bool HasErrorCode(string output, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return false;
        }

        using var document = JsonDocument.Parse(output);
        if (!document.RootElement.TryGetProperty(
                "errors",
                out var errors))
        {
            return false;
        }

        return errors.EnumerateArray().Any(error =>
            string.Equals(
                error.GetProperty("code").GetString(),
                errorCode,
                StringComparison.Ordinal));
    }

    private static string GetErrors(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors))
        {
            return "No diagnostic was returned.";
        }

        return string.Join(
            Environment.NewLine,
            errors.EnumerateArray().Select(error =>
                error.TryGetProperty("message", out var message)
                    ? message.GetString()
                    : error.ToString()));
    }

    private static bool PathsEqual(string left, string? right)
    {
        if (string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
    }

    private static string ReadEditorPath(string output, string editorVersion)
    {
        try
        {
            using var document = JsonDocument.Parse(output);
            var path = document.RootElement
                .GetProperty("data")
                .GetProperty("path")
                .GetString();
            if (!string.IsNullOrWhiteSpace(path))
            {
                return path;
            }
        }
        catch (Exception exception) when (
            exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new UnityUnavailableException(
                $"The Unity CLI returned invalid editor path data for {editorVersion}.",
                exception);
        }

        throw new UnityUnavailableException(
            $"The Unity CLI did not return an editor path for {editorVersion}.");
    }

    private static string GetDiagnostic(ProcessResult result)
    {
        var diagnostic = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : result.StandardError;
        return diagnostic.Trim();
    }
}

internal sealed record ConnectedUnityEditor(
    int ProcessId,
    string State,
    string EditorVersion);

internal sealed class UnityConsoleSnapshot
{
    public List<UnityConsoleEntry> Entries { get; init; } = [];
    public long Cursor { get; init; }
}

internal sealed class UnityConsoleEntry
{
    public long Seq { get; init; }
    public string Level { get; init; } = "";
    public string Message { get; init; } = "";
    public string StackTrace { get; init; } = "";
}
