using System.Collections.Concurrent;
using Microsoft.Build.BuildLink.IO;
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
        token.ThrowIfCancellationRequested();
        List<string> sources = nugetInfoRequest.PackageSource != null
            ? new List<string>() { nugetInfoRequest.PackageSource }
            : _packageSourcesProvider.GetPackageSources().ToList();
        token.ThrowIfCancellationRequested();

        NuGetVersion nuGetVersion = await GetVersion(nugetInfoRequest, token, sources).ConfigureAwait(false);

        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);

        List<Task<NugetInfo?>> tasks = sources.Select(async source =>
        {
            NugetInfo? packageInfo = null;
            try
            {
                packageInfo = await FetchNugetInfoInternal(nugetInfoRequest, source, nuGetVersion, linkedCts.Token);
                if (packageInfo == null)
                {
                    _logger.Log(LogLevel.Information, "Not found {name}:{version} nuget info within source: {source}", nugetInfoRequest.PackageName, nuGetVersion, source);
                }
                else
                {
                    _logger.Log(LogLevel.Information, "Fetched {name}:{version} nuget info from source: {source}", nugetInfoRequest.PackageName, nuGetVersion, source);
                }
            }
            catch (Exception e)
            {
                _logger.Log(e is OperationCanceledException ? LogLevel.Trace : LogLevel.Warning, e,
                    "Failed to fetch nuget info from source: {source}", source);
                token.ThrowIfCancellationRequested();
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

    private async Task<NuGetVersion> GetVersion(NugetInfoRequest nugetInfoRequest, CancellationToken token,
        List<string> sources)
    {
        NuGetVersion nuGetVersion;
        if (nugetInfoRequest.PackageVersion == null)
        {
            var versions = await Task.WhenAll(sources.Select(source => TryFetchLatestVersion(nugetInfoRequest, source, token)))
                .ConfigureAwait(false);
            NuGetVersion? nullableVersion = versions.Max();
            if (nullableVersion != null)
            {
                _logger.LogInformation("Resolved latest version as: {version}", nullableVersion);
                nuGetVersion = nullableVersion;
            }
            else
            {
                throw new BuildLinkException($"Cannot locate versions of package {nugetInfoRequest.PackageName}",
                    BuildLinkErrorCode.NotEnoughInformationToProceed);
            }
        }
        else
        {
            nuGetVersion = nugetInfoRequest.PackageVersion;
        }

        return nuGetVersion;
    }

    private static async Task<FindPackageByIdResource> GetPackageByIdResource(string packageSource, CancellationToken token)
    {
        PackageSource source = new PackageSource(packageSource);
        SourceRepository repository = Repository.Factory.GetCoreV3(source);
        return await repository.GetResourceAsync<FindPackageByIdResource>(token).ConfigureAwait(false);
    }

    private async Task<NuGetVersion?> TryFetchLatestVersion(
        NugetInfoRequest nugetInfoRequest,
        string packageSource,
        CancellationToken token)
    {
        try
        {
            var resource = await GetPackageByIdResource(packageSource, token).ConfigureAwait(false);
            SourceCacheContext cacheSettings = new SourceCacheContext();

            var nugetLogger = new NuGetLogger(_loggerFactory);

            var res = await resource.GetAllVersionsAsync(nugetInfoRequest.PackageName, cacheSettings, nugetLogger, token)
                .ConfigureAwait(false);
            if (!nugetInfoRequest.AllowPrerelease)
            {
                res = res.Where(v => !v.IsPrerelease);
            }

            return res.Max();
        }
        catch (Exception e)
        {
            _logger.Log(e is OperationCanceledException ? LogLevel.Trace : LogLevel.Warning, e,
                "Failed to fetch nuget versions from source: {source}", packageSource);
            token.ThrowIfCancellationRequested();
            return null;
        }
    }

    private async Task<NugetInfo?> FetchNugetInfoInternal(
        NugetInfoRequest nugetInfoRequest,
        string packageSource,
        NuGetVersion nuGetVersion,
        CancellationToken token)
    {
        var resource = await GetPackageByIdResource(packageSource, token).ConfigureAwait(false);
        SourceCacheContext cacheSettings = new SourceCacheContext();

        var nugetLogger = new NuGetLogger(_loggerFactory);

        string packageDir = Path.Combine(_environment.BuildLinkHomeFolder, "packages");
        _fileSystem.CreateDirectory(packageDir);
        string packagePath = Path.Combine(packageDir,
            $"{nugetInfoRequest.PackageName}.{nuGetVersion.ToString()}.nupkg");

        bool needsDownload = !_fileSystem.FileExists(packagePath);
        string? originalFile = null;
        if (!needsDownload)
        {
            SourceRepository localRepo = Repository.Factory.GetCoreV3(packageDir, FeedType.FileSystemV2);
            resource = await localRepo.GetResourceAsync<FindPackageByIdResource>(token).ConfigureAwait(false);
        }
        else
        {
            originalFile = packagePath;
            packagePath += Path.GetFileName(Path.GetTempFileName());
        }

        List<string> libFiles;
        RepositoryMetadata repositoryMetadata;

        //scope here to force disposing before file renaming
        {
            using IPackageDownloader packageDownloader = await resource.GetPackageDownloaderAsync(
                new PackageIdentity(nugetInfoRequest.PackageName, nuGetVersion),
                cacheSettings,
                nugetLogger,
                token).ConfigureAwait(false);
            if (packageDownloader == null)
            {
                return null;
            }

            if (needsDownload)
            {
                if (!await packageDownloader.CopyNupkgFileToAsync(packagePath, token).ConfigureAwait(false))
                {
                    return null;
                }
            }

            libFiles =
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

            repositoryMetadata = RepositoryMetadata.FromRepositoryMetadata(metadata.Repository);
        }

        if (originalFile != null)
        {
            _fileSystem.RenameFile(packagePath, originalFile);
            packagePath = originalFile;
        }

        return new NugetInfo(nugetInfoRequest.PackageName, nuGetVersion.ToString(), repositoryMetadata, libFiles, packagePath);
    }
}
