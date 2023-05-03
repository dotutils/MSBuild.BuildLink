using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.Build.BuildLink.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Build.BuildLink.Commands;

internal class TestCommand : ExecutableCommand<TestCommandArgs, TestCommandHandler>
{
    private const string CommandName = "test";

    private readonly Argument<string> _packageNameArgument = new("package-name")
    {
        Description = "Name of the package",
        // 0 for case where only path is specified
        Arity = new ArgumentArity(1, 1)
    };

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

    protected internal override TestCommandArgs ParseContext(ParseResult parseResult)
    {
        return new TestCommandArgs(
            parseResult.GetValueForArgument(_packageNameArgument),
            parseResult.GetValueForOption(_packageVersionOption)
        );
    }
}

internal class TestCommandHandler : ICommandExecutor<TestCommandArgs>
{
    private readonly ILogger<TestCommandHandler> _logger;
    private readonly IFoo _foo;
    private readonly IServiceProvider _serviceProvider;

    public TestCommandHandler(ILogger<TestCommandHandler> logger, IFoo foo, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _foo = foo;
        _serviceProvider = serviceProvider;
    }

    public async Task<BuildLinkErrorCode> ExecuteAsync(TestCommandArgs args, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        //ILogger logger = host.Services.GetRequiredService<ILogger<TestCommand>>();// loggerFactory.CreateLogger<TestCommand>();
        _logger.LogInformation("Running the test of {name}.", args.Name);
        _logger.LogInformation("Ver: {version}.", args.Value);
        _logger.LogInformation("Injected: {foo}.", _foo.Foo);
        _logger.LogInformation("Injected, retrieved indirectly: {foo}.", _serviceProvider.GetRequiredService<IFoo>().Foo);


        return BuildLinkErrorCode.Success;
    }
}




