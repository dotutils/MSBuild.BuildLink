namespace Microsoft.Build.BuildLink.NuGet;

internal interface IFileSystem
{
    void CreateDirectory(string path);

    bool FileExists(string path);

    void RenameFile(string original, string @new);
}
