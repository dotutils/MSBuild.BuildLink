using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Build.BuildLink.NuGet;
using Microsoft.Build.BuildLink.Reporting;
using Microsoft.Build.BuildLink.SourceCodes;
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

    private readonly Option<string> _libFileOption = new(new[] { "-l", "--lib" })
    {
        Description = "Lib file name (or prefix), for which we want to return the MSBuild project file. Multiple matches allowed. Any directory separator is normalized. If not supplied - all lib files are inspected.",
    };

    private readonly Option<string> _buildFilePathOption = new(new[] { "-b", "--build-metadata-file" })
    {
        Description = "Path to the build descriptor file. Relative path resolved relative to repository root. Absolute path allows to use local file not located in repository",
    };

    private readonly Option<bool> _allowPrereleaseOption = new(new[] { "--include-prerelease" })
    {
        Description = "Allow prerelease version to be used",
    };

    private readonly Option<string> _packageSourceOption = new(new[] { "--feed" })
    {
        Description = "Package source feed to be used to pull the nuget",
    };

    private readonly Option<string> _sourceCodeRootOption = new(new[] { "-d", "--destination" })
    {
        Description = "Local destination folder for fetching sources. If exists, proper repository and version of codes will be validated/checked-out.",
    };

    public GetSourcesCommand()
        : base(CommandName, "Fetches the sources for the given package")
    {
        AddArgument(_packageNameArgument);
        AddOption(_packageVersionOption);
        AddOption(_buildFilePathOption);
        AddOption(_allowPrereleaseOption);
        AddOption(_packageSourceOption);
        AddOption(_sourceCodeRootOption);
        AddOption(_libFileOption);
    }

    protected internal override GetSourcesCommandArgs ParseContext(ParseResult parseResult)
    {
        return new GetSourcesCommandArgs(
            parseResult.GetValueForArgument(_packageNameArgument),
            parseResult.GetValueForOption(_packageVersionOption),
            parseResult.GetValueForOption(_buildFilePathOption),
            parseResult.GetValueForOption(_allowPrereleaseOption),
            parseResult.GetValueForOption(_packageSourceOption),
            parseResult.GetValueForOption(_sourceCodeRootOption),
            parseResult.GetValueForOption(_libFileOption)
        );
    }
}

internal class GetSourcesCommandHandler : ICommandExecutor<GetSourcesCommandArgs>
{
    private readonly ILogger<GetSourcesCommandHandler> _logger;
    private readonly INugetInfoProvider _nugetInfoProvider;
    private readonly ISourceFetcher _sourceFetcher;

    public GetSourcesCommandHandler(
        ILogger<GetSourcesCommandHandler> logger,
        INugetInfoProvider nugetInfoProvider,
        ISourceFetcher sourceFetcher)
    {
        _logger = logger;
        _nugetInfoProvider = nugetInfoProvider;
        _sourceFetcher = sourceFetcher;
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

        if (info.Repository == null)
        {
            //TODO: fetch from symbols here
            throw new BuildLinkException($"Source code repository metadata not populated for {args.PackageName}",
                BuildLinkErrorCode.NotEnoughInformationToProceed);
        }

        string destinationDir = args.SourcesCodesDownloadRoot ?? $"{args.PackageName}.{info.Version}";

        _sourceFetcher.FetchRepository(info.Repository, destinationDir);

        // get the project file
          // if the build metadata is found - use it
          // if something looking like pre-build is found (script, global.json, submodules, etc.) - suggest adding of build metadata
        // get project for each lib

          return BuildLinkErrorCode.Success;
    }
}
