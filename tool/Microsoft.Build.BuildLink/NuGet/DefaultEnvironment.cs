using System.Runtime.InteropServices;
using Microsoft.Build.BuildLink.Reporting;

namespace Microsoft.Build.BuildLink.NuGet;

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