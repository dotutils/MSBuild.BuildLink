using System.Runtime.InteropServices;
using DotUtils.MsBuild.BuildLink.Reporting;

namespace DotUtils.MsBuild.BuildLink.NuGet;

internal class DefaultEnvironment: IEnvironment
{
    public DefaultEnvironment()
    {
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        string userProfile = Environment.GetEnvironmentVariable(isWindows ? "USERPROFILE" : "HOME")
                             ?? throw new BuildLinkException("HOME or USERPROFILE environment variable is not defined, the environment is not supported", BuildLinkErrorCode.UnsupportedScenario);

        BuildLinkHomeFolder = Path.Combine(userProfile, ".buildlink");
    }

    public string BuildLinkHomeFolder { get; }
}