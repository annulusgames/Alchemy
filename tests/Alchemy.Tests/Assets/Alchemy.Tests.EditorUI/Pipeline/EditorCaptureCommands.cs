using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Alchemy.Tests.Pipeline
{
    [InitializeOnLoad]
    internal static class EditorCaptureCommands
    {
        const string EditorUiTestPackage =
            "Packages/com.annulusgames.alchemy.editor-ui-test";
        const double InitialSettleSeconds = 1d;
        const double ReusedInspectorSettleSeconds = 0.25d;
        const double PostRepaintSeconds = 0.25d;
        const int MaximumCapturedLogEntries = 1000;
        const int MaximumDimension = 4096;
        const float WindowX = 80f;
        const float WindowY = 80f;

        static readonly object Gate = new object();

        static InspectorSession inspectorSession;
        static CaptureOperation operation;
        static InspectorCaptureResult lastResult =
            InspectorCaptureResult.CreateIdle();

        static EditorCaptureCommands()
        {
            AssemblyReloadEvents.beforeAssemblyReload += CleanupBeforeReload;
        }

        [CliCommand(
            "alchemy_editor_capture_inspector_open",
            "Open the Inspector window used by the capture session.",
            MainThreadRequired = true)]
        static InspectorCaptureResult OpenInspector(
            [CliArg("width", "Capture width in pixels.")]
            int width = 640,
            [CliArg("height", "Capture height in pixels.")]
            int height = 900)
        {
            if (operation != null)
            {
                return InspectorCaptureResult.CreateFailure(
                    operation.JobId,
                    "The Inspector cannot open while a capture is running.");
            }

            if (inspectorSession != null)
            {
                return InspectorCaptureResult.CreateFailure(
                    "",
                    "An Inspector capture session is already open.");
            }

            ValidateDimensions(width, height);
            var previousSelection = Selection.objects;
            var previousActiveObject = Selection.activeObject;
            EditorWindow window = null;
            try
            {
                window = CreateInspectorWindow(width, height);
                inspectorSession = new InspectorSession(
                    window,
                    width,
                    height,
                    previousSelection,
                    previousActiveObject);
                var result = InspectorCaptureResult.CreateReady(
                    width,
                    height);
                SetLastResult(result);
                return result;
            }
            catch
            {
                if (window != null)
                {
                    window.Close();
                }

                RestoreSelection(
                    previousSelection,
                    previousActiveObject);
                inspectorSession = null;
                throw;
            }
        }

        [CliCommand(
            "alchemy_editor_capture_inspector_start",
            "Display a prefab in the open Inspector and capture it to a PNG.",
            MainThreadRequired = true)]
        static InspectorCaptureResult Start(
            [CliArg(
                "prefab",
                "Inspector test prefab name or package asset path.",
                Required = true)]
            string prefab = "",
            [CliArg(
                "output",
                "Output PNG path, absolute or relative to the Unity project.",
                Required = true)]
            string output = "")
        {
            if (operation != null)
            {
                return InspectorCaptureResult.CreateFailure(
                    operation.JobId,
                    "An Inspector capture is already running.");
            }

            var currentSession = inspectorSession;
            if (currentSession == null)
            {
                return InspectorCaptureResult.CreateFailure(
                    "",
                    "The Inspector capture session is not open.");
            }

            var prefabPath = ResolvePrefabPath(prefab);
            var outputPath = ResolveOutputPath(output);
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                throw new FileNotFoundException(
                    $"Inspector test prefab was not found: {prefabPath}",
                    prefabPath);
            }

            var pending = new CaptureOperation(
                Guid.NewGuid().ToString("N"),
                prefabPath,
                outputPath,
                currentSession.Width,
                currentSession.Height,
                currentSession.Window,
                currentSession,
                EditorApplication.timeSinceStartup +
                (currentSession.CaptureCount == 0
                    ? InitialSettleSeconds
                    : ReusedInspectorSettleSeconds));
            operation = pending;
            Application.logMessageReceived += CaptureLog;
            try
            {
                pending.Root = PrefabUtility.LoadPrefabContents(prefabPath);
                SetInspectorTarget(currentSession.Window, pending.Root);
                ExpandInspector(pending.Root, currentSession.Window);
                SetLastResult(InspectorCaptureResult.CreateRunning(pending));
                EditorApplication.update += UpdateCapture;
                return SnapshotLastResult();
            }
            catch
            {
                if (pending.Root != null)
                {
                    SetInspectorLocked(currentSession.Window, false);
                    Selection.activeObject = null;
                    PrefabUtility.UnloadPrefabContents(pending.Root);
                }

                Application.logMessageReceived -= CaptureLog;
                operation = null;
                throw;
            }
        }

        [CliCommand(
            "alchemy_editor_capture_status",
            "Return the current or most recent Editor UI capture status.",
            MainThreadRequired = false)]
        static InspectorCaptureResult Status(
            [CliArg("job_id", "Capture job identifier returned by start.")]
            string jobId = "")
        {
            var result = SnapshotLastResult();
            if (!string.IsNullOrWhiteSpace(jobId) &&
                !string.Equals(
                    jobId,
                    result.JobId,
                    StringComparison.Ordinal))
            {
                return InspectorCaptureResult.CreateFailure(
                    jobId,
                    $"Inspector capture job '{jobId}' was not found.");
            }

            return result;
        }

        [CliCommand(
            "alchemy_editor_capture_cancel",
            "Cancel the active Editor UI capture and restore the Editor state.",
            MainThreadRequired = true)]
        static InspectorCaptureResult Cancel(
            [CliArg(
                "job_id",
                "Capture job identifier returned by start.",
                Required = true)]
            string jobId = "")
        {
            var current = operation;
            if (current == null ||
                !string.Equals(
                    current.JobId,
                    jobId,
                    StringComparison.Ordinal))
            {
                return InspectorCaptureResult.CreateFailure(
                    jobId,
                    $"Inspector capture job '{jobId}' is not running.");
            }

            EditorApplication.update -= UpdateCapture;
            try
            {
                Cleanup(current);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                var failed = InspectorCaptureResult.CreateFailure(
                    current,
                    $"Inspector capture cancellation cleanup failed: {exception.Message}");
                SetLastResult(failed);
                throw;
            }
            finally
            {
                Application.logMessageReceived -= CaptureLog;
                operation = null;
            }

            var canceled = InspectorCaptureResult.CreateCanceled(current);
            SetLastResult(canceled);
            return canceled;
        }

        [CliCommand(
            "alchemy_editor_capture_inspector_close",
            "Close the Inspector window used by the capture session.",
            MainThreadRequired = true)]
        static InspectorCaptureResult CloseInspector()
        {
            if (operation != null)
            {
                return InspectorCaptureResult.CreateFailure(
                    operation.JobId,
                    "The Inspector cannot close while a capture is running.");
            }

            var currentSession = inspectorSession;
            if (currentSession == null)
            {
                return InspectorCaptureResult.CreateFailure(
                    "",
                    "The Inspector capture session is not open.");
            }

            inspectorSession = null;
            CloseInspectorSession(currentSession);
            var result = InspectorCaptureResult.CreateClosed();
            SetLastResult(result);
            return result;
        }

        [CliCommand(
            "alchemy_editor_capture_close",
            "Close an automated Editor session.",
            MainThreadRequired = true)]
        static InspectorCaptureResult Close(
            [CliArg(
                "force",
                "Allow closing an Editor not launched with --auto-quit.")]
            bool force = false)
        {
            if (operation != null)
            {
                return InspectorCaptureResult.CreateFailure(
                    operation.JobId,
                    "The Editor cannot close while an Inspector capture is running.");
            }

            if (inspectorSession != null)
            {
                return InspectorCaptureResult.CreateFailure(
                    "",
                    "Close the Inspector capture session before closing the Editor.");
            }

            if (!force &&
                !Environment.GetCommandLineArgs().Contains("--auto-quit"))
            {
                return InspectorCaptureResult.CreateFailure(
                    "",
                    "The Editor was not launched with --auto-quit.");
            }

            EditorApplication.delayCall += () => EditorApplication.Exit(0);
            return new InspectorCaptureResult
            {
                Status = "closing",
                Success = true,
                Message = "The automated Editor session is closing.",
            };
        }

        static void UpdateCapture()
        {
            var current = operation;
            if (current == null)
            {
                EditorApplication.update -= UpdateCapture;
                return;
            }

            try
            {
                if (EditorApplication.isCompiling ||
                    EditorApplication.isUpdating)
                {
                    current.CaptureAfter =
                        EditorApplication.timeSinceStartup +
                        InitialSettleSeconds;
                    current.CaptureReadyAfter = 0d;
                    return;
                }

                if (EditorApplication.timeSinceStartup < current.CaptureAfter)
                {
                    PrepareInspectorWindow(current);
                    current.CaptureReadyAfter = 0d;
                    return;
                }

                if (current.CaptureReadyAfter <= 0d)
                {
                    PrepareInspectorWindow(current);
                    current.CaptureReadyAfter =
                        EditorApplication.timeSinceStartup +
                        PostRepaintSeconds;
                    return;
                }

                if (EditorApplication.timeSinceStartup <
                    current.CaptureReadyAfter)
                {
                    return;
                }

                var bytes = Capture(current);
                Complete(current, bytes);
            }
            catch (Exception exception)
            {
                Fail(current, exception);
            }
        }

        static void PrepareInspectorWindow(CaptureOperation current)
        {
            PositionInspectorWindow(
                current.Window,
                current.Width,
                current.Height);
            ExpandInspector(current.Root, current.Window);
            current.Window.titleContent = new GUIContent("Inspector");
            current.Window.Focus();
            current.Window.Repaint();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        static int Capture(CaptureOperation current)
        {
            var pointRect = current.Window.position;
            var pointWidth = Mathf.RoundToInt(pointRect.width);
            var pointHeight = Mathf.RoundToInt(pointRect.height);
            if (pointWidth != current.Width || pointHeight != current.Height)
            {
                throw new InvalidOperationException(
                    $"Inspector window size was {pointWidth}x{pointHeight}; " +
                    $"expected {current.Width}x{current.Height}.");
            }

            var pixelRect = EditorGUIUtility.PointsToPixels(pointRect);
            var pixelXMin = Mathf.RoundToInt(pixelRect.xMin);
            var pixelYMin = Mathf.RoundToInt(pixelRect.yMin);
            var pixelXMax = Mathf.RoundToInt(pixelRect.xMax);
            var pixelYMax = Mathf.RoundToInt(pixelRect.yMax);
            var pixelWidth = pixelXMax - pixelXMin;
            var pixelHeight = pixelYMax - pixelYMin;
            var pixels = ReadWindowPixels(
                pixelXMin,
                pixelYMin,
                pixelWidth,
                pixelHeight);
            if (pixels == null || pixels.Length != pixelWidth * pixelHeight)
            {
                throw new InvalidOperationException(
                    $"Expected {pixelWidth * pixelHeight} pixels but received " +
                    $"{(pixels == null ? 0 : pixels.Length)}.");
            }

            var sourceTexture = new Texture2D(
                pixelWidth,
                pixelHeight,
                TextureFormat.RGB24,
                false);
            Texture2D outputTexture = null;
            try
            {
                sourceTexture.SetPixels(pixels);
                sourceTexture.Apply(false, false);
                outputTexture = ResizeCapture(
                    sourceTexture,
                    current.Width,
                    current.Height);
                var png = outputTexture.EncodeToPNG();
                var directory = Path.GetDirectoryName(current.OutputPath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    throw new InvalidOperationException(
                        $"Could not determine the output directory for " +
                        $"'{current.OutputPath}'.");
                }

                Directory.CreateDirectory(directory);
                File.WriteAllBytes(current.OutputPath, png);
                return png.Length;
            }
            finally
            {
                if (outputTexture != null && outputTexture != sourceTexture)
                {
                    Object.DestroyImmediate(outputTexture);
                }

                Object.DestroyImmediate(sourceTexture);
            }
        }

        static Color[] ReadWindowPixels(
            int x,
            int y,
            int width,
            int height)
        {
            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                return UnityEditorInternal.InternalEditorUtility
                    .ReadScreenPixel(
                        new Vector2(x, y),
                        width,
                        height);
            }

            if (x < 0 || y < 0)
            {
                throw new InvalidOperationException(
                    $"Inspector capture does not support negative macOS " +
                    $"screen coordinates: ({x}, {y}).");
            }

            // On macOS, ReadScreenPixel ignores its origin on Retina displays.
            // Read the screen prefix that contains the window, then crop it in
            // texture space. Pixel rows are bottom-up, so the requested window
            // occupies the bottom rows of a prefix ending at y + height.
            var screenWidth = x + width;
            var screenHeight = y + height;
            var screenPixels = UnityEditorInternal.InternalEditorUtility
                .ReadScreenPixel(
                    Vector2.zero,
                    screenWidth,
                    screenHeight);
            if (screenPixels == null ||
                screenPixels.Length != screenWidth * screenHeight)
            {
                throw new InvalidOperationException(
                    $"Expected {screenWidth * screenHeight} macOS screen " +
                    $"pixels but received " +
                    $"{(screenPixels == null ? 0 : screenPixels.Length)}.");
            }

            var result = new Color[width * height];
            for (var row = 0; row < height; row++)
            {
                Array.Copy(
                    screenPixels,
                    row * screenWidth + x,
                    result,
                    row * width,
                    width);
            }

            return result;
        }

        static Texture2D ResizeCapture(
            Texture2D source,
            int width,
            int height)
        {
            if (source.width == width && source.height == height)
            {
                return source;
            }

            source.filterMode = FilterMode.Bilinear;
            var previousRenderTexture = RenderTexture.active;
            var renderTexture = RenderTexture.GetTemporary(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32);
            Texture2D result = null;
            try
            {
                Graphics.Blit(source, renderTexture);
                RenderTexture.active = renderTexture;
                result = new Texture2D(
                    width,
                    height,
                    TextureFormat.RGB24,
                    false);
                result.ReadPixels(
                    new Rect(0f, 0f, width, height),
                    0,
                    0);
                result.Apply(false, false);
                return result;
            }
            catch
            {
                if (result != null)
                {
                    Object.DestroyImmediate(result);
                }

                throw;
            }
            finally
            {
                RenderTexture.active = previousRenderTexture;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        static void Complete(
            CaptureOperation current,
            int bytes)
        {
            EditorApplication.update -= UpdateCapture;
            try
            {
                Cleanup(current);
                current.Session.CaptureCount++;
                SetLastResult(InspectorCaptureResult.CreateCompleted(
                    current,
                    bytes));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetLastResult(InspectorCaptureResult.CreateFailure(
                    current,
                    $"Inspector capture cleanup failed: {exception.Message}"));
            }
            finally
            {
                Application.logMessageReceived -= CaptureLog;
                operation = null;
            }
        }

        static void Fail(
            CaptureOperation current,
            Exception captureException)
        {
            EditorApplication.update -= UpdateCapture;
            Debug.LogException(captureException);

            Exception cleanupException = null;
            try
            {
                Cleanup(current);
            }
            catch (Exception exception)
            {
                cleanupException = exception;
                Debug.LogException(exception);
            }

            var message = cleanupException == null
                ? captureException.Message
                : $"{captureException.Message} Cleanup also failed: " +
                  cleanupException.Message;
            SetLastResult(InspectorCaptureResult.CreateFailure(
                current,
                message));
            Application.logMessageReceived -= CaptureLog;
            operation = null;
        }

        static void CaptureLog(
            string message,
            string stackTrace,
            LogType type)
        {
            var current = operation;
            if (current == null)
            {
                return;
            }

            current.AddLog(
                message,
                stackTrace,
                GetLogKind(type));
        }

        static string GetLogKind(LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    return "warning";
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    return "error";
                default:
                    return "info";
            }
        }

        static EditorWindow CreateInspectorWindow(
            int width,
            int height)
        {
            var inspectorType = typeof(EditorWindow).Assembly.GetType(
                "UnityEditor.InspectorWindow",
                true);
            var window = (EditorWindow)ScriptableObject.CreateInstance(
                inspectorType);
            window.titleContent = new GUIContent("Inspector");
            var size = new Vector2(width, height);
            window.minSize = size;
            window.maxSize = size;
            window.ShowUtility();
            PositionInspectorWindow(window, width, height);
            SetInspectorLocked(window, false);
            window.Focus();
            window.Repaint();
            return window;
        }

        static void SetInspectorTarget(
            EditorWindow window,
            GameObject root)
        {
            SetInspectorLocked(window, false);
            Selection.activeGameObject = root;
            SetInspectorLocked(window, true);
            window.Focus();
            window.Repaint();
        }

        static void SetInspectorLocked(
            EditorWindow window,
            bool value)
        {
            var lockProperty = window.GetType().GetProperty(
                "isLocked",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            if (lockProperty == null)
            {
                throw new MissingMemberException(
                    window.GetType().FullName,
                    "isLocked");
            }

            lockProperty.SetValue(window, value);
        }

        static void PositionInspectorWindow(
            EditorWindow window,
            int width,
            int height)
        {
            window.position = new Rect(
                WindowX,
                WindowY,
                width,
                height);
        }

        static void ExpandInspector(
            GameObject root,
            EditorWindow window)
        {
            foreach (var component in root.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                UnityEditorInternal.InternalEditorUtility
                    .SetIsInspectorExpanded(component, true);
                var serializedObject = new SerializedObject(component);
                var property = serializedObject.GetIterator();
                var hasProperty = property.NextVisible(true);
                while (hasProperty)
                {
                    property.isExpanded = true;
                    hasProperty = property.NextVisible(true);
                }
            }

            if (window == null)
            {
                return;
            }

            var pending = new Stack<VisualElement>();
            pending.Push(window.rootVisualElement);
            while (pending.Count > 0)
            {
                var element = pending.Pop();
                if (element is Foldout foldout)
                {
                    foldout.SetValueWithoutNotify(true);
                }

                for (var index = 0;
                     index < element.hierarchy.childCount;
                     index++)
                {
                    pending.Push(element.hierarchy[index]);
                }
            }
        }

        static string ResolvePrefabPath(string prefab)
        {
            if (string.IsNullOrWhiteSpace(prefab))
            {
                throw new ArgumentException(
                    "A prefab name or asset path is required.",
                    nameof(prefab));
            }

            var value = prefab.Replace('\\', '/');
            if (!value.EndsWith(
                    ".prefab",
                    StringComparison.OrdinalIgnoreCase))
            {
                value += ".prefab";
            }

            return value.Contains("/")
                ? value
                : $"{EditorUiTestPackage}/{value}";
        }

        static string ResolveOutputPath(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                throw new ArgumentException(
                    "An output PNG path is required.",
                    nameof(output));
            }

            var path = Path.IsPathRooted(output)
                ? output
                : Path.Combine(
                    Path.GetDirectoryName(Application.dataPath),
                    output);
            path = Path.GetFullPath(path);
            if (!string.Equals(
                    Path.GetExtension(path),
                    ".png",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Inspector capture output must be a PNG path: {path}",
                    nameof(output));
            }

            return path;
        }

        static void ValidateDimensions(int width, int height)
        {
            if (width <= 0 ||
                height <= 0 ||
                width > MaximumDimension ||
                height > MaximumDimension)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    $"Inspector capture dimensions must be between 1 and " +
                    $"{MaximumDimension}: {width}x{height}.");
            }
        }

        static void Cleanup(CaptureOperation current)
        {
            if (current.Window != null)
            {
                SetInspectorLocked(current.Window, false);
            }

            Selection.activeObject = null;
            if (current.Root != null)
            {
                PrefabUtility.UnloadPrefabContents(current.Root);
            }
        }

        static void CloseInspectorSession(InspectorSession current)
        {
            if (current.Window != null)
            {
                SetInspectorLocked(current.Window, false);
                current.Window.Close();
            }

            RestoreSelection(
                current.PreviousSelection,
                current.PreviousActiveObject);
        }

        static void RestoreSelection(
            Object[] previousSelection,
            Object previousActiveObject)
        {
            Selection.objects = previousSelection;
            if (previousActiveObject != null)
            {
                Selection.activeObject = previousActiveObject;
            }
        }

        static void CleanupBeforeReload()
        {
            var current = operation;
            if (current != null)
            {
                EditorApplication.update -= UpdateCapture;
                Cleanup(current);
                Application.logMessageReceived -= CaptureLog;
                operation = null;
                SetLastResult(InspectorCaptureResult.CreateFailure(
                    current,
                    "Inspector capture was interrupted by an assembly reload."));
            }

            var currentSession = inspectorSession;
            if (currentSession != null)
            {
                inspectorSession = null;
                CloseInspectorSession(currentSession);
            }
        }

        static void SetLastResult(InspectorCaptureResult result)
        {
            lock (Gate)
            {
                lastResult = result;
            }
        }

        static InspectorCaptureResult SnapshotLastResult()
        {
            lock (Gate)
            {
                return lastResult.Copy();
            }
        }

        sealed class CaptureOperation
        {
            public CaptureOperation(
                string jobId,
                string prefabPath,
                string outputPath,
                int width,
                int height,
                EditorWindow window,
                InspectorSession session,
                double captureAfter)
            {
                JobId = jobId;
                PrefabPath = prefabPath;
                OutputPath = outputPath;
                Width = width;
                Height = height;
                Window = window;
                Session = session;
                CaptureAfter = captureAfter;
            }

            public string JobId { get; }
            public string PrefabPath { get; }
            public string OutputPath { get; }
            public int Width { get; }
            public int Height { get; }
            public GameObject Root { get; set; }
            public EditorWindow Window { get; }
            public InspectorSession Session { get; }
            public double CaptureAfter { get; set; }
            public double CaptureReadyAfter { get; set; }
            public List<InspectorCaptureLogEntry> Logs { get; } =
                new List<InspectorCaptureLogEntry>();
            public int WarningCount { get; private set; }
            public int ErrorCount { get; private set; }
            public int DroppedLogCount { get; private set; }

            public void AddLog(
                string message,
                string stackTrace,
                string kind)
            {
                if (kind == "warning")
                {
                    WarningCount++;
                }
                else if (kind == "error")
                {
                    ErrorCount++;
                }

                if (Logs.Count >= MaximumCapturedLogEntries)
                {
                    DroppedLogCount++;
                    return;
                }

                Logs.Add(new InspectorCaptureLogEntry
                {
                    Kind = kind,
                    Message = message,
                    StackTrace = stackTrace,
                });
            }
        }

        sealed class InspectorSession
        {
            public InspectorSession(
                EditorWindow window,
                int width,
                int height,
                Object[] previousSelection,
                Object previousActiveObject)
            {
                Window = window;
                Width = width;
                Height = height;
                PreviousSelection = previousSelection;
                PreviousActiveObject = previousActiveObject;
            }

            public EditorWindow Window { get; }
            public int Width { get; }
            public int Height { get; }
            public Object[] PreviousSelection { get; }
            public Object PreviousActiveObject { get; }
            public int CaptureCount { get; set; }
        }

        sealed class InspectorCaptureResult
        {
            public string JobId { get; set; }
            public string Status { get; set; }
            public bool Success { get; set; }
            public string Message { get; set; }
            public string PrefabPath { get; set; }
            public string Path { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int Bytes { get; set; }
            public InspectorCaptureLogEntry[] Logs { get; set; } =
                new InspectorCaptureLogEntry[0];
            public int WarningCount { get; set; }
            public int ErrorCount { get; set; }
            public int DroppedLogCount { get; set; }

            public InspectorCaptureResult Copy()
            {
                return new InspectorCaptureResult
                {
                    JobId = JobId,
                    Status = Status,
                    Success = Success,
                    Message = Message,
                    PrefabPath = PrefabPath,
                    Path = Path,
                    Width = Width,
                    Height = Height,
                    Bytes = Bytes,
                    Logs = Logs.Select(entry => entry.Copy()).ToArray(),
                    WarningCount = WarningCount,
                    ErrorCount = ErrorCount,
                    DroppedLogCount = DroppedLogCount,
                };
            }

            public static InspectorCaptureResult CreateIdle()
            {
                return new InspectorCaptureResult
                {
                    Status = "idle",
                    Success = true,
                    Message = "No Inspector capture has run.",
                };
            }

            public static InspectorCaptureResult CreateReady(
                int width,
                int height)
            {
                return new InspectorCaptureResult
                {
                    Status = "ready",
                    Success = true,
                    Message = "The Inspector capture session is ready.",
                    Width = width,
                    Height = height,
                };
            }

            public static InspectorCaptureResult CreateClosed()
            {
                return new InspectorCaptureResult
                {
                    Status = "closed",
                    Success = true,
                    Message = "The Inspector capture session is closed.",
                };
            }

            public static InspectorCaptureResult CreateRunning(
                CaptureOperation current)
            {
                return new InspectorCaptureResult
                {
                    JobId = current.JobId,
                    Status = "running",
                    Success = true,
                    Message = "Inspector capture is settling.",
                    PrefabPath = current.PrefabPath,
                    Path = current.OutputPath,
                    Width = current.Width,
                    Height = current.Height,
                };
            }

            public static InspectorCaptureResult CreateCompleted(
                CaptureOperation current,
                int bytes)
            {
                return new InspectorCaptureResult
                {
                    JobId = current.JobId,
                    Status = "completed",
                    Success = true,
                    Message = "Inspector capture completed.",
                    PrefabPath = current.PrefabPath,
                    Path = current.OutputPath,
                    Width = current.Width,
                    Height = current.Height,
                    Bytes = bytes,
                    Logs = current.Logs
                        .Select(entry => entry.Copy())
                        .ToArray(),
                    WarningCount = current.WarningCount,
                    ErrorCount = current.ErrorCount,
                    DroppedLogCount = current.DroppedLogCount,
                };
            }

            public static InspectorCaptureResult CreateCanceled(
                CaptureOperation current)
            {
                return new InspectorCaptureResult
                {
                    JobId = current.JobId,
                    Status = "canceled",
                    Success = false,
                    Message = "Inspector capture was canceled.",
                    Logs = current.Logs
                        .Select(entry => entry.Copy())
                        .ToArray(),
                    WarningCount = current.WarningCount,
                    ErrorCount = current.ErrorCount,
                    DroppedLogCount = current.DroppedLogCount,
                };
            }

            public static InspectorCaptureResult CreateFailure(
                CaptureOperation current,
                string message)
            {
                return new InspectorCaptureResult
                {
                    JobId = current.JobId,
                    Status = "failed",
                    Success = false,
                    Message = message,
                    Logs = current.Logs
                        .Select(entry => entry.Copy())
                        .ToArray(),
                    WarningCount = current.WarningCount,
                    ErrorCount = current.ErrorCount,
                    DroppedLogCount = current.DroppedLogCount,
                };
            }

            public static InspectorCaptureResult CreateFailure(
                string jobId,
                string message)
            {
                return new InspectorCaptureResult
                {
                    JobId = jobId,
                    Status = "failed",
                    Success = false,
                    Message = message,
                };
            }
        }

        sealed class InspectorCaptureLogEntry
        {
            public string Kind { get; set; }
            public string Message { get; set; }
            public string StackTrace { get; set; }

            public InspectorCaptureLogEntry Copy()
            {
                return new InspectorCaptureLogEntry
                {
                    Kind = Kind,
                    Message = Message,
                    StackTrace = StackTrace,
                };
            }
        }
    }
}
