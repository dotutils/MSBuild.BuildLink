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
using LibGit2Sharp;
using Microsoft.Build.BuildLink.Commands;
using Microsoft.Build.BuildLink.NuGet;
using Microsoft.Extensions.Logging.Console;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Microsoft.Build.BuildLink.Reporting;
using Microsoft.Build.BuildLink.SourceCodes;

namespace Microsoft.Build.BuildLink
{

    //TODOs: add ability to log to file (logging.AddFile), somehow separate verbosity for console???
    //  - probably flipping between file and console logger
    //  - quiet adds null logger
    //TODO: the build script finder implementation

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

                    });
                    host.ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();

                        ParseResult parseResult = (host.Properties[typeof(InvocationContext)] as InvocationContext).ParseResult;

                        var consoleLogLevel = parseResult.GetConsoleVerbosityOption().ToLogLevel();
                        var fileLogLevel = parseResult.GetFileVerbosityOption().ToLogLevel();
                        
                        if (consoleLogLevel < LogLevel.None)
                        {
                            logging.AddConsole();
                        }

                        if (fileLogLevel < LogLevel.None)
                        {
                            var loggingSection = FetchConfiguration()?.GetSection("Logging");
                            if (loggingSection != null)
                            {
                                logging.AddFile(loggingSection);
                            }
                            else
                            {
                                logging.AddFile("build-link.log", cfg =>
                                {
                                    cfg.Append = true;
                                    cfg.MinLevel = fileLogLevel;
                                    cfg.FileSizeLimitBytes = 10000;
                                    cfg.MaxRollingFiles = 3;
                                });
                            }
                        }

                        var minLevel = (LogLevel)Math.Min((int)consoleLogLevel, (int)fileLogLevel);
                        logging.SetMinimumLevel(minLevel);
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

        private static IConfigurationRoot? FetchConfiguration()
        {
            var currentDir = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(currentDir, "appsettings.json")))
            {
                currentDir = Directory.GetParent(currentDir)?.FullName;
                if (string.IsNullOrWhiteSpace(currentDir))
                {
                    return null;
                }
            }

            return new ConfigurationBuilder()
                .SetBasePath(currentDir)
                .AddJsonFile("appsettings.json", false)
                .Build();
        }
    }
}
