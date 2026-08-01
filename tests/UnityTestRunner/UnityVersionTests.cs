using TUnit.Core;

namespace Alchemy.UnityTestRunner;

public sealed class Unity2022_3UnitTests
{
    private static readonly UnityProject Project =
        UnityProject.Locate("../versions/Unity2022.3");

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

public sealed class Unity6000_5UnitTests
{
    private static readonly UnityProject Project =
        UnityProject.Locate("../versions/Unity6000.5");

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

public sealed class Unity6000_7UnitTests
{
    private static readonly UnityProject Project =
        UnityProject.Locate("../versions/Unity6000.7");

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
