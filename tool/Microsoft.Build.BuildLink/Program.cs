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
using System.CommandLine.NamingConventionBinder;
using Microsoft.Build.BuildLink.Commands;
using static Microsoft.Build.BuildLink.Program;
using NuGet.Protocol.Core.Types;
using NuGet.Configuration;
using NuGet.Protocol;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.BuildLink.NuGet;
using Microsoft.Extensions.Logging.Console;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using NuGetPe;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using ManifestUtility = Microsoft.Build.BuildLink.NuGet.ManifestUtility;
using Microsoft.Build.BuildLink.Reporting;

namespace Microsoft.Build.BuildLink
{
    internal sealed class Program
    {
        //static async Task<int> Main(string[] args)
        //{
        //    CancellationToken token = default;
        //    string packageName = "Newtonsoft.Json";
        //    string version = null;//"13.0.3";
        //    bool allowPrerelease = false;
        //    //from dotnet  nuget list source
        //    string packageSource = "https://api.nuget.org/v3/index.json";

        //    // create a logger
        //    var loggerFactory = LoggerFactory.Create(
        //        builder => builder
        //            // add console as logging target
        //            .AddConsole()
        //            // add debug output as logging target
        //            .AddDebug()
        //            // set minimum level to log
        //            .SetMinimumLevel(LogLevel.Debug)
        //    );
        //    //var logger = loggerFactory.CreateLogger<Program>();
        //    var nugetLogger = new NuGetLogger(loggerFactory);


        //    {
        //        SourceRepository repository1 = Repository.Factory.GetCoreV3("packages", FeedType.FileSystemV2);
        //        FindPackageByIdResource resource1 = await repository1.GetResourceAsync<FindPackageByIdResource>(token).ConfigureAwait(false);
        //        SourceCacheContext cacheSettings1 = new SourceCacheContext();

        //        var downloader1 = await resource1.GetPackageDownloaderAsync(
        //            new PackageIdentity(packageName, NuGetVersion.Parse("13.0.3")),
        //            cacheSettings1,
        //            nugetLogger,
        //            token).ConfigureAwait(false);

        //        var items1 = await downloader1.ContentReader.GetLibItemsAsync(token).ConfigureAwait(false);
        //        foreach (FrameworkSpecificGroup group in items1)
        //        {
        //            Console.WriteLine(
        //                $"{group.TargetFramework.ToString()}, empty: {group.HasEmptyFolder}, items: {string.Join(',', group.Items)}");
        //        }
        //    }

        //    PackageSource source = new PackageSource(packageSource);
        //    SourceRepository repository = Repository.Factory.GetCoreV3(source);
        //    FindPackageByIdResource resource;
        //    resource = await repository.GetResourceAsync<FindPackageByIdResource>(token).ConfigureAwait(false);
        //    SourceCacheContext cacheSettings = new SourceCacheContext();

        //    NuGetVersion nuGetVersion;
        //    if (version == null)
        //    {
        //        var res = await resource.GetAllVersionsAsync(packageName, cacheSettings, nugetLogger, token)
        //            .ConfigureAwait(false);
        //        if (!allowPrerelease)
        //        {
        //            res = res.Where(v => !v.IsPrerelease);
        //        }

        //        nuGetVersion = res.Max();
        //    }
        //    else
        //    {
        //        NuGetVersion.TryParse(version, out nuGetVersion);
        //    }

        //    //PackageMetadataResource metadataResource = await repository.GetResourceAsync<PackageMetadataResource>(token).ConfigureAwait(false);
        //    //var metadata = await metadataResource
        //    //    .GetMetadataAsync(new PackageIdentity(packageName, nuGetVersion), cacheSettings, nugetLogger, token)
        //    //    .ConfigureAwait(false);

        //    //var url = metadata.ProjectUrl;
        //    //metadata.repo
            
        //    //resource.GetAllVersionsAsync()
        //    var downloader = await resource.GetPackageDownloaderAsync(
        //        new PackageIdentity(packageName, nuGetVersion),
        //        cacheSettings,
        //        nugetLogger,
        //        token).ConfigureAwait(false);

        //    string packagePath = "packages\\Newtonsoft.Json.13.0.3.nupkg";
        //    bool b = await downloader.CopyNupkgFileToAsync(packagePath, token).ConfigureAwait(false);

        //    var items = await downloader.ContentReader.GetLibItemsAsync(token).ConfigureAwait(false);
        //    foreach (FrameworkSpecificGroup group in items)
        //    {
        //        Console.WriteLine(
        //            $"{group.TargetFramework.ToString()}, empty: {group.HasEmptyFolder}, items: {string.Join(',', group.Items)}");
        //    }

        //    downloader.Dispose();

        //    using NuGetPe.ZipPackage zp = new ZipPackage(packagePath);
        //    string m = zp.Repository.Commit;

        //    //using var stream = _streamFactory();
        //    using var reader = new PackageArchiveReader(File.OpenRead(packagePath), false);
        //    using var nuspecStream = reader.GetNuspec();
        //    using var manifestStream = ManifestUtility.ReadManifest(nuspecStream);

        //    var manifest = Manifest.ReadFrom(manifestStream, false);
        //    var metadata = manifest.Metadata;


        //    var url = metadata.Repository.Url;
        //    var commit = metadata.Repository.Commit;

        //    //IPackage
        //    //IPackageMetadata

        //    //var builder = new PackageBuilder(true, nugetLogger);// (str, Path.GetDirectoryName(packagePath));
        //    //var builder = new PackageBuilder(packagePath, null, true);
        //    //var url = builder.Repository.Url;
        //    //var commit = builder.Repository.Commit;


        //    return 5;
        //}

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
                        services.AddSingleton<TestCommandHandler>();
                        services.AddSingleton<GetSourcesCommandHandler>();
                        services.AddSingleton<AddBuildMetadataCommandHandler>();
                        services.AddSingleton<IStderrWriter, DefaultStderrWriter>();
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
