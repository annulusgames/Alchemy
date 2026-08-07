namespace Alchemy.Docs;

internal sealed class DocsCommands
{
    /// <summary>
    /// Generate attribute documentation pages (and Inspector screenshots unless disabled).
    /// </summary>
    /// <param name="dryRun">Compute diffs only; do not write files or launch Unity.</param>
    /// <param name="noCapture">Update markdown/toc/i18n only; skip Unity capture.</param>
    public async Task Generate(
        bool dryRun = false,
        bool noCapture = false,
        CancellationToken cancellationToken = default)
    {
        var paths = RepoPaths.Locate();
        if (dryRun)
        {
            noCapture = true;
        }

        Environment.ExitCode = await GeneratePipeline.RunAsync(
            paths,
            dryRun,
            noCapture,
            cancellationToken);
    }
}
