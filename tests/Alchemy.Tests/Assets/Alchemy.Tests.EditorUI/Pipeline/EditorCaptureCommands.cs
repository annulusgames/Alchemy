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
        const double SettleSeconds = 1d;
        const double PostRepaintSeconds = 0.25d;
        const int MaximumDimension = 4096;
        const float WindowX = 80f;
        const float WindowY = 80f;

        static readonly object Gate = new object();

        static CaptureOperation operation;
        static InspectorCaptureResult lastResult =
            InspectorCaptureResult.CreateIdle();

        static EditorCaptureCommands()
        {
            AssemblyReloadEvents.beforeAssemblyReload += CleanupBeforeReload;
        }

        [CliCommand(
            "alchemy_editor_capture_inspector_start",
            "Open an Inspector for a prefab, expand its contents, and capture it to a PNG.",
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
            string output = "",
            [CliArg("width", "Capture width in pixels.")]
            int width = 640,
            [CliArg("height", "Capture height in pixels.")]
            int height = 900)
        {
            if (operation != null)
            {
                return InspectorCaptureResult.CreateFailure(
                    operation.JobId,
                    "An Inspector capture is already running.");
            }

            ValidateDimensions(width, height);
            var prefabPath = ResolvePrefabPath(prefab);
            var outputPath = ResolveOutputPath(output);
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                throw new FileNotFoundException(
                    $"Inspector test prefab was not found: {prefabPath}",
                    prefabPath);
            }

            CaptureOperation pending = null;
            GameObject root = null;
            EditorWindow window = null;
            var previousSelection = Selection.objects;
            var previousActiveObject = Selection.activeObject;
            try
            {
                root = PrefabUtility.LoadPrefabContents(prefabPath);
                Selection.activeGameObject = root;
                ExpandInspector(root, null);

                window = CreateInspectorWindow(width, height);
                pending = new CaptureOperation(
                    Guid.NewGuid().ToString("N"),
                    prefabPath,
                    outputPath,
                    width,
                    height,
                    root,
                    window,
                    previousSelection,
                    previousActiveObject,
                    EditorApplication.timeSinceStartup + SettleSeconds);
                operation = pending;
                SetLastResult(InspectorCaptureResult.CreateRunning(pending));
                EditorApplication.update += UpdateCapture;
                return SnapshotLastResult();
            }
            catch
            {
                if (window != null)
                {
                    window.Close();
                }

                Selection.activeObject = null;
                if (root != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                Selection.objects = previousSelection;
                if (previousActiveObject != null)
                {
                    Selection.activeObject = previousActiveObject;
                }
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
            operation = null;
            try
            {
                Cleanup(current);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                var failed = InspectorCaptureResult.CreateFailure(
                    current.JobId,
                    $"Inspector capture cancellation cleanup failed: {exception.Message}");
                SetLastResult(failed);
                throw;
            }

            var canceled = InspectorCaptureResult.CreateCanceled(current.JobId);
            SetLastResult(canceled);
            return canceled;
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
                        EditorApplication.timeSinceStartup + SettleSeconds;
                    current.CaptureReadyAfter = 0d;
                    return;
                }

                if (EditorApplication.timeSinceStartup < current.CaptureAfter)
                {
                    current.Window.position = new Rect(
                        WindowX,
                        WindowY,
                        current.Width,
                        current.Height);
                    ExpandInspector(current.Root, current.Window);
                    current.Window.titleContent =
                        new GUIContent("Inspector");
                    current.Window.Focus();
                    current.Window.Repaint();
                    EditorApplication.QueuePlayerLoopUpdate();
                    current.CaptureReadyAfter = 0d;
                    return;
                }

                if (current.CaptureReadyAfter <= 0d)
                {
                    current.Window.position = new Rect(
                        WindowX,
                        WindowY,
                        current.Width,
                        current.Height);
                    ExpandInspector(current.Root, current.Window);
                    current.Window.titleContent =
                        new GUIContent("Inspector");
                    current.Window.Focus();
                    current.Window.Repaint();
                    EditorApplication.QueuePlayerLoopUpdate();
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

                var result = Capture(current);
                Complete(current, result);
            }
            catch (Exception exception)
            {
                Fail(current, exception);
            }
        }

        static InspectorCaptureResult Capture(CaptureOperation current)
        {
            var rect = current.Window.position;
            var width = Mathf.RoundToInt(rect.width);
            var height = Mathf.RoundToInt(rect.height);
            if (width != current.Width || height != current.Height)
            {
                throw new InvalidOperationException(
                    $"Inspector window size was {width}x{height}; " +
                    $"expected {current.Width}x{current.Height}.");
            }

            var origin = new Vector2(
                Mathf.Round(rect.x),
                Mathf.Round(rect.y));
            var pixels = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(
                origin,
                width,
                height);
            if (pixels == null || pixels.Length != width * height)
            {
                throw new InvalidOperationException(
                    $"Expected {width * height} pixels but received " +
                    $"{(pixels == null ? 0 : pixels.Length)}.");
            }

            var texture = new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false);
            try
            {
                texture.SetPixels(pixels);
                texture.Apply(false, false);
                var png = texture.EncodeToPNG();
                var directory = Path.GetDirectoryName(current.OutputPath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    throw new InvalidOperationException(
                        $"Could not determine the output directory for " +
                        $"'{current.OutputPath}'.");
                }

                Directory.CreateDirectory(directory);
                File.WriteAllBytes(current.OutputPath, png);
                return InspectorCaptureResult.CreateCompleted(
                    current,
                    png.Length);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        static void Complete(
            CaptureOperation current,
            InspectorCaptureResult result)
        {
            EditorApplication.update -= UpdateCapture;
            operation = null;
            try
            {
                Cleanup(current);
                SetLastResult(result);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetLastResult(InspectorCaptureResult.CreateFailure(
                    current.JobId,
                    $"Inspector capture cleanup failed: {exception.Message}"));
            }
        }

        static void Fail(
            CaptureOperation current,
            Exception captureException)
        {
            EditorApplication.update -= UpdateCapture;
            operation = null;
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
                current.JobId,
                message));
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
            window.Show();
            window.position = new Rect(
                WindowX,
                WindowY,
                width,
                height);

            var lockProperty = inspectorType.GetProperty(
                "isLocked",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            lockProperty?.SetValue(window, true);
            window.titleContent = new GUIContent("Inspector");
            window.Focus();
            window.Repaint();
            return window;
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
                current.Window.Close();
            }

            Selection.activeObject = null;
            if (current.Root != null)
            {
                PrefabUtility.UnloadPrefabContents(current.Root);
            }

            Selection.objects = current.PreviousSelection;
            if (current.PreviousActiveObject != null)
            {
                Selection.activeObject = current.PreviousActiveObject;
            }
        }

        static void CleanupBeforeReload()
        {
            var current = operation;
            if (current == null)
            {
                return;
            }

            EditorApplication.update -= UpdateCapture;
            operation = null;
            Cleanup(current);
            SetLastResult(InspectorCaptureResult.CreateFailure(
                current.JobId,
                "Inspector capture was interrupted by an assembly reload."));
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
                GameObject root,
                EditorWindow window,
                Object[] previousSelection,
                Object previousActiveObject,
                double captureAfter)
            {
                JobId = jobId;
                PrefabPath = prefabPath;
                OutputPath = outputPath;
                Width = width;
                Height = height;
                Root = root;
                Window = window;
                PreviousSelection = previousSelection;
                PreviousActiveObject = previousActiveObject;
                CaptureAfter = captureAfter;
            }

            public string JobId { get; }
            public string PrefabPath { get; }
            public string OutputPath { get; }
            public int Width { get; }
            public int Height { get; }
            public GameObject Root { get; }
            public EditorWindow Window { get; }
            public Object[] PreviousSelection { get; }
            public Object PreviousActiveObject { get; }
            public double CaptureAfter { get; set; }
            public double CaptureReadyAfter { get; set; }
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
                };
            }

            public static InspectorCaptureResult CreateCanceled(
                string jobId)
            {
                return new InspectorCaptureResult
                {
                    JobId = jobId,
                    Status = "canceled",
                    Success = false,
                    Message = "Inspector capture was canceled.",
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
    }
}
