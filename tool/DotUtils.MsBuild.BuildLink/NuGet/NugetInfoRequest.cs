using DotUtils.MsBuild.BuildLink.Reporting;
using NuGet.Versioning;

namespace DotUtils.MsBuild.BuildLink.NuGet;

public class NugetInfoRequest
{
    public NugetInfoRequest(string packageName)
    {
        PackageName = packageName;
    }

    public NugetInfoRequest(string packageName, string? packageVersion)
    {
        PackageName = packageName;

        if (!string.IsNullOrEmpty(packageVersion))
        {
            if (!NuGetVersion.TryParse(packageName, out NuGetVersion v))
            {
                throw new BuildLinkException($"Unrecognizable nuget version specified: '{packageVersion}'.",
                    BuildLinkErrorCode.InvalidData);
            }

            PackageVersion = v;
        }
    }

    public string PackageName { get; init; }
    public NuGetVersion? PackageVersion { get; init; }
    public bool AllowPrerelease { get; set; }
    public string? PackageSource { get; set; }
}
