using System.CommandLine;
using System.CommandLine.Parsing;
using System.Runtime;
using DotUtils.MsBuild.BuildLink.NuGet;
using DotUtils.MsBuild.BuildLink.Reporting;
using DotUtils.MsBuild.BuildLink.SourceCodes;
using DotUtils.MsBuild.BuildLink.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NuGet.Common;

namespace DotUtils.MsBuild.BuildLink.Commands;

internal class GetSourcesCommand : ExecutableCommand<GetSourcesCommandArgs, GetSourcesCommandHandler>
{
    private const string CommandName = "get-sources";

    private readonly Argument<string> _packageNameArgument = new("package-name")
    {
        Description = "Name of the package",
        // 0 for case where only path is specified
        Arity = new ArgumentArity(1, 1)
    };

    private readonly Option<string> _packageVersionOption = new(new[] { "-ver", "--version" })
    {
        Description = "Version of the package",
    };

    private readonly Option<string> _libFileOption = new(new[] { "-l", "--lib" })
    {
        Description = "Lib file name (without extension), for which we want to return the MSBuild project file. If not supplied - all lib files are inspected.",
    };

    private readonly Option<string> _buildFilePathOption = new(new[] { "-b", "--build-metadata-file" })
    {
        Description = "Path to the build descriptor file. Relative path resolved relative to repository root. Absolute path allows to use local file not located in repository",
    };

    private readonly Option<bool> _allowPrereleaseOption = new(new[] { "--include-prerelease" })
    {
        Description = "Allow prerelease version to be used",
    };

    private readonly Option<bool> _ignoreBuildLinkJsonOption = new(new[] { "--ignore-buildlink-json" })
    {
        Description = "Ignore any available buildlink.json file and perform repo search instead.",
    };

    private readonly Option<string> _packageSourceOption = new(new[] { "--feed" })
    {
        Description = "Package source feed to be used to pull the nuget",
    };

    private readonly Option<string> _sourceCodeRootOption = new(new[] { "-d", "--destination" })
    {
        Description = "Local destination folder for fetching sources. If exists, proper repository and version of codes will be validated/checked-out.",
    };

    private readonly Option<string> _sourceCodebaseDirOption = new(new[] { "--checkout-base-dir" })
    {
        Description = "Local destination folder for fetching sources - the name and version of package is appended (as subdir).",
        IsHidden = true,
    };

    private readonly Option<string> _flushBuildJsonPathOption = new(new[] { "--flush-buildlink-path" })
    {
        Description = "Local destination file to flush the generated buildlink.json",
    };

    private readonly Option<bool> _buildJsonAppendOption = new(new[] { "--buildlink-append" })
    {
        Description = "The specified local destination file to flush the generated buildlink.json should be appended if exists",
    };

    public GetSourcesCommand()
        : base(CommandName, "Fetches the sources for the given package")
    {
        AddArgument(_packageNameArgument);
        AddOption(_packageVersionOption);
        AddOption(_packageSourceOption);
        AddOption(_allowPrereleaseOption);
        AddOption(_sourceCodeRootOption);
        AddOption(_libFileOption);
        AddOption(_buildFilePathOption);
        AddOption(_ignoreBuildLinkJsonOption);
        AddOption(_flushBuildJsonPathOption);
        AddOption(_buildJsonAppendOption);
        AddOption(_sourceCodebaseDirOption);
    }

    protected internal override GetSourcesCommandArgs ParseContext(ParseResult parseResult)
    {
        return new GetSourcesCommandArgs(
            parseResult.GetValueForArgument(_packageNameArgument),
            parseResult.GetValueForOption(_packageVersionOption),
            parseResult.GetValueForOption(_buildFilePathOption),
            parseResult.GetValueForOption(_ignoreBuildLinkJsonOption),
            parseResult.GetValueForOption(_allowPrereleaseOption),
            parseResult.GetValueForOption(_packageSourceOption),
            parseResult.GetValueForOption(_sourceCodeRootOption),
            parseResult.GetValueForOption(_sourceCodebaseDirOption),
            parseResult.GetValueForOption(_libFileOption),
            parseResult.GetValueForOption(_flushBuildJsonPathOption),
            parseResult.GetValueForOption(_buildJsonAppendOption)
        );
    }
}

internal class GetSourcesCommandHandler : ICommandExecutor<GetSourcesCommandArgs>
{
    private readonly ILogger<GetSourcesCommandHandler> _logger;
    private readonly INugetInfoProvider _nugetInfoProvider;
    private readonly ISourceFetcher _sourceFetcher;
    private readonly IBuildDescriptionFinder _buildDescriptionFinder;
    private readonly IBuildDescriptorSerializer _buildDescriptorSerializer;
    private readonly IStdoutWriter _stdoutWriter;
    private readonly IFileStreamFactory _streamFactory;

    public GetSourcesCommandHandler(
        ILogger<GetSourcesCommandHandler> logger,
        INugetInfoProvider nugetInfoProvider,
        ISourceFetcher sourceFetcher,
        IBuildDescriptionFinder buildDescriptionFinder,
        IBuildDescriptorSerializer buildDescriptorSerializer,
        IStdoutWriter stdoutWriter,
        IFileStreamFactory streamFactory)
    {
        _logger = logger;
        _nugetInfoProvider = nugetInfoProvider;
        _sourceFetcher = sourceFetcher;
        _buildDescriptionFinder = buildDescriptionFinder;
        _buildDescriptorSerializer = buildDescriptorSerializer;
        _stdoutWriter = stdoutWriter;
        _streamFactory = streamFactory;
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

        string[] assembliesExtensions = new[] { ".exe", ".dll" };
        List<string> assemblyNames = info.LibFiles
            .Where(f => assembliesExtensions.Contains(Path.GetExtension(f), StringComparer.CurrentCultureIgnoreCase))
            .Select(Path.GetFileNameWithoutExtension)
            .GroupBy(n => n)
            .Select(g => g.Key!)
            .ToList();

        string? filterByLibFile = args.LibFile;
        if (!string.IsNullOrEmpty(filterByLibFile))
        {
            filterByLibFile = filterByLibFile.Trim().RemoveAnyFromEnd(assembliesExtensions);
            if (!assemblyNames.Contains(filterByLibFile, StringComparer.CurrentCultureIgnoreCase))
            {
                throw new BuildLinkException(
                    $"Requested lib file [{args.LibFile}], is not present amongst discovered lib assemblies of the '{args.PackageName}' package. Discovered lib assemblies: {string.Join(',', assemblyNames)}",
                    BuildLinkErrorCode.InvalidOption);
            }
            assemblyNames = new List<string>() { filterByLibFile };
        }

        string sourcesDestinationDir = args.SourcesCodesDownloadRoot ?? $"{args.PackageName}.{info.Version}";
        sourcesDestinationDir = Path.Combine(args.SourcesCheckoutBasePath ?? string.Empty, sourcesDestinationDir);

        cancellationToken.ThrowIfCancellationRequested();
        _sourceFetcher.FetchRepository(info.Repository, sourcesDestinationDir);
        cancellationToken.ThrowIfCancellationRequested();

        WorkingCopyBuildDescriptor? buildDescriptor = null;
        bool descriptorFileUsed = true;
        if (!args.IgnoreBuildlinkJson)
        {
            buildDescriptor = await _buildDescriptionFinder.GetBuildDescriptorAsync(sourcesDestinationDir,
                    args.SupplementalBuildMetadataFilePath, args.PackageName, filterByLibFile, cancellationToken).ConfigureAwait(false);
        }

        if (buildDescriptor == null)
        {
            descriptorFileUsed = false;
            buildDescriptor = _buildDescriptionFinder.DiscoverBuildFiles(sourcesDestinationDir, args.PackageName, assemblyNames, cancellationToken);
        }

        string descriptorString = _buildDescriptorSerializer.WriteToString(buildDescriptor);

        if (!descriptorFileUsed && buildDescriptor.HasNonMsBuildInfo())
        {
            _logger.LogWarning("The buildlink.json file was not provided, nor found in the repository, however it seems there might be some source or/and build initialization steps needed, that can be captured within the buildlink.json. Run 'add-build-metadata' subcommand to create suggested file or use the output of this command or refer to https://aka.ms/build-link/buildlink.json for more details.");
            _logger.LogDebug("Build Descriptor (buildlink.json) initially discovered content:");
            _logger.LogDebug(descriptorString);
        }

        IStdoutWriter writer = _stdoutWriter;
        IStdoutWriter? delimiterWriter = _stdoutWriter;
        if (!string.IsNullOrEmpty(args.FlushBuildJsonPath))
        {
            writer = _streamFactory.CreateStreamWriter<IStdoutWriter>(args.FlushBuildJsonPath, args.AppendBuildJsonOnFlush ? IO.FileCreateOptions.Append : IO.FileCreateOptions.ThrowIfExists);
            delimiterWriter = null;
        }

        delimiterWriter?.WriteLine("----------------------------- Build description info -----------------------------");
        writer.WriteLine(_buildDescriptorSerializer.PrependProperty(descriptorString, "RepoRoot",  Path.GetFullPath(sourcesDestinationDir)));
        delimiterWriter?.WriteLine("----------------------------------------------------------------------------------");
        writer.Dispose();

        return BuildLinkErrorCode.Success;
    }
}
