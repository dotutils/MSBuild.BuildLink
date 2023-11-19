namespace DotUtils.MsBuild.BuildLink.NuGet;

internal interface IPackageSourcesProvider
{
    IEnumerable<string> GetPackageSources();
}