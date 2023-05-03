namespace Microsoft.Build.BuildLink.NuGet;

internal interface IPackageSourcesProvider
{
    IEnumerable<string> GetPackageSources();
}