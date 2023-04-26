namespace Microsoft.Build.BuildLink.Commands;

public sealed class GetSourcesCommandArgs
{
    public GetSourcesCommandArgs(string packageName, string? packageVersion)
    {
        PackageName = packageName;
        PackageVersion = packageVersion;
    }

    public GetSourcesCommandArgs(string packageName, string? packageVersion, string? supplementalBuildMetadataFilePath)
    {
        PackageName = packageName;
        PackageVersion = packageVersion;
        SupplementalBuildMetadataFilePath = supplementalBuildMetadataFilePath;
    }

    public string PackageName { get; init; }
    public string? PackageVersion { get; init; }
    public string? SupplementalBuildMetadataFilePath { get; init; }
}