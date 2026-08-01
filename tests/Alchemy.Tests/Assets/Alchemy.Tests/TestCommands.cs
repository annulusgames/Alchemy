using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Alchemy.Tests
{
    [InitializeOnLoad]
    public static class TestCommands
    {
        private const string MenuRoot = "Alchemy/Tests/";
        private const string RunStateKey = "Alchemy.Tests.TestCommands.RunState";
        private const string TestCountKey = "Alchemy.Tests.TestCommands.TestCount";
        private const string FailureCountKey = "Alchemy.Tests.TestCommands.FailureCount";
        private const string AutoQuitArgument = "--auto-quit";
        private const string ResultArgument = "-testResults";
        private const string LongResultArgument = "--test-results";

        private const int SuccessExitCode = 0;
        private const int TestFailureExitCode = 2;
        private const int RunErrorExitCode = 3;

        private static readonly TestRunnerApi TestRunnerApi;

        static TestCommands()
        {
            TestRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            TestRunnerApi.RegisterCallbacks(new TestCallbacks());
        }

        [MenuItem(MenuRoot + "Refresh")]
        public static void Refresh()
        {
            AssetDatabase.Refresh();
            if (ShouldAutoQuit)
            {
                EditorApplication.Exit(SuccessExitCode);
            }
        }

        [MenuItem(MenuRoot + "Run All EditMode Tests")]
        public static void RunAllEditModeTests()
        {
            StartRun(RunState.EditMode);
        }

        [MenuItem(MenuRoot + "Run All EditMode Tests", true)]
        private static bool CanRunAllEditModeTests()
        {
            return !IsRunning;
        }

        [MenuItem(MenuRoot + "Run All PlayMode Tests")]
        public static void RunAllPlayModeTests()
        {
            StartRun(RunState.PlayMode);
        }

        [MenuItem(MenuRoot + "Run All PlayMode Tests", true)]
        private static bool CanRunAllPlayModeTests()
        {
            return !IsRunning;
        }

        [MenuItem(MenuRoot + "Run All Tests")]
        public static void RunAllTests()
        {
            StartRun(RunState.AllEditMode);
        }

        [MenuItem(MenuRoot + "Run All Tests", true)]
        private static bool CanRunAllTests()
        {
            return !IsRunning;
        }

        private static bool IsRunning => CurrentRunState != RunState.None;

        private static RunState CurrentRunState
        {
            get => (RunState)SessionState.GetInt(RunStateKey, (int)RunState.None);
            set => SessionState.SetInt(RunStateKey, (int)value);
        }

        private static void StartRun(RunState runState)
        {
            if (Application.isBatchMode &&
                Array.Exists(Environment.GetCommandLineArgs(),
                    argument => string.Equals(argument, "-quit", StringComparison.OrdinalIgnoreCase)))
            {
                FinishWithError(
                    "Do not pass -quit when using Alchemy test commands. " +
                    "The command exits Unity after the asynchronous test run finishes.");
                return;
            }

            if (IsRunning)
            {
                Debug.LogError("An Alchemy test command is already running.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(RunErrorExitCode);
                }

                return;
            }

            SessionState.SetInt(TestCountKey, 0);
            SessionState.SetInt(FailureCountKey, 0);
            CurrentRunState = runState;

            var testMode = GetTestMode(runState);
            Debug.Log($"Running all Alchemy {testMode} tests.");
            Execute(testMode);
        }

        private static void Execute(TestMode testMode)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    EnsureTestAssembliesLoaded();
                    TestRunnerApi.Execute(new ExecutionSettings(new Filter
                    {
                        testMode = testMode
                    }));
                }
                catch (Exception exception)
                {
                    FinishWithError(exception.ToString());
                }
            };
        }

        private static void EnsureTestAssembliesLoaded()
        {
            Assembly.Load("Alchemy.Tests.EditMode.Serialization");
            Assembly.Load("Alchemy.Tests.PlayMode.Serialization");
        }

        private static TestMode GetTestMode(RunState runState)
        {
            return runState == RunState.PlayMode || runState == RunState.AllPlayMode
                ? TestMode.PlayMode
                : TestMode.EditMode;
        }

        private static void Finish(ITestResultAdaptor result)
        {
            try
            {
                WriteResult(result, CurrentRunState);
            }
            catch (Exception exception)
            {
                FinishWithError(exception.ToString());
                return;
            }

            var testCount = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
            var failureCount = result.FailCount + result.InconclusiveCount;

            SessionState.SetInt(TestCountKey, SessionState.GetInt(TestCountKey, 0) + testCount);
            SessionState.SetInt(FailureCountKey, SessionState.GetInt(FailureCountKey, 0) + failureCount);

            Debug.Log(
                $"Alchemy {GetTestMode(CurrentRunState)} tests finished: " +
                $"{result.PassCount} passed, {result.FailCount} failed, " +
                $"{result.InconclusiveCount} inconclusive, {result.SkipCount} skipped.");

            if (CurrentRunState == RunState.AllEditMode)
            {
                CurrentRunState = RunState.AllPlayMode;
                EditorApplication.delayCall += () =>
                {
                    Debug.Log("Running all Alchemy PlayMode tests.");
                    Execute(TestMode.PlayMode);
                };
                return;
            }

            var totalTestCount = SessionState.GetInt(TestCountKey, 0);
            var totalFailureCount = SessionState.GetInt(FailureCountKey, 0);
            ClearRunState();

            if (totalTestCount == 0)
            {
                Debug.LogWarning("No Alchemy tests were executed.");
            }

            Debug.Log($"Alchemy test run finished: {totalTestCount} tests, {totalFailureCount} failures.");

            if (ShouldAutoQuit)
            {
                EditorApplication.Exit(totalFailureCount == 0 ? SuccessExitCode : TestFailureExitCode);
            }
        }

        private static void FinishWithError(string message)
        {
            ClearRunState();
            Debug.LogError($"Alchemy test run failed: {message}");

            if (ShouldAutoQuit)
            {
                EditorApplication.Exit(RunErrorExitCode);
            }
        }

        private static bool ShouldAutoQuit => HasArgument(AutoQuitArgument);

        private static void WriteResult(ITestResultAdaptor result, RunState runState)
        {
            var outputPath = GetResultPath(runState);
            if (string.IsNullOrEmpty(outputPath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
            };

            using var writer = XmlWriter.Create(outputPath, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("test-run");
            writer.WriteAttributeString("id", "2");
            writer.WriteAttributeString(
                "testcasecount",
                (result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount).ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("result", result.ResultState);
            writer.WriteAttributeString("total", (result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount).ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("passed", result.PassCount.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("failed", result.FailCount.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("inconclusive", result.InconclusiveCount.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("skipped", result.SkipCount.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("asserts", result.AssertCount.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("engine-version", "3.5.0.0");
            writer.WriteAttributeString("clr-version", Environment.Version.ToString());
            writer.WriteAttributeString("start-time", result.StartTime.ToString("u", CultureInfo.InvariantCulture));
            writer.WriteAttributeString("end-time", result.EndTime.ToString("u", CultureInfo.InvariantCulture));
            writer.WriteAttributeString("duration", result.Duration.ToString(CultureInfo.InvariantCulture));
            writer.WriteRaw(result.ToXml().OuterXml);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        private static string GetResultPath(RunState runState)
        {
            var path = GetArgumentValue(ResultArgument) ?? GetArgumentValue(LongResultArgument);
            if (string.IsNullOrEmpty(path) || runState == RunState.EditMode || runState == RunState.PlayMode)
            {
                return path;
            }

            var suffix = GetTestMode(runState).ToString();
            return Path.Combine(
                Path.GetDirectoryName(path) ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(path)}.{suffix}{Path.GetExtension(path)}");
        }

        private static string GetArgumentValue(string name)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }

        private static bool HasArgument(string name)
        {
            return Array.Exists(
                Environment.GetCommandLineArgs(),
                argument => string.Equals(argument, name, StringComparison.OrdinalIgnoreCase));
        }

        private static void ClearRunState()
        {
            SessionState.EraseInt(RunStateKey);
            SessionState.EraseInt(TestCountKey);
            SessionState.EraseInt(FailureCountKey);
        }

        private enum RunState
        {
            None,
            EditMode,
            PlayMode,
            AllEditMode,
            AllPlayMode
        }

        private sealed class TestCallbacks : IErrorCallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                if (IsRunning)
                {
                    Finish(result);
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }

            public void OnError(string message)
            {
                if (IsRunning)
                {
                    FinishWithError(message);
                }
            }
        }
    }
}
