using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Threading;
using Microsoft.Build.BuildLink.IO;
using Microsoft.Build.BuildLink.NuGet;
using Microsoft.Build.BuildLink.Reporting;
using Microsoft.Build.BuildLink.SourceCodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Build.BuildLink.Commands;

internal class AddBuildMetadataCommand : ExecutableCommand<AddBuildMetadataCommandArgs, AddBuildMetadataCommandHandler>
{
    private const string CommandName = "add-build-metadata";

    private readonly Argument<string[]> _packageNameArgument = new("package-name")
    {
        Description = "Name of the package for which the build descriptor file should be generated. Multiple values allowed.",
        // 0 for case of empty template
        Arity = new ArgumentArity(0, 1000)
    };

    private readonly Option<string> _sourcesRootOption = new(new[] { "-s", "--sources" })
    {
        Description = "Location of the sources that should be inspected. Defaults to current directory.",
    };

    private readonly Option<string> _buildFilePathOption = new(new[] { "-b", "--build-metadata-file" })
    {
        Description = "Path to the build descriptor file to be generated. Relative path resolved relative to repository root. Filename is assumed to be part of the argument. Defaults to 'buildlink.json'",
    };

    private readonly Option<string> _libFileOption = new(new[] { "-l", "--lib" })
    {
        Description = "Lib file name (without extension), for which we want to filter the returned build descriptor file. If not supplied - all lib files are assumed.",
    };

    private readonly Option<bool> _generateSampleTemplateOption = new(new[] { "-t", "--template" })
    {
        Description = "Generate sample build descriptor file",
    };

    private readonly Option<bool> _overwriteOption = new(new[] { "-f", "--force" })
    {
        Description = "Overwrite existing file",
    };



    public AddBuildMetadataCommand()
        : base(CommandName, "Inspects a local repository and creates a build descriptive metadata file (https://aka.ms/build-link/buildlink.json).")
    {
        AddArgument(_packageNameArgument);
        AddOption(_sourcesRootOption);
        AddOption(_buildFilePathOption);
        AddOption(_libFileOption);
        AddOption(_generateSampleTemplateOption);
        AddOption(_overwriteOption);
    }

    protected internal override AddBuildMetadataCommandArgs ParseContext(ParseResult parseResult)
    {
        return new AddBuildMetadataCommandArgs(
            parseResult.GetValueForArgument(_packageNameArgument),
            parseResult.GetValueForOption(_sourcesRootOption),
            parseResult.GetValueForOption(_buildFilePathOption),
            parseResult.GetValueForOption(_libFileOption),
            parseResult.GetValueForOption(_generateSampleTemplateOption),
            parseResult.GetValueForOption(_overwriteOption)
        );
    }
}

internal class AddBuildMetadataCommandHandler : ICommandExecutor<AddBuildMetadataCommandArgs>
{
    private readonly ILogger<AddBuildMetadataCommandHandler> _logger;
    private readonly INugetInfoProvider _nugetInfoProvider;
    private readonly ISourceFetcher _sourceFetcher;
    private readonly IBuildDescriptionFinder _buildDescriptionFinder;
    private readonly IBuildDescriptorSerializer _buildDescriptorSerializer;
    private readonly IStdoutWriter _stdoutWriter;
    private readonly IFileStreamFactory _streamFactory;
    private readonly IFileSystem _fileSystem;

    public AddBuildMetadataCommandHandler(
        ILogger<AddBuildMetadataCommandHandler> logger,
        INugetInfoProvider nugetInfoProvider,
        ISourceFetcher sourceFetcher,
        IBuildDescriptionFinder buildDescriptionFinder,
        IBuildDescriptorSerializer buildDescriptorSerializer,
        IStdoutWriter stdoutWriter,
        IFileStreamFactory streamFactory,
        IFileSystem fileSystem)
    {
        _logger = logger;
        _nugetInfoProvider = nugetInfoProvider;
        _sourceFetcher = sourceFetcher;
        _buildDescriptionFinder = buildDescriptionFinder;
        _buildDescriptorSerializer = buildDescriptorSerializer;
        _stdoutWriter = stdoutWriter;
        _streamFactory = streamFactory;
        _fileSystem = fileSystem;
    }

    public async Task<BuildLinkErrorCode> ExecuteAsync(AddBuildMetadataCommandArgs args, CancellationToken cancellationToken)
    {
        string repoRoot = args.RepoRootPath ?? ".";
        string destinationFile = Path.Combine(repoRoot, args.BuildlinkJsonPath ?? "buildlink.json");

        if (!(args.OverwriteExisting ?? false) && _fileSystem.FileExists(destinationFile))
        {
            throw new BuildLinkException($"Attempt to write to existing file [{destinationFile}] without the --force flag.", BuildLinkErrorCode.FileSystemWriteFailed);
        }

        WorkingCopyBuildDescriptor buildDescriptor;
        if (args.GenerateEmptyTemplate ?? false)
        {
            buildDescriptor = _buildDescriptionFinder.CreateSample();
        }
        else
        {
            buildDescriptor = GenerateDescriptor(repoRoot, args, cancellationToken);
        }

        await _buildDescriptorSerializer.WriteToFile(destinationFile, buildDescriptor, cancellationToken)
            .ConfigureAwait(false);
        
        return BuildLinkErrorCode.Success;
    }

    private WorkingCopyBuildDescriptor GenerateDescriptor(string repoRoot, AddBuildMetadataCommandArgs args, CancellationToken token)
    {
        if (args.PackageName == null || args.PackageName.Length == 0)
        {
            throw new BuildLinkException($"Inferring package names from sources is not yet supported.", BuildLinkErrorCode.NotYetImplementedScenario);
        }

        if (args.PackageName.Length > 1)
        {
            throw new BuildLinkException($"Multiple package names are not yet supported.", BuildLinkErrorCode.NotYetImplementedScenario);
        }

        return _buildDescriptionFinder.DiscoverBuildFiles(repoRoot, args.PackageName[0],
            string.IsNullOrEmpty(args.LibFile) ? Array.Empty<string>() : new string[] { args.LibFile }, token);
    }
}
