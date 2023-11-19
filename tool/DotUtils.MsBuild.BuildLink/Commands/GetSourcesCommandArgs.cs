namespace DotUtils.MsBuild.BuildLink.Commands;

public sealed class GetSourcesCommandArgs
{
    public GetSourcesCommandArgs(
        string packageName,
        string? packageVersion,
        string? supplementalBuildMetadataFilePath,
        bool ignoreBuildlinkJson,
        bool allowPreRelease,
        string? packageSource,
        string? sourcesCodesDownloadRoot,
        string? sourcesCheckoutBasePath,
        string? libFile,
        string? flushBuildJsonPath,
        bool appendBuildJsonOnFlush)
    {
        PackageName = packageName;
        PackageVersion = packageVersion;
        SupplementalBuildMetadataFilePath = supplementalBuildMetadataFilePath;
        IgnoreBuildlinkJson = ignoreBuildlinkJson;
        AllowPreRelease = allowPreRelease;
        PackageSource = packageSource;
        SourcesCodesDownloadRoot = sourcesCodesDownloadRoot;
        SourcesCheckoutBasePath = sourcesCheckoutBasePath;
        LibFile = libFile;
        FlushBuildJsonPath = flushBuildJsonPath;
        AppendBuildJsonOnFlush = appendBuildJsonOnFlush;
    }

    public string PackageName { get; init; }
    public string? PackageVersion { get; init; }
    public string? SupplementalBuildMetadataFilePath { get; init; }
    public bool IgnoreBuildlinkJson { get; init; }
    public bool AllowPreRelease { get; init; }
    public string? PackageSource { get; init; }
    public string? SourcesCodesDownloadRoot { get; init; }
    public string? SourcesCheckoutBasePath { get; init; }
    public string? LibFile { get; init; }
    public string? FlushBuildJsonPath { get; init; }
    public bool AppendBuildJsonOnFlush { get; init; }
}
