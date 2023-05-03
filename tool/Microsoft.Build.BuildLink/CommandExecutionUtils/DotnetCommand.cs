// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
//using Xunit.Abstractions;

namespace Microsoft.Build.BuildLink.CommandExecutionUtils
{
    internal abstract class ExecutableCommand : TestCommand
    {
        protected abstract string ExecutableFilePath { get; }

        internal ExecutableCommand(ILogger log) : base(log)
        { }

        //internal ExecutableCommand(ITestOutputHelper log) : base(log)
        //{ }

        private protected override SdkCommandSpec CreateCommand(IEnumerable<string> args)
        {
            var sdkCommandSpec = new SdkCommandSpec()
            {
                FileName = ExecutableFilePath,
                Arguments = args.ToList(),
                WorkingDirectory = WorkingDirectory
            };
            return sdkCommandSpec;
        }
    }

    internal class PowershellCommand : ExecutableCommand
    {
        internal PowershellCommand(ILogger log, string scriptPath, params string[] args) : base(log)
        {
            Arguments.Add("&");
            Arguments.Add(scriptPath);
            Arguments.AddRange(args);
        }

        //internal PowershellCommand(ITestOutputHelper log, string scriptPath, params string[] args) : base(log)
        //{
        //    Arguments.Add("&");
        //    Arguments.Add(scriptPath);
        //    Arguments.AddRange(args);
        //}

        protected override string ExecutableFilePath => "powershell.exe";
    }

    internal class DotnetCommand : ExecutableCommand
    {
        private string _executableFilePath = "dotnet";

        internal DotnetCommand(ILogger log, string subcommand, params string[] args) : base(log)
        {
            Arguments.Add(subcommand);
            Arguments.AddRange(args);
        }

        //internal DotnetCommand(ITestOutputHelper log, string subcommand, params string[] args) : base(log)
        //{
        //    Arguments.Add(subcommand);
        //    Arguments.AddRange(args);
        //}

        internal DotnetCommand WithoutTelemetry()
        {
            WithEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "true");
            return this;
        }

        internal DotnetCommand WithCustomExecutablePath(string? executableFilePath)
        {
            if (!string.IsNullOrEmpty(executableFilePath))
            {
                _executableFilePath = executableFilePath;
            }
            return this;
        }

        protected override string ExecutableFilePath => _executableFilePath;
    }
}
