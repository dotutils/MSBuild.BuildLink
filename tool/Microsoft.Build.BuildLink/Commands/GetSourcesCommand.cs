using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Build.BuildLink.Commands;

internal class GetSourcesCommand : ExecutableCommand<GetSourcesCommandArgs>
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
    protected override async Task<int> ExecuteAsync(
        GetSourcesCommandArgs args,
        IHost host,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ILogger logger = host.Services.GetRequiredService<ILogger<TestCommand>>();// loggerFactory.CreateLogger<TestCommand>();
        logger.LogInformation("Running the test of {name}.", args.PackageName);
        logger.LogInformation("Ver: {version}.", args.PackageVersion);
        logger.LogInformation("Injected: {foo}.", host.Services.GetRequiredService<IFoo>().Foo);

        return 0;
    }

    protected internal override GetSourcesCommandArgs ParseContext(ParseResult parseResult)
    {
        return new GetSourcesCommandArgs(
            parseResult.GetValueForArgument(_packageNameArgument),
            parseResult.GetValueForOption(_packageVersionOption)
        );
    }
}
