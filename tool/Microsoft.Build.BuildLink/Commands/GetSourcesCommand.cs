using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Build.BuildLink.NuGet;
using Microsoft.Build.BuildLink.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Build.BuildLink.Commands;

internal class GetSourcesCommand : ExecutableCommand<GetSourcesCommandArgs, GetSourcesCommandHandler>
{
    private const string CommandName = "get-sources";

    private readonly Argument<string> _packageNameArgument = new("package-name")
    {
        Description = "Name of the package",
        // 0 for case where only path is specified
        Arity = new ArgumentArity(1, 1)
    };

    private readonly Option<string> _packageVersionOption = new(new[] { "-v", "--version" })
    {
        Description = "Version of the package",
    };

    private readonly Option<string> _buildFilePathOption = new(new[] { "--build-descriptor" })
    {
        Description = "Path to the build descriptor file. Relative path resolved relative to repository root. Absolute path allows to use local file not located in repository",
    };

    private readonly Option<bool> _allowPrereleaseOption = new(new[] { "--include-prerelease" })
    {
        Description = "Allow prerelease version to be used",
    };

    private readonly Option<string> _packageSourceOption = new(new[] { "-s", "--source" })
    {
        Description = "Package source feed to be used to pull the nuget",
    };

    public GetSourcesCommand()
        : base(CommandName, "Fetches the sources for the given package")
    {
        AddArgument(_packageNameArgument);
        AddOption(_packageVersionOption);
    }

    protected internal override GetSourcesCommandArgs ParseContext(ParseResult parseResult)
    {
        return new GetSourcesCommandArgs(
            parseResult.GetValueForArgument(_packageNameArgument),
            parseResult.GetValueForOption(_packageVersionOption),
            parseResult.GetValueForOption(_buildFilePathOption),
            parseResult.GetValueForOption(_allowPrereleaseOption),
            parseResult.GetValueForOption(_packageSourceOption)
        );
    }
}

internal class GetSourcesCommandHandler : ICommandExecutor<GetSourcesCommandArgs>
{
    private readonly ILogger<GetSourcesCommandHandler> _logger;
    private readonly INugetInfoProvider _nugetInfoProvider;

    public GetSourcesCommandHandler(ILogger<GetSourcesCommandHandler> logger, INugetInfoProvider nugetInfoProvider)
    {
        _logger = logger;
        _nugetInfoProvider = nugetInfoProvider;
    }

    public async Task<BuildLinkErrorCode> ExecuteAsync(GetSourcesCommandArgs args, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Obtaining the sources for {name}.{version}.", args.PackageName, args.PackageVersion);

        NugetInfoRequest request = new(args.PackageName, args.PackageVersion)
        {
            AllowPrerelease = args.AllowPreRelease, PackageSource = args.PackageSource,
        };

        NugetInfo info = await _nugetInfoProvider.FetchNugetInfoAsync(request, cancellationToken).ConfigureAwait(false);

        return BuildLinkErrorCode.Success;
    }
}
