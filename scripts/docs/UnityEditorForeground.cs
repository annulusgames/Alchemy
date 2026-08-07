using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Alchemy.Docs;

/// <summary>
/// Brings a Unity Editor process to the foreground before Inspector captures.
/// Mirrors <c>tests/UnityTestRunner/UnityEditorForeground.cs</c> for Win/macOS.
/// </summary>
internal static class UnityEditorForeground
{
    const int SwShow = 5;
    const int SwRestore = 9;

    static readonly TimeSpan WindowsActivationTimeout = TimeSpan.FromSeconds(2);
    static readonly TimeSpan MacOSActivationTimeout = TimeSpan.FromSeconds(10);

    public static async Task ActivateAsync(
        int processId,
        CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows())
        {
            await ActivateWindowsAsync(processId, cancellationToken);
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            await ActivateMacOSAsync(processId, cancellationToken);
            return;
        }

        throw new InvalidOperationException(
            "Editor capture foreground activation is supported only on Windows and macOS.");
    }

    static async Task ActivateWindowsAsync(
        int processId,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + WindowsActivationTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsForegroundProcess(processId) ||
                TryActivateWindowsEditor(processId))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }

        throw new InvalidOperationException(
            $"Could not bring Unity Editor process {processId} to the foreground.");
    }

    static async Task ActivateMacOSAsync(
        int processId,
        CancellationToken cancellationToken)
    {
        var script =
            $"tell application \"System Events\"{Environment.NewLine}" +
            $"set unityProcess to first application process whose unix id " +
            $"is {processId}{Environment.NewLine}" +
            $"set frontmost of unityProcess to true{Environment.NewLine}" +
            $"if (count of windows of unityProcess) > 0 then{Environment.NewLine}" +
            $"perform action \"AXRaise\" of window 1 of unityProcess" +
            $"{Environment.NewLine}" +
            $"end if{Environment.NewLine}" +
            $"delay 0.1{Environment.NewLine}" +
            $"return frontmost of unityProcess{Environment.NewLine}" +
            "end tell";

        var psi = new ProcessStartInfo("osascript")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("-e");
        psi.ArgumentList.Add(script);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Could not start osascript.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(MacOSActivationTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // best effort
            }

            throw new InvalidOperationException(
                $"osascript timed out after {MacOSActivationTimeout} while activating Unity {processId}.");
        }

        var stdout = (await stdoutTask).Trim();
        var stderr = (await stderrTask).Trim();
        if (process.ExitCode == 0 &&
            string.Equals(stdout, "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var diagnostic = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
        throw new InvalidOperationException(
            $"Could not bring Unity Editor process {processId} to the foreground with AppleScript: {diagnostic}");
    }

    static bool TryActivateWindowsEditor(int processId)
    {
        var window = FindEditorWindow(processId);
        if (window == IntPtr.Zero)
        {
            return false;
        }

        var popup = GetLastActivePopup(window);
        if (popup != IntPtr.Zero &&
            IsWindowVisible(popup) &&
            WindowBelongsToProcess(popup, processId))
        {
            window = popup;
        }

        _ = ShowWindowAsync(
            window,
            IsIconic(window) ? SwRestore : SwShow);

        var currentThreadId = GetCurrentThreadId();
        var foregroundThreadId = GetWindowThreadProcessId(
            GetForegroundWindow(),
            out _);
        var targetThreadId = GetWindowThreadProcessId(window, out _);
        var attachedToForeground =
            foregroundThreadId != 0 &&
            foregroundThreadId != currentThreadId &&
            AttachThreadInput(
                currentThreadId,
                foregroundThreadId,
                true);
        var attachedToTarget =
            targetThreadId != 0 &&
            targetThreadId != currentThreadId &&
            targetThreadId != foregroundThreadId &&
            AttachThreadInput(
                currentThreadId,
                targetThreadId,
                true);
        try
        {
            _ = BringWindowToTop(window);
            _ = SetActiveWindow(window);
            _ = SetForegroundWindow(window);
            _ = SetFocus(window);
        }
        finally
        {
            if (attachedToTarget)
            {
                _ = AttachThreadInput(
                    currentThreadId,
                    targetThreadId,
                    false);
            }

            if (attachedToForeground)
            {
                _ = AttachThreadInput(
                    currentThreadId,
                    foregroundThreadId,
                    false);
            }
        }

        return IsForegroundProcess(processId);
    }

    static IntPtr FindEditorWindow(int processId)
    {
        using var process = Process.GetProcessById(processId);
        process.Refresh();
        if (process.MainWindowHandle != IntPtr.Zero &&
            IsWindowVisible(process.MainWindowHandle))
        {
            return process.MainWindowHandle;
        }

        var result = IntPtr.Zero;
        _ = EnumWindows(
            (window, _) =>
            {
                if (!IsWindowVisible(window) ||
                    !WindowBelongsToProcess(window, processId))
                {
                    return true;
                }

                result = window;
                return false;
            },
            IntPtr.Zero);
        return result;
    }

    static bool IsForegroundProcess(int processId) =>
        WindowBelongsToProcess(GetForegroundWindow(), processId);

    static bool WindowBelongsToProcess(IntPtr window, int processId)
    {
        if (window == IntPtr.Zero)
        {
            return false;
        }

        _ = GetWindowThreadProcessId(window, out var windowProcessId);
        return windowProcessId == (uint)processId;
    }

    delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetLastActivePopup(IntPtr window);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsIconic(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool ShowWindowAsync(IntPtr window, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool AttachThreadInput(
        uint idAttach,
        uint idAttachTo,
        [MarshalAs(UnmanagedType.Bool)] bool attach);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool BringWindowToTop(IntPtr window);

    [DllImport("user32.dll")]
    static extern IntPtr SetActiveWindow(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    static extern IntPtr SetFocus(IntPtr window);
}
