namespace Microsoft.Build.BuildLink.NuGet;

internal interface IFileSystem
{
    void CreateDirectory(string path);

    bool FileExists(string path);

    bool PathExists(string path);

    void RenameFile(string original, string @new);

    void DeleteDirectory(string path);

    IEnumerable<string> EnumerateDirectories(string dir);
}
