using System.ComponentModel;
using System.Diagnostics;

namespace Alchemy.UnityTestRunner;

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(ProcessSpec specification, CancellationToken cancellationToken);
}

public sealed class ProcessRunner : IProcessRunner
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(30);

    public async Task<ProcessResult> RunAsync(ProcessSpec specification, CancellationToken cancellationToken)
    {
        var timeout = specification.Timeout ?? DefaultTimeout;
        if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
        {
            throw new ProcessExecutionException(
                $"Process timeout must be positive: {timeout}.");
        }

        using var process = new Process
        {
            StartInfo = CreateStartInfo(specification),
            EnableRaisingEvents = true,
        };

        try
        {
            if (!process.Start())
            {
                throw new ProcessExecutionException($"Unable to start process '{specification.FileName}'.");
            }
        }
        catch (Win32Exception exception)
        {
            throw new ProcessExecutionException(
                $"Unable to start process '{specification.FileName}'.",
                exception);
        }

        WindowsProcessJob? processJob;
        try
        {
            processJob = WindowsProcessJob.Attach(
                process,
                specification.TerminateDescendantsOnExit);
        }
        catch
        {
            TryKill(process);
            throw;
        }

        using (processJob)
        {
            return await WaitForExitAsync(
                process,
                specification,
                timeout,
                cancellationToken);
        }
    }

    private static async Task<ProcessResult> WaitForExitAsync(
        Process process,
        ProcessSpec specification,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var processId = process.Id;
        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(timeout);
        var processCancellationToken = timeoutCancellation.Token;
        var standardOutput = process.StandardOutput.ReadToEndAsync(processCancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(processCancellationToken);

        try
        {
            await process.WaitForExitAsync(processCancellationToken);
            await Task.WhenAll(standardOutput, standardError);
        }
        catch (OperationCanceledException) when (
            !cancellationToken.IsCancellationRequested && timeoutCancellation.IsCancellationRequested)
        {
            TryKill(process);
            throw new ProcessExecutionException(
                $"Process '{specification.FileName}' timed out after {timeout}.");
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        return new ProcessResult(
            processId,
            process.ExitCode,
            await standardOutput,
            await standardError);
    }

    private static ProcessStartInfo CreateStartInfo(ProcessSpec specification)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = specification.FileName,
            WorkingDirectory = specification.WorkingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        // ArgumentList preserves argument boundaries across Windows, macOS, and Unix shells.
        foreach (var argument in specification.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static void TryKill(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
    }
}
