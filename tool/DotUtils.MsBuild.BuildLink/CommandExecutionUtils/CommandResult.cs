// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace DotUtils.MsBuild.BuildLink.CommandExecutionUtils
{
    internal readonly struct CommandResult
    {
        internal static readonly CommandResult Empty;

        internal CommandResult(ProcessStartInfo startInfo, int exitCode, string? stdOut, string? stdErr)
        {
            StartInfo = startInfo;
            ExitCode = exitCode;
            StdOut = stdOut;
            StdErr = stdErr;
        }

        internal ProcessStartInfo StartInfo { get; }

        internal int ExitCode { get; }

        internal string? StdOut { get; }

        internal string? StdErr { get; }

        public override string ToString() => $"""
Exit Code: {ExitCode}
Command: {StartInfo.FileName} Args: {string.Join(' ', StartInfo.ArgumentList)}
Std Out:
=================================
{StdOut}
=================================
Std Err:
=================================
{StdErr}
=================================
""";
    }
}
