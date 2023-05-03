namespace Microsoft.Build.BuildLink.NuGet;

internal interface IFileSystem
{
    void CreateDirectory(string path);
}