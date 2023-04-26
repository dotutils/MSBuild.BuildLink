namespace Microsoft.Build.BuildLink.Commands;

public sealed class AddBuildMetadataCommandArgs
{
    public AddBuildMetadataCommandArgs(string repoRootPath, bool? overwriteExisting)
    {
        RepoRootPath = repoRootPath;
        OverwriteExisting = overwriteExisting;
    }

    public string RepoRootPath { get; init; }
    public bool? OverwriteExisting { get; init; }
}