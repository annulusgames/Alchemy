# Alchemy test platform

This directory contains the Unity version projects and the TUnit integration tests that execute Alchemy's Unity Test Framework tests through Microsoft Testing Platform.

Run all registered Unity versions and test modes from the repository root:

```sh
# Run all tests
dotnet run --project tests/UnityTestRunner --configuration Release
# Run only EditMode tests
dotnet run --project tests/UnityTestRunner --configuration Release -- --treenode-filter "/*/Alchemy.UnityTestRunner/Unity*UnitTests/EditMode"
# Run only PlayMode tests
dotnet run --project tests/UnityTestRunner --configuration Release -- --treenode-filter "/*/Alchemy.UnityTestRunner/Unity*UnitTests/PlayMode"
# Run only Visual Regression Tests
dotnet run --project tests/UnityTestRunner --configuration Release -- --treenode-filter "/*/Alchemy.UnityTestRunner/Unity*EditorCaptureTests/*"
```

Unity versions are registered explicitly in `UnityTestRunner/UnityVersionTests.cs`. Each version has an EditMode test and a PlayMode test.

Unity Editor logs are stored under each version project's `Logs/UnityTestRunner/<run-id>` directory. Unity logs and NUnit reports are attached to their TUnit test, and warning-or-higher entries are written to stderr.

Visual regression testing is not automated; captures require manual review.

### Requirements

- .NET 10 SDK or later must be installed.
- The `unity` command must already be available on `PATH`.
- Every editor version declared by the version projects must already be installed.
