using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Build.BuildLink.Commands;

internal class AddBuildMetadataCommand : ExecutableCommand<AddBuildMetadataCommandArgs>
{
    private const string CommandName = "add-build-metadata";

    private readonly Argument<string> _repoRootPathArgument = new("repo-root")
    {
        Description = "Path to the root of local repository where the build-link.json will be placed",
        // 0 for case where only path is specified
        Arity = new ArgumentArity(1, 1)
    };

    private readonly Option<bool> _overwriteOption = new(new[] { "-f", "--force" })
    {
        Description = "Overwrite existing file",
    };

    public AddBuildMetadataCommand()
        : base(CommandName, "Inspects a local repository and creates a build-link.json build descriptive metadata file")
    {
        AddArgument(_repoRootPathArgument);
        AddOption(_overwriteOption);
    }
    protected override async Task<int> ExecuteAsync(
        AddBuildMetadataCommandArgs args,
        IHost host,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ILogger logger = host.Services.GetRequiredService<ILogger<TestCommand>>();// loggerFactory.CreateLogger<TestCommand>();
        logger.LogInformation("Running the test of {name}.", args.RepoRootPath);
        logger.LogInformation("Ver: {version}.", args.OverwriteExisting);
        logger.LogInformation("Injected: {foo}.", host.Services.GetRequiredService<IFoo>().Foo);

        return 0;
    }

    protected internal override AddBuildMetadataCommandArgs ParseContext(ParseResult parseResult)
    {
        return new AddBuildMetadataCommandArgs(
            parseResult.GetValueForArgument(_repoRootPathArgument),
            parseResult.GetValueForOption(_overwriteOption)
        );
    }
}
