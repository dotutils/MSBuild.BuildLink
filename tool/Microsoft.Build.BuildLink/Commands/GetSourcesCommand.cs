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
            parseResult.GetValueForOption(_packageVersionOption)
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
        _logger.LogInformation("Running the test of {name}.", args.PackageName);
        _logger.LogInformation("Ver: {version}.", args.PackageVersion);
        //_logger.LogInformation("Injected: {foo}.", host.Services.GetRequiredService<IFoo>().Foo);

        return BuildLinkErrorCode.Success;
    }
}
