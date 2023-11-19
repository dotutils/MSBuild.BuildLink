using System.Text.RegularExpressions;
using BuildUtils;
using DotUtils.MsBuild.BuildLink.IO;
using DotUtils.MsBuild.BuildLink.Reporting;
using DotUtils.MsBuild.BuildLink.Utils;
using Microsoft.Extensions.Logging;

namespace DotUtils.MsBuild.BuildLink.SourceCodes;

internal class BuildDescriptionFinder : IBuildDescriptionFinder
{
    private readonly ILogger<BuildDescriptionFinder> _logger;
    private readonly IFileSystemHelper _fileSystemHelper;
    private readonly IFileSystem _fileSystem;
    private readonly IBuildDescriptorSerializer _buildDescriptorSerializer;

    public BuildDescriptionFinder(
        ILogger<BuildDescriptionFinder> logger,
        IFileSystemHelper fileSystemHelper,
        IFileSystem fileSystem,
        IBuildDescriptorSerializer buildDescriptorSerializer)
    {
        _logger = logger;
        _fileSystemHelper = fileSystemHelper;
        _fileSystem = fileSystem;
        _buildDescriptorSerializer = buildDescriptorSerializer;
    }

    public async Task<WorkingCopyBuildDescriptor?> GetBuildDescriptorAsync(
        string repoRoot,
        string? buildDescriptorFile,
        string packageName,
        string? filterByLibFile,
        CancellationToken token)
    {
        _logger.LogInformation("Searching for build descriptor file for {codes}", repoRoot);

        string? filePath = buildDescriptorFile;
        if (!string.IsNullOrEmpty(filePath))
        {
            if (!_fileSystem.FileExists(filePath) && !Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(repoRoot, buildDescriptorFile);
            }

            if (!_fileSystem.FileExists(filePath))
            {
                throw new BuildLinkException(
                    $"The specified build description file: [{buildDescriptorFile}] does not exist.",
                    BuildLinkErrorCode.InvalidData);
            }
        }
        else
        {
            filePath = FindBuildDescriptorFile(repoRoot);
            if (!string.IsNullOrEmpty(filePath))
            {
                _logger.LogInformation("Found a build description file in repository files: {file}. Will use that one instead of attempting build files discovery.", filePath);
            }
            else
            {
                return null;
            }
        }

        WorkingCopyBuildDescriptor? result;
        try
        {
            result = await _buildDescriptorSerializer.ReadFromFileAsync(filePath!, token).ConfigureAwait(false);
            result = result?.CreateSinglePackageCopy(packageName, filterByLibFile);
        }
        catch (Exception e)
        {
            throw new BuildLinkException(
                $"Encountered error during parsing build description file [{filePath}]: {e.Message}",
                BuildLinkErrorCode.InvalidData, e);
        }

        if (result == null)
        {
            throw new BuildLinkException(
                $"Build description file [{filePath}] was empty, or didn't contain 'NugetBuildDescriptors' subsection for requested package [{packageName}] (or no information for lib asset if that was specified).",
                BuildLinkErrorCode.InvalidData);
        }

        return result;
    }

    public WorkingCopyBuildDescriptor DiscoverBuildFiles(
        string repoRoot,
        string packageName,
        IReadOnlyList<string> assemblyNames,
        CancellationToken token = default)
    {
        _logger.LogInformation("Searching for build files in {codes}", repoRoot);

        string[] extensionsOfInterest =
            BuildTypeExtensions.ProjectExtensions.Union(BuildTypeExtensions.ScriptExtensions)
                .Union(new[] { BuildTypeExtensions.SlnExtension }).ToArray();

        var discoveredFiles =
            _fileSystemHelper.EnumerateFiles(repoRoot, extensionsOfInterest, token: token)
                .Select(p => p.RemoveFromStart(repoRoot.EndsWith(Path.DirectorySeparatorChar) ? repoRoot : (repoRoot + Path.DirectorySeparatorChar)))
                .GroupBy(p => Path.GetExtension(p) switch
                {
                    { } extension when extension.Equals(BuildTypeExtensions.SlnExtension, StringComparison.CurrentCultureIgnoreCase) =>
                        BuildType.SolutionFile,
                    { } extension when BuildTypeExtensions.ProjectExtensions.Contains(extension,
                        StringComparer.CurrentCultureIgnoreCase) => BuildType.ProjectFile,
                    { } extension when BuildTypeExtensions.ScriptExtensions.Contains(extension, StringComparer.CurrentCultureIgnoreCase)
                        => BuildType.BuildScript,
                    { } x => throw new ArgumentException($"Unknown extension: {x}"),
                })
                .Select(s => (s.Key, s.ToList()))
                .ToDictionary(k => k.Key, k => k.Item2);

        // script
        ScriptGroup buildScript = GetBuildFile(discoveredFiles, BuildType.BuildScript, () => ScriptGroup.NullScript,
            FilterBuildScripts);

        List<string> unifiedAssemblyNames = assemblyNames
            .GroupBy(n => n).Select(g => g.Key ?? string.Empty).ToList();

        // sln
        string sln = GetBuildFile(discoveredFiles, BuildType.SolutionFile, () => string.Empty,
            candidates => FilterProjAndSlnList(BuildType.SolutionFile, candidates, repoRoot,
                unifiedAssemblyNames.Count == 1 ? unifiedAssemblyNames[0] : string.Empty, packageName));

        // project
        Dictionary<string, string> projectsPerLib = GetBuildFile(
            discoveredFiles,
            BuildType.ProjectFile,
            () => new Dictionary<string, string>(),
            candidates => unifiedAssemblyNames.ToDictionary(
                assemblyName => assemblyName,
                assemblyName => FilterProjAndSlnList(BuildType.ProjectFile, candidates, repoRoot,
                    assemblyName, packageName))
        );
        string msBuildProject = string.Empty;
        var distinctProjects = projectsPerLib.Values.GroupBy(n => n).Select(g => g.Key).ToList();
        if (distinctProjects.Count == 1)
        {
            msBuildProject = distinctProjects[0];
            projectsPerLib = new Dictionary<string, string>();
        }

        ToolingVersionInfo tvi = null;
        if(_fileSystem.FileExists(Path.Combine(repoRoot, "global.json")))
        {
            tvi = new ToolingVersionInfo(true);
        }

        NugetBuildDescriptor nugetBuildDescriptor = new(packageName, msBuildProject, projectsPerLib);
        WorkingCopyBuildDescriptor result = new(buildScript, sln, tvi, new[] { nugetBuildDescriptor });
        return result;
    }

    public WorkingCopyBuildDescriptor CreateSample()
    {
        return new WorkingCopyBuildDescriptor(
            workingCopyInitScript: ScriptGroup.FromPaths("build/init.ps1", "build/init.sh"),
            preBuildScript: new ScriptGroup("build/restore.cake"),
            toolingVersionInfo: new ToolingVersionInfo(true, "Roslyn:4.4.0"),
            buildScript: ScriptGroup.FromPaths("build/build.ps1", "build/build.sh", "build/build.cake"),
            workingCopySolutionFile: "src/my-project.sln",
            new[]
            {
                new NugetBuildDescriptor(
                    "Package01",
                    new ScriptGroup("src/package01/build.cake"),
                    "src/package01/package01.csproj",
                    new Dictionary<string, ScriptGroup>(),
                    new Dictionary<string, string>()),

                new NugetBuildDescriptor(
                    "Package02",
                    "src/package01/package02.fsproj",
                    new Dictionary<string, string>()),

                new NugetBuildDescriptor(
                    "Package03",
                    string.Empty,
                    new Dictionary<string, string>()
                    {
                        {"net6/Package03.dll", "src/package03/net6/package03.vbproj"},
                        {"net7/Package03.dll", "src/package03/net7/package03.csproj"}
                    }),
            }
        );
    }

    private string? FindBuildDescriptorFile(string repoRoot)
    {
        const string defaultFileName = "buildlink.json";
        string descriptorFile = Path.Combine(repoRoot, defaultFileName);
        if (_fileSystem.FileExists(descriptorFile))
        {
            return descriptorFile;
        }

        descriptorFile = Path.Combine(repoRoot, "build", defaultFileName);
        if (_fileSystem.FileExists(descriptorFile))
        {
            return descriptorFile;
        }

        descriptorFile = Path.Combine(repoRoot, "src", defaultFileName);
        if (_fileSystem.FileExists(descriptorFile))
        {
            return descriptorFile;
        }

        return null;
    }

    private T GetBuildFile<T>(
        Dictionary<BuildType, List<string>> discoveredFiles,
        BuildType buildType,
        Func<T> nullFactory,
        Func<List<string>, T> valueFactory)
    {
        if (!discoveredFiles.TryGetValue(buildType, out List<string>? candidates))
        {
            _logger.LogInformation("Couldn't find any recognized {fileType} in the sources. Assuming no {fileType} exists.",
                buildType.ToHumanReadableString(), buildType.ToHumanReadableString());
            return nullFactory();
        }
        else
        {
            return valueFactory(candidates);
        }
    }

    private ScriptGroup FilterBuildScripts(
        List<string> candidates)
    {
        //first in root, then in build, than in src, then in sources

        //localbuild, runbuild, build 

        //.ps1, .cmd, .bat (others not yet)

        const int filterOutRank = int.MaxValue;

        int GetScriptFileAscendingRank(string fullPath)
        {
            int nameRank;
            switch (Path.GetFileNameWithoutExtension(fullPath))
            {
                case string d when d.Equals("localbuild", StringComparison.InvariantCultureIgnoreCase):
                    nameRank = 1;
                    break;
                case string d when d.Equals("runbuild", StringComparison.InvariantCultureIgnoreCase):
                    nameRank = 2;
                    break;
                case string d when d.Equals("build", StringComparison.InvariantCultureIgnoreCase):
                    nameRank = 3;
                    break;
                default:
                    return filterOutRank;
            }

            int locationRank;
            switch (Path.GetDirectoryName(fullPath))
            {
                // root doesn't have parent dir
                case string d when d.Equals(string.Empty, StringComparison.InvariantCultureIgnoreCase):
                    locationRank = 1;
                    break;
                case string d when d.Equals("build", StringComparison.InvariantCultureIgnoreCase):
                    locationRank = 2;
                    break;
                case string d when d.Equals("src", StringComparison.InvariantCultureIgnoreCase):
                    locationRank = 3;
                    break;
                case string d when d.Equals("sources", StringComparison.InvariantCultureIgnoreCase):
                    locationRank = 4;
                    break;
                default:
                    return filterOutRank;
            }

            int extensionRank;

            string extension = Path.GetExtension(fullPath);
            extensionRank = Array.FindIndex(BuildTypeExtensions.ScriptExtensions, ext => ext.Equals(extension, StringComparison.InvariantCultureIgnoreCase));
            if(extensionRank == -1)
            {
                return filterOutRank;
            }

            return locationRank * 100 + nameRank * 10 + extensionRank;
        }

        //var possibleMatches = candidates
        //    .Select(path => (path, rank: GetScriptFileAscendingRank(path)))
        //    .Where(p => p.rank != filterOutRank)
        //    .OrderBy(p => p.rank)
        //    .Select(p => p.path)
        //    .ToList();

        var possibleMatches = candidates
            .Select(path => (path, rank: GetScriptFileAscendingRank(path)))
            .Where(p => p.rank != filterOutRank)
            .GroupBy(p => p.path.ToScriptType().ToDefaultOsPlatform())
            .Select(g => g.OrderBy(p => p.rank).First().path)
            .ToList();

        //var bestMatch = possibleMatches.FirstOrDefault();

        if (possibleMatches.Count == 0)
        {
            _logger.LogInformation(
                "Couldn't find exactly one matching {fileType} (possible matches: {matchesCount}). So assuming no {fileType} exists.",
                BuildType.BuildScript.ToHumanReadableString(), possibleMatches.Count, BuildType.BuildScript.ToHumanReadableString());
        }

        return ScriptGroup.FromPaths(possibleMatches);
    }

    private string FilterProjAndSlnList(
        BuildType buildType,
        List<string> candidates,
        string repoRoot,
        string assemblyName,
        string packageName)
    {
        List<string> filtered;

        // filter by assembly name
        if (!string.IsNullOrEmpty(assemblyName))
        {
            filtered = candidates.Where(p =>
                Path.GetFileNameWithoutExtension(p)
                    .Equals(assemblyName, StringComparison.CurrentCultureIgnoreCase)).ToList();

            if (filtered.Count == 1)
            {
                return filtered[0];
            }
        }

        // filter by package name
        filtered = candidates.Where(p =>
            Path.GetFileNameWithoutExtension(p)
                .Equals(packageName, StringComparison.CurrentCultureIgnoreCase)).ToList();

        if (filtered.Count == 1)
        {
            return filtered[0];
        }

        if (buildType == BuildType.ProjectFile)
        {
            var prefilteredByContnt = candidates
                .Select(p => (
                    hasExplicitAssemblyName: TryGetAssemblyName(Path.Combine(repoRoot, p), out string explicitAssemblyName),
                    explicitAssemblyName,
                    path: p))
                .Where(p => p.hasExplicitAssemblyName).ToList();

            // filter by assembly name
            var filteredByContnt = prefilteredByContnt.Where(p =>
                    p.explicitAssemblyName.Equals(assemblyName, StringComparison.CurrentCultureIgnoreCase))
                .Select(p => p.path)
                .ToList();

            if (filteredByContnt.Count == 1)
            {
                return filteredByContnt[0];
            }

            // filter by package name
            filteredByContnt = prefilteredByContnt.Where(p =>
                    p.explicitAssemblyName.Equals(packageName, StringComparison.CurrentCultureIgnoreCase))
                .Select(p => p.path)
                .ToList();

            if (filteredByContnt.Count == 1)
            {
                return filteredByContnt[0];
            }
        }

        // files in root don't have parent folder
        var rootFiles = FilterPathsByParentFolder(string.Empty, candidates);

        if (rootFiles.Count == 1)
        {
            return rootFiles[0];
        }

        var srcFiles = FilterPathsByParentFolder("src", candidates);

        if (srcFiles.Count == 1)
        {
            return srcFiles[0];
        }

        var sourceFiles = FilterPathsByParentFolder("sources", candidates);

        if (sourceFiles.Count == 1)
        {
            return sourceFiles[0];
        }

        _logger.LogInformation(
            "Couldn't find exactly one matching {fileType} (possible matches: {matchesCount}). So assuming no {fileType} exists.",
            buildType.ToHumanReadableString(), candidates.Count, buildType.ToHumanReadableString());
        return string.Empty;
    }

    private static List<string> FilterPathsByParentFolder(string parentFolder, List<string> paths)
    {
        //TODO: OSes with case sensitive paths
        return paths.Select(p => (dir: Path.GetDirectoryName(p), path: p))
            .Where(p => p.dir.Equals(parentFolder, StringComparison.CurrentCultureIgnoreCase))
            .Select(p => p.path)
            .ToList();
    }

    private static bool TryGetAssemblyName(string path, out string assemblyName)
    {
        string regex = @"<AssemblyName>(.*)<\/AssemblyName>";
        Regex r = new Regex(regex, RegexOptions.IgnoreCase);

        string content = File.ReadAllText(path);
        Match m = r.Match(content);
        if (m.Success)
        {
            assemblyName = m.Groups[1].Value;
            return true;
        }

        assemblyName = string.Empty;
        return false;
    }
}
