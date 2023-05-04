﻿namespace Microsoft.Build.BuildLink.Commands;

public sealed class GetSourcesCommandArgs
{
    public GetSourcesCommandArgs(string packageName, string? packageVersion, string? supplementalBuildMetadataFilePath,
        bool allowPreRelease, string? packageSource, string? sourcesCodesDownloadRoot)
    {
        PackageName = packageName;
        PackageVersion = packageVersion;
        SupplementalBuildMetadataFilePath = supplementalBuildMetadataFilePath;
        AllowPreRelease = allowPreRelease;
        PackageSource = packageSource;
        SourcesCodesDownloadRoot = sourcesCodesDownloadRoot;
    }

    public string PackageName { get; init; }
    public string? PackageVersion { get; init; }
    public string? SupplementalBuildMetadataFilePath { get; init; }
    public bool AllowPreRelease { get; init; }
    public string? PackageSource { get; init; }
    public string? SourcesCodesDownloadRoot { get; init; }
}
