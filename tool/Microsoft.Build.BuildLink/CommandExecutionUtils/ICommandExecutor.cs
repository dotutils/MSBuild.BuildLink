// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Build.BuildLink.CommandExecutionUtils;

internal interface ICommandExecutor
{
    public CommandResult Execute(string command, IEnumerable<string>? args);
    public CommandResult Execute(string command, string workingDir, IEnumerable<string>? args);
}
