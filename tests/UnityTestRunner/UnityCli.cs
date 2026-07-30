using System.Text.Json;

namespace Alchemy.UnityTestRunner;

public sealed class UnityCli
{
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
