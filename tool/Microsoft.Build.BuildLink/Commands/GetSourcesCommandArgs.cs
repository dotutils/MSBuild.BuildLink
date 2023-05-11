namespace Microsoft.Build.BuildLink.Commands;

public sealed class GetSourcesCommandArgs
{
    public GetSourcesCommandArgs(string packageName, string? packageVersion, string? supplementalBuildMetadataFilePath,
        bool ignoreBuildlinkJson, bool allowPreRelease, string? packageSource, string? sourcesCodesDownloadRoot, string? libFile)
    {
        PackageName = packageName;
        PackageVersion = packageVersion;
        SupplementalBuildMetadataFilePath = supplementalBuildMetadataFilePath;
        IgnoreBuildlinkJson = ignoreBuildlinkJson;
        AllowPreRelease = allowPreRelease;
        PackageSource = packageSource;
        SourcesCodesDownloadRoot = sourcesCodesDownloadRoot;
        LibFile = libFile;
    }

    public string PackageName { get; init; }
    public string? PackageVersion { get; init; }
    public string? SupplementalBuildMetadataFilePath { get; init; }
    public bool IgnoreBuildlinkJson { get; init; }
    public bool AllowPreRelease { get; init; }
    public string? PackageSource { get; init; }
    public string? SourcesCodesDownloadRoot { get; init; }
    public string? LibFile { get; init; }
}
