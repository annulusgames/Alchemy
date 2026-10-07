using TUnit.Core;

namespace Alchemy.UnityTestRunner;

public sealed class Unity6000_0UnitTests
{
    private static readonly UnityProject Project =
        UnityProject.Locate("../versions/Unity6000.0");

    [Before(HookType.Class)]
    public static Task Refresh(CancellationToken cancellationToken) =>
        UnityTest.RefreshAsync(Project, cancellationToken);

    [Test]
    public Task EditMode(CancellationToken cancellationToken) =>
        UnityTest.RunAsync(Project, TestMode.EditMode, cancellationToken);

    [Test]
    public Task PlayMode(CancellationToken cancellationToken) =>
        UnityTest.RunAsync(Project, TestMode.PlayMode, cancellationToken);
}

public sealed class Unity6000_3UnitTests
{
    private static readonly UnityProject Project =
        UnityProject.Locate("../versions/Unity6000.3");

    [Before(HookType.Class)]
    public static Task Refresh(CancellationToken cancellationToken) =>
        UnityTest.RefreshAsync(Project, cancellationToken);

    [Test]
    public Task EditMode(CancellationToken cancellationToken) =>
        UnityTest.RunAsync(Project, TestMode.EditMode, cancellationToken);

    [Test]
    public Task PlayMode(CancellationToken cancellationToken) =>
        UnityTest.RunAsync(Project, TestMode.PlayMode, cancellationToken);
}

public sealed class Unity6000_6UnitTests
{
    private static readonly UnityProject Project =
        UnityProject.Locate("../versions/Unity6000.6");

    [Before(HookType.Class)]
    public static Task Refresh(CancellationToken cancellationToken) =>
        UnityTest.RefreshAsync(Project, cancellationToken);

    [Test]
    public Task EditMode(CancellationToken cancellationToken) =>
        UnityTest.RunAsync(Project, TestMode.EditMode, cancellationToken);

    [Test]
    public Task PlayMode(CancellationToken cancellationToken) =>
        UnityTest.RunAsync(Project, TestMode.PlayMode, cancellationToken);
}

public sealed class Unity7000_0UnitTests
{
    private static readonly UnityProject Project =
        UnityProject.Locate("../versions/Unity7000.0");

    [Before(HookType.Class)]
    public static Task Refresh(CancellationToken cancellationToken) =>
        UnityTest.RefreshAsync(Project, cancellationToken);

    [Test]
    public Task EditMode(CancellationToken cancellationToken) =>
        UnityTest.RunAsync(Project, TestMode.EditMode, cancellationToken);

    [Test]
    public Task PlayMode(CancellationToken cancellationToken) =>
        UnityTest.RunAsync(Project, TestMode.PlayMode, cancellationToken);
}
