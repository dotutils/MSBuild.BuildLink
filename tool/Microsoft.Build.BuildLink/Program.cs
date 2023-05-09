// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.CommandLine;
using Microsoft.Extensions.Hosting;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine.Hosting;
using Microsoft.Build.BuildLink.Commands;
using Microsoft.Build.BuildLink.NuGet;
using Microsoft.Build.BuildLink.Reporting;
using Microsoft.Build.BuildLink.SourceCodes;

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
                        services.AddSingleton<GetSourcesCommandHandler>();
                        services.AddSingleton<AddBuildMetadataCommandHandler>();
                        services.AddSingleton<IStderrWriter, DefaultStderrWriter>();
                        services.AddSingleton<INugetInfoProvider, NugetInfoProvider>();
                        services.AddSingleton<IPackageSourcesProvider, DotnetCommandPackageSourcesProvider>();
                        services.AddSingleton<IEnvironment, DefaultEnvironment>();
                        services.AddSingleton<IO.IFileSystem, IO.PhysicalFileSystem>();
                        services.AddSingleton<ISourceFetcher, SourceFetcher>();

                    })
                    .ConfigureLogging(logging =>
                    {
                        logging.ConfigureBuildLinkLogging(host);
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

            root.AddCommand(new GetSourcesCommand());
            root.AddCommand(new AddBuildMetadataCommand());
            root.AddGlobalOption(CommonOptionsExtension.s_consoleVerbosityOption);
            root.AddGlobalOption(CommonOptionsExtension.s_fileVerbosityOption);

            return new CommandLineBuilder(root);
        }
    }
}
