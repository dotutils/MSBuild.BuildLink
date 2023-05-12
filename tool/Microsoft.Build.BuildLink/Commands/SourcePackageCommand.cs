// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.BuildLink.Reporting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Build.BuildLink.Commands;

internal class SourcePackageCommand : ExecutableCommand<SourcePackageCommandArgs, SourcePackageCommandHandler>
{
    private const string CommandName = "source-package";

    private readonly Argument<string> _projectPathArgument = new("project")
    {
        Description = "Path to the project or solution that will be inspected and transformed",
        // 0 for case where only path is specified
        Arity = new ArgumentArity(1, 1)
    };

    private readonly Option<bool> _overwriteOption = new(new[] { "-f", "--force" })
    {
        Description = "Overwrite existing file",
    };

    public SourcePackageCommand()
        : base(CommandName, "Converts selected PackageReference into ProjectReference")
    {
        AddArgument(_projectPathArgument);
    }

    protected internal override SourcePackageCommandArgs ParseContext(ParseResult parseResult)
    {
        return new SourcePackageCommandArgs();
    }
}

internal class SourcePackageCommandHandler : ICommandExecutor<SourcePackageCommandArgs>
{
    private readonly ILogger<SourcePackageCommandHandler> _logger;

    public SourcePackageCommandHandler(ILogger<SourcePackageCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<BuildLinkErrorCode> ExecuteAsync(SourcePackageCommandArgs args, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Running the source-package command");

        throw new BuildLinkException($"Command is not yet implemented", BuildLinkErrorCode.NotYetImplementedScenario);

        return BuildLinkErrorCode.Success;
    }
}
