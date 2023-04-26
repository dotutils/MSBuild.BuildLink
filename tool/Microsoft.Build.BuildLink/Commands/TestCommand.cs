using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Build.BuildLink.Commands;

internal class TestCommand : ExecutableCommand<TestCommandArgs>
{
    private const string CommandName = "test";

    private readonly Argument<string> _packageNameArgument = new("package-name")
    {
        Description = "Name of the package",
        // 0 for case where only path is specified
        Arity = new ArgumentArity(1, 1)
    };

    //private readonly Option<string> _packageVersionOption = new(new[] { "-v", "--version" })
    //{
    //    Description = "Version of the package",
    //};

    private readonly Option<int> _packageVersionOption = new(new[] { "-v", "--version" })
    {
        Description = "Version of the package",
    };

    public TestCommand()
        : base(CommandName, "Some help string for test command")
    {
        AddArgument(_packageNameArgument);
        AddOption(_packageVersionOption);
    }

    protected override async Task<int> ExecuteAsync(
        TestCommandArgs args,
        //ILoggerFactory loggerFactory,
        IHost host,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ILogger logger = host.Services.GetRequiredService<ILogger<TestCommand>>();// loggerFactory.CreateLogger<TestCommand>();
        logger.LogInformation("Running the test of {name}.", args.Name);
        logger.LogInformation("Ver: {version}.", args.Value);
        logger.LogInformation("Injected: {foo}.", host.Services.GetRequiredService<IFoo>().Foo);

        return 0;
    }

    protected internal override TestCommandArgs ParseContext(ParseResult parseResult)
    {
        return new TestCommandArgs(
            parseResult.GetValueForArgument(_packageNameArgument),
            parseResult.GetValueForOption(_packageVersionOption)
        );
    }
}
