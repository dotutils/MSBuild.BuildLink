namespace Microsoft.Build.BuildLink.SourceCodes;

internal interface IBuildDescriptionFinder
{
    Task<WorkingCopyBuildDescriptor?> GetBuildDescriptorAsync(
        string repoRoot,
        string? buildDescriptorFile,
        string packageName,
        string? libAsset,
        CancellationToken token);

    WorkingCopyBuildDescriptor DiscoverBuildFiles(
        string repoRoot,
        string packageName,
        IReadOnlyList<string> assemblyNames,
        CancellationToken token = default);
}
