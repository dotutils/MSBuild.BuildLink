// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Build.BuildLink.CommandExecutionUtils;

internal class CommandExecutor : ICommandExecutor
{
    private ILogger<CommandExecutor> _logger;

    public CommandExecutor(ILogger<CommandExecutor> logger)
    {
        _logger = logger;
    }

    public CommandResult Execute(string command, IEnumerable<string>? args)
        => Execute(command, string.Empty, args); 

    public CommandResult Execute(string command, string workingDir, IEnumerable<string>? args)
    {
        ExecutableCommand ec = new ExecutableCommand(_logger);
        if (!string.IsNullOrEmpty(workingDir))
        {
            ec.WorkingDirectory = workingDir;
        }

        if (args != null)
        {
            ec.Arguments.AddRange(args);
        }

        return ec.Execute();
    }
}
