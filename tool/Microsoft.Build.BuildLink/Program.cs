// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine.Hosting;
using System;
using Microsoft.Build.BuildLink.Commands;
using static Microsoft.Build.BuildLink.Program;

namespace Microsoft.Build.BuildLink
{
    internal sealed class Program
    {
        static Task<int> Main(string[] args)
        {
            return BuildCommandLine()
                .UseHost(
                _ => Host.CreateDefaultBuilder(),
                host =>
                {
                    host.ConfigureServices(services =>
                    {
                        services.AddSingleton<IFoo, Foo2>();
                    });
                    host.ConfigureLogging(logging =>
                    {
                        logging.AddConsole();
                        var logLevel = (host.Properties[typeof(InvocationContext)] as InvocationContext).ParseResult.GetVerbosityOption().ToLogLevel();
                        logging.SetMinimumLevel(logLevel);
                    });
                })
                .UseHelp()
                .UseDefaults()
                .EnablePosixBundling(true)
                .Build()
                .InvokeAsync(args);
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            var root = new RootCommand("build-link - provides commands for turning NuGets into sources and for managing .NET build metadata");
            root.AddCommand(new TestCommand());
            root.AddCommand(new GetSourcesCommand());
            root.AddCommand(new AddBuildMetadataCommand());
            root.AddGlobalOption(CommonOptionsExtension.s_verbosityOption);

            return new CommandLineBuilder(root);
        }
    }
}
