using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Alchemy.Docs;

/// <summary>
/// Drives Unity Inspector captures for documentation samples on Unity6000.3
/// using the same Pipeline commands as UnityTestRunner.
/// </summary>
internal sealed class UnityCapture
{
    const string PrefabCommand = "alchemy_editor_ui_generate_documentation_prefabs";
    const string OpenCommand = "alchemy_editor_capture_inspector_open";
    const string StartCommand = "alchemy_editor_capture_inspector_start";
    const string StatusCommand = "alchemy_editor_capture_status";
    const string CloseInspectorCommand = "alchemy_editor_capture_inspector_close";
    const string CloseEditorCommand = "alchemy_editor_capture_close";
    const int CaptureWidth = 640;
    const int CaptureHeight = 900;

    readonly RepoPaths paths;
    readonly string projectPath;
    readonly string editorVersion;

    public UnityCapture(RepoPaths paths)
    {
        this.paths = paths;
        projectPath = Path.GetFullPath(paths.UnityProject);
        if (!Directory.Exists(projectPath))
        {
            throw new InvalidOperationException(
                $"Unity capture project not found: {projectPath}");
        }

        editorVersion = ReadEditorVersion(projectPath);
    }

    public async Task CaptureAsync(
        IReadOnlyList<AttributeInfo> attributes,
        IReadOnlyDictionary<string, SampleInfo> samples,
        CancellationToken cancellationToken)
    {
        EnsureUnityCli();
        var targets = attributes
            .Where(a =>
                samples.TryGetValue(a.SampleTypeName, out var sample) &&
                sample.Capture)
            .ToArray();
        if (targets.Length == 0)
        {
            Console.Error.WriteLine("warning: no documentation samples to capture.");
            return;
        }

        Console.WriteLine(
            $"Capturing {targets.Length} samples " +
            $"(skipped {attributes.Count(a => samples.TryGetValue(a.SampleTypeName, out var s) && !s.Capture)} with Capture=false).");

        var tempDir = Directory.CreateTempSubdirectory("alchemy-docs-capture-").FullName;
        var editorLog = Path.Combine(tempDir, "Editor.log");
        Console.WriteLine($"Opening Unity {editorVersion} for documentation capture...");
        Console.WriteLine($"Editor log: {editorLog}");

        try
        {
            await CloseExistingEditorAsync(cancellationToken);
            await OpenEditorAsync(editorLog, cancellationToken);
            var editor = await WaitUntilReadyAsync(TimeSpan.FromMinutes(15), cancellationToken);
            await WaitForCommandAsync(OpenCommand, TimeSpan.FromMinutes(10), cancellationToken);

            RunPipeline(PrefabCommand, []);
            await UnityEditorForeground.ActivateAsync(editor.Pid, cancellationToken);
            var open = RunPipeline(
                OpenCommand,
                [
                    "--width", CaptureWidth.ToString(CultureInfo.InvariantCulture),
                    "--height", CaptureHeight.ToString(CultureInfo.InvariantCulture),
                ]);
            EnsureSuccess(open, "open Inspector capture session");
            await UnityEditorForeground.ActivateAsync(editor.Pid, cancellationToken);

            foreach (var attribute in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sample = attribute.SampleTypeName;
                var rawPath = Path.Combine(tempDir, sample + ".png");
                var prefab =
                    $"Packages/com.annulusgames.alchemy.editor-ui-test/{sample}.prefab";
                Console.WriteLine($"Capturing {sample}...");
                await UnityEditorForeground.ActivateAsync(editor.Pid, cancellationToken);
                var start = RunPipeline(
                    StartCommand,
                    ["--prefab", prefab, "--output", rawPath]);
                EnsureSuccess(start, $"start capture for {sample}");
                var jobId = GetString(start, "jobId")
                    ?? throw new InvalidOperationException("Capture returned no jobId.");
                var completed = await WaitForCaptureAsync(jobId, cancellationToken);
                EnsureSuccess(completed, $"complete capture for {sample}");
                if (!File.Exists(rawPath))
                {
                    throw new InvalidOperationException(
                        $"Capture did not produce '{rawPath}'.");
                }

                var output = Path.Combine(
                    paths.Images,
                    $"img-attribute-{attribute.Slug}.png");
                if (!ImageCrop.TryCropFile(rawPath, output, out var cropError))
                {
                    Console.Error.WriteLine(
                        $"warning: cyan crop failed for {sample}: {cropError}. Copying full capture.");
                    Directory.CreateDirectory(paths.Images);
                    await CopyWithRetryAsync(rawPath, output, cancellationToken);
                }
                else
                {
                    Console.WriteLine($"Wrote {Relative(output)}");
                }
            }

            TryRunPipeline(CloseInspectorCommand, []);
        }
        finally
        {
            TryRunPipeline(CloseEditorCommand, ["--force", "true"]);
            await ForceCloseProjectEditorsAsync(CancellationToken.None);
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // best effort
            }
        }
    }

    static void EnsureUnityCli()
    {
        var result = RunProcess("unity", ["--no-banner", "--version"]);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "The 'unity' command is required on PATH for documentation capture.");
        }
    }

    static async Task CopyWithRetryAsync(
        string source,
        string destination,
        CancellationToken cancellationToken)
    {
        const int attempts = 8;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                File.Copy(source, destination, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < attempts)
            {
                await Task.Delay(250 * attempt, cancellationToken);
            }
        }
    }

    async Task CloseExistingEditorAsync(CancellationToken cancellationToken)
    {
        var editor = await FindConnectedEditorAsync(cancellationToken);
        if (editor is null)
        {
            return;
        }

        Console.WriteLine($"Preflight: closing existing Editor {editor.Value.Pid}...");
        TryRunPipeline(CloseEditorCommand, ["--force", "true"]);
        await ForceCloseProjectEditorsAsync(cancellationToken);
    }

    async Task ForceCloseProjectEditorsAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(45);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var editor = await FindConnectedEditorAsync(cancellationToken);
            if (editor is null)
            {
                return;
            }

            try
            {
                using var process = Process.GetProcessById(editor.Value.Pid);
                if (!process.HasExited)
                {
                    if (!process.CloseMainWindow())
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
            }
            catch (ArgumentException)
            {
                // already exited
            }
            catch (InvalidOperationException)
            {
                // already exited
            }

            await Task.Delay(500, cancellationToken);
        }
    }

    async Task OpenEditorAsync(string logPath, CancellationToken cancellationToken)
    {
        // Match UnityTestRunner: do not redirect stdout/stderr on `unity open`.
        var psi = new ProcessStartInfo("unity")
        {
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            WorkingDirectory = projectPath,
        };
        foreach (var arg in new[]
                 {
                     "--no-banner",
                     "--non-interactive",
                     "open",
                     projectPath,
                     "--editor-version",
                     editorVersion,
                     "--args",
                     // Same flags as UnityTestRunner.UnityCli.OpenEditorAsync.
                     $"-automated --auto-quit -logFile \"{logPath}\"",
                     "--format",
                     "json",
                 })
        {
            psi.ArgumentList.Add(arg);
        }

        using var launcher = Process.Start(psi)
            ?? throw new InvalidOperationException("Could not start unity open.");
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(10);
        try
        {
            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await FindConnectedEditorAsync(cancellationToken) is not null)
                {
                    Console.WriteLine("Unity Pipeline instance registered.");
                    return;
                }

                if (launcher.HasExited && launcher.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"unity open exited with code {launcher.ExitCode}. See {logPath}");
                }

                await Task.Delay(500, cancellationToken);
            }

            throw new InvalidOperationException(
                $"Unity {editorVersion} did not register a Pipeline instance within 10 minutes. See {logPath}");
        }
        finally
        {
            if (!launcher.HasExited)
            {
                try
                {
                    launcher.Kill(entireProcessTree: false);
                }
                catch
                {
                    // best effort
                }
            }
        }
    }

    async Task<ConnectedEditor> WaitUntilReadyAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        string? lastState = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var editor = await FindConnectedEditorAsync(cancellationToken);
            if (editor is { State: var state })
            {
                if (!string.Equals(state, lastState, StringComparison.Ordinal))
                {
                    Console.WriteLine($"Unity editor state: {state} (pid {editor.Value.Pid})");
                    lastState = state;
                }

                if (string.Equals(state, "ready", StringComparison.OrdinalIgnoreCase))
                {
                    return editor.Value;
                }
            }
            else if (lastState is not null)
            {
                Console.WriteLine("Unity editor disconnected; waiting for reconnect...");
                lastState = null;
            }

            await Task.Delay(500, cancellationToken);
        }

        throw new InvalidOperationException(
            $"Unity {editorVersion} did not become ready within {timeout}. See editor log.");
    }

    async Task<ConnectedEditor?> FindConnectedEditorAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        var status = RunProcess(
            "unity",
            [
                "--no-banner", "status",
                "--project-path", projectPath,
                "--format", "json",
            ]);
        if (status.ExitCode != 0)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(status.StandardOutput);
            if (!document.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("instances", out var instances))
            {
                return null;
            }

            foreach (var instance in instances.EnumerateArray())
            {
                var project = instance.TryGetProperty("project", out var p)
                    ? p.GetString()
                    : null;
                if (!PathsEqual(projectPath, project))
                {
                    continue;
                }

                var state = instance.TryGetProperty("state", out var s)
                    ? s.GetString() ?? ""
                    : "";
                var pid = instance.TryGetProperty("pid", out var id)
                    ? id.GetInt32()
                    : 0;
                return new ConnectedEditor(pid, state);
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    async Task WaitForCommandAsync(
        string command,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Prefer `unity list` (current CLI). Fall back to `unity command` with no
            // command name, which also enumerates available Pipeline tools.
            var listed = RunProcess(
                "unity",
                [
                    "--no-banner", "list",
                    "--project-path", projectPath,
                    "--format", "json",
                ]);
            if (listed.ExitCode != 0)
            {
                listed = RunProcess(
                    "unity",
                    [
                        "--no-banner", "command",
                        "--project-path", projectPath,
                        "--format", "json",
                    ]);
            }

            if (listed.ExitCode == 0 &&
                listed.StandardOutput.Contains(command, StringComparison.Ordinal))
            {
                return;
            }

            await Task.Delay(500, cancellationToken);
        }

        throw new InvalidOperationException(
            $"Pipeline command '{command}' was not registered within {timeout}.");
    }

    async Task<JsonElement> WaitForCaptureAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var status = RunPipeline(StatusCommand, ["--job_id", jobId]);
            var state = GetString(status, "status");
            if (state is "completed" or "failed" or "canceled")
            {
                return status;
            }

            await Task.Delay(200, cancellationToken);
        }

        throw new InvalidOperationException(
            $"Capture job '{jobId}' timed out.");
    }

    JsonElement RunPipeline(string command, IReadOnlyList<string> commandArgs)
    {
        var args = new List<string>
        {
            "--no-banner", "command", command,
            "--project-path", projectPath,
            "--timeout", "60",
            "--format", "json",
        };
        args.AddRange(commandArgs);
        var result = RunProcess("unity", args);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"unity command '{command}' failed: {result.StandardError}\n{result.StandardOutput}");
        }

        using var document = JsonDocument.Parse(result.StandardOutput);
        var root = document.RootElement;
        if (!root.GetProperty("success").GetBoolean())
        {
            throw new InvalidOperationException(
                $"unity command '{command}' reported failure: {result.StandardOutput}");
        }

        var data = root.GetProperty("data");
        if (!data.GetProperty("success").GetBoolean())
        {
            throw new InvalidOperationException(
                $"Pipeline '{command}' failed: {result.StandardOutput}");
        }

        return data.GetProperty("result").Clone();
    }

    void TryRunPipeline(string command, IReadOnlyList<string> commandArgs)
    {
        try
        {
            RunPipeline(command, commandArgs);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"warning: pipeline '{command}' failed: {exception.Message}");
        }
    }

    static void EnsureSuccess(JsonElement result, string action)
    {
        var success = GetProperty(result, "success");
        if (success is { ValueKind: JsonValueKind.False })
        {
            var message = GetString(result, "message") ?? result.ToString();
            throw new InvalidOperationException($"Failed to {action}: {message}");
        }
    }

    static string? GetString(JsonElement element, string name) =>
        GetProperty(element, name)?.GetString();

    static JsonElement? GetProperty(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var exact))
        {
            return exact;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }

    static string ReadEditorVersion(string project)
    {
        var path = Path.Combine(project, "ProjectSettings", "ProjectVersion.txt");
        var line = File.ReadLines(path)
            .FirstOrDefault(l => l.StartsWith("m_EditorVersion:", StringComparison.Ordinal));
        var value = line?.Split(':', 2).ElementAtOrDefault(1)?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Could not read editor version from {path}");
        }

        return value;
    }

    static ProcessResult RunProcess(string fileName, IReadOnlyList<string> arguments)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
        {
            psi.ArgumentList.Add(argument);
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Could not start {fileName}");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
    }

    string Relative(string path) =>
        Path.GetRelativePath(paths.Root, path).Replace('\\', '/');

    readonly record struct ProcessResult(int ExitCode, string StandardOutput, string StandardError);
    readonly record struct ConnectedEditor(int Pid, string State);
}
