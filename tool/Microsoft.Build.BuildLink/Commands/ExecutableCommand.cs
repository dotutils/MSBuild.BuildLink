using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Build.BuildLink.Commands;

internal abstract class ExecutableCommand<TModel> : Command, ICommandHandler where TModel : class
{
    internal ExecutableCommand(string name, string? description = null)
        : base(name, description)
    {
        Handler = this;
    }

    /// <inheritdoc/>
    public async Task<int> InvokeAsync(InvocationContext context)
    {
        ParseResult parseResult = context.ParseResult;
        //var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(/*parseResult.GetVerbosityOption().ToLogLevel()*/ LogLevel.Error);
            builder.AddConsole();
        });
        TModel arguments = ParseContext(parseResult);


        //exceptions are handled by parser itself
        return await ExecuteAsync(arguments, context.GetHost(), context.GetCancellationToken()).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public int Invoke(InvocationContext context) => InvokeAsync(context).GetAwaiter().GetResult();

    /// <summary>
    /// Parses the context from <see cref="ParseResult"/>.
    /// </summary>
    protected internal abstract TModel ParseContext(ParseResult parseResult);

    /// <summary>
    /// Executes the command on the parsed context.
    /// </summary>
    protected abstract Task<int> ExecuteAsync(TModel args, IHost host, CancellationToken cancellationToken);

}
