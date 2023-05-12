using System.Text.RegularExpressions;
using BuildUtils;
using Microsoft.Build.BuildLink.IO;
using Microsoft.Build.BuildLink.Reporting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Build.BuildLink.SourceCodes;

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
            candidates => ScriptGroup.FromPath(FilterBuildScripts(repoRoot, candidates)));

        List<string> unifiedAssemblyNames = assemblyNames
            .GroupBy(n => n).Select(g => g.Key ?? string.Empty).ToList();

        // sln
        string sln = GetBuildFile(discoveredFiles, BuildType.SolutionFile, () => string.Empty,
            candidates => FilterProjAndSlnList(repoRoot, BuildType.SolutionFile, candidates,
                unifiedAssemblyNames.Count == 1 ? unifiedAssemblyNames[0] : string.Empty, packageName));

        // project
        Dictionary<string, string> projectsPerLib = GetBuildFile(
            discoveredFiles,
            BuildType.ProjectFile,
            () => new Dictionary<string, string>(),
            candidates => unifiedAssemblyNames.ToDictionary(
                assemblyName => assemblyName,
                assemblyName => FilterProjAndSlnList(repoRoot, BuildType.ProjectFile, candidates,
                    assemblyName, packageName))
        );
        string msBuildProject = string.Empty;
        var distinctProjects = projectsPerLib.Values.GroupBy(n => n).Select(g => g.Key).ToList();
        if (distinctProjects.Count == 1)
        {
            msBuildProject = distinctProjects[0];
            projectsPerLib = new Dictionary<string, string>();
        }

        NugetBuildDescriptor nugetBuildDescriptor = new(packageName, msBuildProject, projectsPerLib);
        WorkingCopyBuildDescriptor result = new(buildScript, sln, new[] { nugetBuildDescriptor });
        return result;
    }

    private string? FindBuildDescriptorFile(string repoRoot)
    {
        const string descriptorFileName = "buildlink.json";
        string descriptorFile = Path.Combine(repoRoot, descriptorFileName);
        if (_fileSystem.FileExists(descriptorFileName))
        {
            return descriptorFile;
        }

        descriptorFile = Path.Combine(repoRoot, "build", descriptorFileName);
        if (_fileSystem.FileExists(descriptorFileName))
        {
            return descriptorFile;
        }

        descriptorFile = Path.Combine(repoRoot, "src", descriptorFileName);
        if (_fileSystem.FileExists(descriptorFileName))
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

    private string FilterBuildScripts(
        string repoRoot,
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
                case string d when d.Equals(repoRoot, StringComparison.InvariantCultureIgnoreCase):
                    locationRank = 1;
                    break;
                case string d when d.Equals(Path.Combine(repoRoot, "build"), StringComparison.InvariantCultureIgnoreCase):
                    locationRank = 2;
                    break;
                case string d when d.Equals(Path.Combine(repoRoot, "src"), StringComparison.InvariantCultureIgnoreCase):
                    locationRank = 3;
                    break;
                case string d when d.Equals(Path.Combine(repoRoot, "sources"), StringComparison.InvariantCultureIgnoreCase):
                    locationRank = 4;
                    break;
                default:
                    return filterOutRank;
            }

            int extensionRank;
            switch (Path.GetExtension(fullPath))
            {
                case string d when d.Equals(".ps1", StringComparison.InvariantCultureIgnoreCase):
                    extensionRank = 1;
                    break;
                case string d when d.Equals(".cmd", StringComparison.InvariantCultureIgnoreCase):
                    extensionRank = 2;
                    break;
                case string d when d.Equals(".bat", StringComparison.InvariantCultureIgnoreCase):
                    extensionRank = 3;
                    break;
                case string d when BuildTypeExtensions.ScriptExtensions.Contains(d, StringComparer.InvariantCultureIgnoreCase):
                    extensionRank = 4;
                    break;
                default:
                    return filterOutRank;
            }

            return locationRank * 100 + nameRank * 10 + extensionRank;
        }

        var possibleMatches = candidates
            .Select(path => (path, rank: GetScriptFileAscendingRank(path)))
            .Where(p => p.rank != filterOutRank)
            .OrderBy(p => p.rank)
            .Select(p => p.path)
            .ToList();

        var bestMatch = possibleMatches.FirstOrDefault();

        if (string.IsNullOrEmpty(bestMatch))
        {
            _logger.LogInformation(
                "Couldn't find exactly one matching {fileType} (possible matches: {matchesCount}). So assuming no {fileType} exists.",
                BuildType.BuildScript.ToHumanReadableString(), possibleMatches.Count, BuildType.BuildScript.ToHumanReadableString());
        }

        return bestMatch ?? string.Empty;
    }

    private string FilterProjAndSlnList(
        string repoRoot,
        BuildType buildType,
        List<string> candidates,
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
                    hasExplicitAssemblyName: TryGetAssemblyName(p, out string explicitAssemblyName),
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

        var rootFiles = FilterPathsByParentFolder(repoRoot, candidates);

        if (rootFiles.Count == 1)
        {
            return rootFiles[0];
        }

        var srcFiles = FilterPathsByParentFolder(Path.Combine(repoRoot, "src"), candidates);

        if (srcFiles.Count == 1)
        {
            return srcFiles[0];
        }

        var sourceFiles = FilterPathsByParentFolder(Path.Combine(repoRoot, "sources"), candidates);

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
