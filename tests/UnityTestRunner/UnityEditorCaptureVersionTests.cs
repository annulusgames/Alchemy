using TUnit.Core;

namespace Alchemy.UnityTestRunner;

public interface IUnityEditorCaptureTestProject
{
    static abstract UnityProject Project { get; }
}

public abstract class UnityEditorCaptureTests<TProject>
    where TProject : IUnityEditorCaptureTestProject
{
    private static UnityProject Project => TProject.Project;

    [Before(HookType.Class)]
    public static Task Start(CancellationToken cancellationToken) =>
        UnityEditorCaptureTest.StartAsync(Project, cancellationToken);

    [After(HookType.Class)]
    public static Task Stop(CancellationToken cancellationToken) =>
        UnityEditorCaptureTest.StopAsync(Project, cancellationToken);

    [Test]
    [MethodDataSource(nameof(InspectorTestNames), SkipIfEmpty = true)]
    public Task Inspector(
        string testName,
        CancellationToken cancellationToken) =>
        UnityEditorCaptureTest.CaptureAsync(
            Project,
            testName,
            cancellationToken);

    public static IEnumerable<string> InspectorTestNames()
    {
        return UnityEditorCaptureTest.DiscoverInspectorTestNames(Project);
    }
}

[InheritsTests]
public sealed class Unity6000_0EditorCaptureTests :
    UnityEditorCaptureTests<Unity6000_0EditorCaptureTests>,
    IUnityEditorCaptureTestProject
{
    public static UnityProject Project { get; } =
        UnityProject.Locate("../versions/Unity6000.0");
}

[InheritsTests]
public sealed class Unity6000_3EditorCaptureTests :
    UnityEditorCaptureTests<Unity6000_3EditorCaptureTests>,
    IUnityEditorCaptureTestProject
{
    public static UnityProject Project { get; } =
        UnityProject.Locate("../versions/Unity6000.3");
}

[InheritsTests]
public sealed class Unity6000_5EditorCaptureTests :
    UnityEditorCaptureTests<Unity6000_5EditorCaptureTests>,
    IUnityEditorCaptureTestProject
{
    public static UnityProject Project { get; } =
        UnityProject.Locate("../versions/Unity6000.5");
}

[InheritsTests]
public sealed class Unity6000_7EditorCaptureTests :
    UnityEditorCaptureTests<Unity6000_7EditorCaptureTests>,
    IUnityEditorCaptureTestProject
{
    public static UnityProject Project { get; } =
        UnityProject.Locate("../versions/Unity6000.7");
}
