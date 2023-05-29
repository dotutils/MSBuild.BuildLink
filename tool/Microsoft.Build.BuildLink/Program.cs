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
using BuildUtils;
using System.Reflection;
using Microsoft.Build.BuildLink.CommandExecutionUtils;
using Microsoft.Extensions.Logging;
using ICommandExecutor = Microsoft.Build.BuildLink.CommandExecutionUtils.ICommandExecutor;

namespace Microsoft.Build.BuildLink
{
    internal sealed class Program
    {
        // TODO: detect submodules in source fetcher (plus not implemented exception for recurse submodules option)
        // TODO: detect global.json - and add as part of build descriptor
        
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
                        services.AddSingleton<SourcePackageCommandHandler>();
                        services.AddSingleton<IStderrWriter, DefaultStderrWriter>();
                        services.AddSingleton<IStdoutWriter, DefaultStdoutWriter>();
                        services.AddSingleton<INugetInfoProvider, NugetInfoProvider>();
                        services.AddSingleton<IPackageSourcesProvider, DotnetCommandPackageSourcesProvider>();
                        services.AddSingleton<IEnvironment, DefaultEnvironment>();
                        services.AddSingleton<IO.IFileSystem, IO.PhysicalFileSystem>();
                        services.AddSingleton<ISourceFetcher, SourceFetcher>();
                        services.AddSingleton<IFileSystemHelper, FileSystemHelper>();
                        services.AddSingleton<IBuildDescriptorSerializer, BuildDescriptorSerializer>();
                        services.AddSingleton<IBuildDescriptionFinder, BuildDescriptionFinder>();
                        services.AddSingleton<IFileStreamFactory, FileStreamFactory>();
                        services.AddSingleton<ICommandExecutor, CommandExecutor>();
                    })
                    .AddCancellationTokenProvider()
                    .ConfigureLogging(logging =>
                    {
                        logging.ConfigureBuildLinkLogging(host);
                    });
                })
                .UseExceptionHandler(ExceptionHandler)
                .UseParseErrorReporting((int) BuildLinkErrorCode.InvalidOption)
                .CancelOnProcessTermination()
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
            root.AddCommand(new SourcePackageCommand());
            root.AddGlobalOption(CommonOptionsExtension.s_consoleVerbosityOption);
            root.AddGlobalOption(CommonOptionsExtension.s_fileVerbosityOption);

            return new CommandLineBuilder(root);
        }

        private static void ExceptionHandler(Exception exception, InvocationContext context)
        {
            if (exception is TargetInvocationException)
            {
                exception = exception.InnerException ?? exception;
            }

            ILogger? logger = context.BindingContext.GetService<ILogger<Program>>();
            logger.LogCritical( exception,"Unhandled exception occurred ({type})", exception.GetType());
            context.ExitCode = (int)BuildLinkErrorCode.InternalError;
        }
    }
}
