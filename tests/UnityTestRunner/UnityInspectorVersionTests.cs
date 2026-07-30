using TUnit.Core;

namespace Alchemy.UnityTestRunner;

public interface IUnityInspectorTestProject
{
    static abstract UnityProject Project { get; }
}

public abstract class UnityInspectorTests<TProject>
    where TProject : IUnityInspectorTestProject
{
    private static UnityProject Project => TProject.Project;

    [Before(HookType.Class)]
    public static Task Start(CancellationToken cancellationToken) =>
        UnityInspectorTest.StartAsync(Project, cancellationToken);

    [After(HookType.Class)]
    public static Task Stop(CancellationToken cancellationToken) =>
        UnityInspectorTest.StopAsync(Project, cancellationToken);

    [Test]
    [Arguments("ButtonTest")]
    [Arguments("DepthTest")]
    [Arguments("InheritedSerializeTest")]
    [Arguments("OnListViewChangedTest")]
    [Arguments("PreviewTest")]
    [Arguments("StringListTest")]
    [Arguments("UnsignedTest")]
    public Task Inspector(
        string testName,
        CancellationToken cancellationToken) =>
        UnityInspectorTest.CaptureAsync(
            Project,
            testName,
            cancellationToken);
}

[InheritsTests]
public sealed class Unity6000_0InspectorTests :
    UnityInspectorTests<Unity6000_0InspectorTests>,
    IUnityInspectorTestProject
{
    public static UnityProject Project { get; } =
        UnityProject.Locate("../versions/Unity6000.0");
}

[InheritsTests]
public sealed class Unity6000_3InspectorTests :
    UnityInspectorTests<Unity6000_3InspectorTests>,
    IUnityInspectorTestProject
{
    public static UnityProject Project { get; } =
        UnityProject.Locate("../versions/Unity6000.3");
}

[InheritsTests]
public sealed class Unity6000_4InspectorTests :
    UnityInspectorTests<Unity6000_4InspectorTests>,
    IUnityInspectorTestProject
{
    public static UnityProject Project { get; } =
        UnityProject.Locate("../versions/Unity6000.4");
}

[InheritsTests]
public sealed class Unity6000_5InspectorTests :
    UnityInspectorTests<Unity6000_5InspectorTests>,
    IUnityInspectorTestProject
{
    public static UnityProject Project { get; } =
        UnityProject.Locate("../versions/Unity6000.5");
}

[InheritsTests]
public sealed class Unity6000_7InspectorTests :
    UnityInspectorTests<Unity6000_7InspectorTests>,
    IUnityInspectorTestProject
{
    public static UnityProject Project { get; } =
        UnityProject.Locate("../versions/Unity6000.7");
}
