namespace DotUtils.MsBuild.BuildLink.Commands;

public sealed class AddBuildMetadataCommandArgs
{
    public AddBuildMetadataCommandArgs(
        string[]? packageName,
        string? repoRootPath,
        string? buildlinkJsonPath,
        string? libFile,
        bool? generateEmptyTemplate,
        bool? overwriteExisting)
    {
        PackageName = packageName;
        RepoRootPath = repoRootPath;
        BuildlinkJsonPath = buildlinkJsonPath;
        LibFile = libFile;
        GenerateEmptyTemplate = generateEmptyTemplate;
        OverwriteExisting = overwriteExisting;
    }

    public string[]? PackageName { get; init; }
    public string? RepoRootPath { get; init; }
    public string? BuildlinkJsonPath { get; init; }
    public string? LibFile { get; init; }
    public bool? GenerateEmptyTemplate { get; init; }
    public bool? OverwriteExisting { get; init; }
}
