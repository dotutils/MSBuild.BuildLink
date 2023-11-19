using DotUtils.MsBuild.BuildLink.CommandExecutionUtils;
using DotUtils.MsBuild.BuildLink.Reporting;
using Microsoft.Extensions.Logging;

namespace DotUtils.MsBuild.BuildLink.NuGet;

internal class DotnetCommandPackageSourcesProvider : IPackageSourcesProvider
{
    private readonly ILogger<DotnetCommandPackageSourcesProvider> _logger;

    public DotnetCommandPackageSourcesProvider(ILogger<DotnetCommandPackageSourcesProvider> logger) => _logger = logger;

    public IEnumerable<string> GetPackageSources()
    {
        CommandExecutionUtils.DotnetCommand cmd = new DotnetCommand(_logger, "nuget", "list", "source", "--format", "Short");
        var result = cmd.Execute();
        if (result.ExitCode != 0)
        {
            string error = $"`Dotnet nuget list` command returned unexpected exit code - {result.ExitCode}";
            _logger.LogError(error);
            throw new BuildLinkException(error, BuildLinkErrorCode.DotnetCommandError);
        }

        var sources = (result.StdOut ?? string.Empty)
            .Split(Environment.NewLine)
            .Select(line => line.Split(null, 2))
            // enabled ones
            .Where(parts => parts.Length == 2 && parts[0].Contains('E'))
            .Select(parts => parts[1])
            .ToList();

        if (sources.Count == 0)
        {
            string error = $"Couldn't obtain any enabled nuget sources via `Dotnet nuget list`";
            _logger.LogError(error);
            throw new BuildLinkException(error, BuildLinkErrorCode.NotEnoughInformationToProceed);
        }

        return sources;
    }
}