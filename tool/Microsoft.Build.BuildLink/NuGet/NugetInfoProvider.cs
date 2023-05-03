using Microsoft.Build.BuildLink.Reporting;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Microsoft.Build.BuildLink.NuGet;

internal class NugetInfoProvider : INugetInfoProvider
{
    private readonly IPackageSourcesProvider _packageSourcesProvider;
    private readonly ILogger<NugetInfoProvider> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IEnvironment _environment;
    private readonly IFileSystem _fileSystem;

    public NugetInfoProvider(
        IPackageSourcesProvider packageSourcesProvider,
        ILoggerFactory loggerFactory,
        IEnvironment environment,
        IFileSystem fileSystem)
        => (_packageSourcesProvider, _loggerFactory, _logger, _environment, _fileSystem) =
            (packageSourcesProvider, loggerFactory, loggerFactory.CreateLogger<NugetInfoProvider>(), environment, fileSystem);

    public async Task<NugetInfo> FetchNugetInfoAsync(NugetInfoRequest nugetInfoRequest, CancellationToken token)
    {
        // TODO: try on package that doesn't have repo info

        IEnumerable<string> sources = nugetInfoRequest.PackageSource != null
            ? new[] { nugetInfoRequest.PackageSource }
            : _packageSourcesProvider.GetPackageSources();

        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);

        List<Task<NugetInfo?>> tasks = sources.Select(async source =>
        {
            NugetInfo? packageInfo = null;
            try
            {
                packageInfo = await FetchNugetInfoInternal(nugetInfoRequest, linkedCts.Token, source);
                if (packageInfo == null)
                {
                    _logger.Log(LogLevel.Information, "Not found {name}:{version} nuget info within source: {source}", nugetInfoRequest.PackageName, nugetInfoRequest.PackageVersion, source);
                }
                else
                {
                    _logger.Log(LogLevel.Information, "Fetched {name}:{version} nuget info from source: {source}", nugetInfoRequest.PackageName, packageInfo.Version, source);
                }
            }
            catch (Exception e)
            {
                _logger.Log(e is OperationCanceledException ? LogLevel.Trace : LogLevel.Warning, e,
                    "Failed to fetch nuget info from source: {source}", source);
            }

            return packageInfo;
        }).ToList();

        while (tasks.Any())
        {
            Task<NugetInfo?> finishedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
            _ = tasks.Remove(finishedTask);
            NugetInfo? packageInfo = await finishedTask.ConfigureAwait(false);
            if (packageInfo != null)
            {
                linkedCts.Cancel();
                return packageInfo;
            }
        }

        throw new BuildLinkException($"Cannot locate package {nugetInfoRequest.PackageName}",
            BuildLinkErrorCode.NotEnoughInformationToProceed);
    }

    private async Task<NugetInfo?> FetchNugetInfoInternal(NugetInfoRequest nugetInfoRequest, CancellationToken token, string packageSource)
    {
        PackageSource source = new PackageSource(packageSource);
        SourceRepository repository = Repository.Factory.GetCoreV3(source);
        FindPackageByIdResource resource;
        resource = await repository.GetResourceAsync<FindPackageByIdResource>(token).ConfigureAwait(false);
        SourceCacheContext cacheSettings = new SourceCacheContext();

        var nugetLogger = new NuGetLogger(_loggerFactory);

        NuGetVersion nuGetVersion;
        if (nugetInfoRequest.PackageVersion == null)
        {
            var res = await resource.GetAllVersionsAsync(nugetInfoRequest.PackageName, cacheSettings, nugetLogger, token)
                .ConfigureAwait(false);
            if (!nugetInfoRequest.AllowPrerelease)
            {
                res = res.Where(v => !v.IsPrerelease);
            }

            NuGetVersion? max = res.Max();
            if (max == null)
            {
                return null;
            }

            nuGetVersion = max;
        }
        else
        {
            nuGetVersion = nugetInfoRequest.PackageVersion;
        }

        string packageDir = Path.Combine(_environment.BuildLinkHomeFolder, "packages");
        _fileSystem.CreateDirectory(packageDir);
        string packagePath = Path.Combine(packageDir,
            $"{nugetInfoRequest.PackageName}.{nuGetVersion.ToString()}.nupkg");

        bool needsDownload = !File.Exists(packagePath);
        if (!needsDownload)
        {
            SourceRepository localRepo = Repository.Factory.GetCoreV3(packageDir, FeedType.FileSystemV2);
            resource = await localRepo.GetResourceAsync<FindPackageByIdResource>(token).ConfigureAwait(false);
        }

        IPackageDownloader packageDownloader = await resource.GetPackageDownloaderAsync(
            new PackageIdentity(nugetInfoRequest.PackageName, nuGetVersion),
            cacheSettings,
            nugetLogger,
            token).ConfigureAwait(false);
        if (needsDownload)
        {
            if (!await packageDownloader.CopyNupkgFileToAsync(packagePath, token).ConfigureAwait(false))
            {
                return null;
            }
        }

        var libFiles =
            (await packageDownloader.ContentReader.GetLibItemsAsync(token).ConfigureAwait(false))
            .SelectMany(g => g.Items)
            .ToList();

        // package downloader holds on the opened file
        packageDownloader.Dispose();

        // The easier way - but requires referencing NugetPackageExplorer binaries
        //using NuGetPe.ZipPackage zp = new ZipPackage(packagePath);
        //string m = zp.Repository.Commit;

        using var reader = new PackageArchiveReader(File.OpenRead(packagePath), false);
        using var nuspecStream = await reader.GetNuspecAsync(token);
        using var manifestStream = ManifestUtility.ReadManifest(nuspecStream);

        var manifest = Manifest.ReadFrom(manifestStream, false);
        var metadata = manifest.Metadata;

        RepositoryMetadata repositoryMetadata = RepositoryMetadata.FromRepositoryMetadata(metadata.Repository);

        return new NugetInfo(nugetInfoRequest.PackageName, nuGetVersion.ToString(), repositoryMetadata, libFiles, packagePath);
    }
}