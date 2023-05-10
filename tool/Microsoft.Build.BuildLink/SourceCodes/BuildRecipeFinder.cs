// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BuildUtils;
using LibGit2Sharp.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Build.BuildLink.SourceCodes
{
    public enum BuildType
    {
        SolutionFile,
        ProjectFile,
        BuildScript
    }

    internal static class BuildTypeExtensions
    {
        public static readonly string SlnExtension = ".sln";
        public static readonly string[] ProjectExtensions = new[] { ".csproj", ".vbproj", ".fsproj" };
        public static readonly string[] ScriptExtensions = new[] { ".cmd", ".ps1", ".bat", ".sh", ".cake" };

        public static string ToShortString(this BuildType buildType)
            =>
                buildType switch
                {
                    BuildType.ProjectFile => "Proj",
                    BuildType.SolutionFile => "Sln",
                    BuildType.BuildScript => "Script",
                    _ => throw new ArgumentOutOfRangeException(nameof(buildType), buildType, null)
                };

        public static string ToHumanReadableString(this BuildType buildType)
            =>
                buildType switch
                {
                    BuildType.ProjectFile => "MSBuild project file",
                    BuildType.SolutionFile => "MSBuild solution file",
                    BuildType.BuildScript => "build script",
                    _ => throw new ArgumentOutOfRangeException(nameof(buildType), buildType, null)
                };

        public static string ToShortAlignedString(this BuildType buildType)
            =>
                buildType switch
                {
                    BuildType.ProjectFile => "Proj  ",
                    BuildType.SolutionFile => "Sln   ",
                    BuildType.BuildScript => "Script",
                    _ => throw new ArgumentOutOfRangeException(nameof(buildType), buildType, null)
                };

        public static ScriptType ToScriptType(this string filePath)
            =>
                Path.GetExtension(filePath).ToLowerInvariant() switch
                {
                    ".cmd" => ScriptType.Command,
                    ".ps1" => ScriptType.PowerShell,
                    ".bat" => ScriptType.Command,
                    ".proj" => ScriptType.MsBuild,
                    ".sh" => ScriptType.Shell,
                    ".cake" => ScriptType.Cake,
                    _ => ScriptType.Other
                };

        public static OSPlatform? ToDefaultOsPlatform(this ScriptType scriptType)
            =>
                scriptType switch
                {
                    ScriptType.Command => OSPlatform.Windows,
                    ScriptType.PowerShell => OSPlatform.Windows,
                    ScriptType.MsBuild => null,
                    ScriptType.Cake => null,
                    ScriptType.Shell => OSPlatform.Linux,
                    ScriptType.Bash => OSPlatform.Linux,
                    ScriptType.Other => null,
                    _ => null
                };
    }

    public enum ScriptType
    {
        Command,
        PowerShell,
        MsBuild,
        Cake,
        Shell,
        Bash,
        Other
    }

    public class Script
    {
        public static readonly Script NullScript = new Script();

        private Script()
        {
            ScriptFilePath = string.Empty;
        }

        public static Script FromPath(string path)
        {
            return string.IsNullOrEmpty(path) ? NullScript : new Script(path);
        }

        public Script(string scriptFilePath)
        {
            ScriptFilePath = scriptFilePath;
            ScriptType = scriptFilePath.ToScriptType();
            Platform = ScriptType.ToDefaultOsPlatform();
        }

        public Script(string scriptFilePath, OSPlatform? platform, ScriptType scriptType)
        {
            ScriptFilePath = scriptFilePath;
            Platform = platform;
            ScriptType = scriptType;
        }

        public string ScriptFilePath { get; init; }
        /// <summary>
        /// Platform where the script can be run. If null - unrestricted (e.g. cake)
        /// </summary>
        public OSPlatform? Platform { get; init; }
        public ScriptType ScriptType { get; init; }

    }

    public class NugetBuildDescriptor
    {
        public NugetBuildDescriptor(string packageName, Script buildScript, string msBuildProject, IReadOnlyDictionary<string, Script> buildScriptPerLibAsset, IReadOnlyDictionary<string, string> msBuildProjectFilePerLibAsset)
        {
            PackageName = packageName;
            BuildScript = buildScript;
            MsBuildProject = msBuildProject;
            BuildScriptPerLibAsset = buildScriptPerLibAsset;
            MsBuildProjectFilePerLibAsset = msBuildProjectFilePerLibAsset;
        }

        public NugetBuildDescriptor(string packageName, string msBuildProject,
            IReadOnlyDictionary<string, string> msBuildProjectFilePerLibAsset)
            : this(packageName, Script.NullScript, msBuildProject, new Dictionary<string, Script>(),
                msBuildProjectFilePerLibAsset)
        { }

        /// <summary>
        /// Name of the package for which this section is applicable
        /// </summary>
        public string PackageName { get; init; }
        /// <summary>
        /// Optional path to build script - applicable to all lib assets of the package.
        /// </summary>
        public Script BuildScript { get; init; }
        /// <summary>
        /// Optional path to MSBuild file - applicable to all lib assets of the package.
        /// </summary>
        public string MsBuildProject { get; init; }
        /// <summary>
        /// Optional lookup of paths to build script - indexable by subpath of a lib asset (or it's filename).
        /// </summary>
        public IReadOnlyDictionary<string, Script> BuildScriptPerLibAsset { get; init; }
        /// <summary>
        /// Optional lookup of paths to MSBuild files - indexable by subpath of a lib asset (or it's filename).
        /// </summary>
        public IReadOnlyDictionary<string, string> MsBuildProjectFilePerLibAsset { get; init; }
    }

    public class WorkingCopyBuildDescriptor
    {
        public WorkingCopyBuildDescriptor(Script workingCopyInitScript, Script preBuildScript, Script buildScript,
            string workingCopySolutionFile, NugetBuildDescriptor[] nugetBuildDescriptors)
        {
            WorkingCopyInitScript = workingCopyInitScript;
            PreBuildScript = preBuildScript;
            BuildScript = buildScript;
            WorkingCopySolutionFile = workingCopySolutionFile;
            NugetBuildDescriptors = nugetBuildDescriptors;
        }

        public WorkingCopyBuildDescriptor(Script buildScript, string workingCopySolutionFile,
            NugetBuildDescriptor[] nugetBuildDescriptors)
            : this(Script.NullScript, Script.NullScript, buildScript, workingCopySolutionFile, nugetBuildDescriptors)
        { }

        /// <summary>
        /// Optional - script to be used for one time initialization of working copy (after cloning).
        /// </summary>
        public Script WorkingCopyInitScript { get; init; }
        /// <summary>
        /// Optional - script to be used for build initialization - to be run before each script (e.g. restore, if not part of the build).
        /// </summary>
        public Script PreBuildScript { get; init; }
        /// <summary>
        /// Optional - script that can be used for building of the whole working copy (as an alternative for the individual nuget builds)
        /// </summary>
        public Script BuildScript { get; init; }
        /// <summary>
        /// Optional - path to solution file that can be used for building of the whole working copy (as an alternative for the individual nuget builds)
        /// </summary>
        public string WorkingCopySolutionFile { get; init; }
        /// <summary>
        /// Set of build descriptors for nuget packages buildable from the working copy.
        /// </summary>
        public NugetBuildDescriptor[] NugetBuildDescriptors { get; init; }
    }


    internal interface IBuildDescriptionFinder
    {
        WorkingCopyBuildDescriptor DiscoverBuildFiles(string repoRoot, string packageName, IReadOnlyList<string> assemblyNames);
    }

    internal class BuildDescriptionFinder : IBuildDescriptionFinder
    {
        private readonly ILogger<BuildDescriptionFinder> _logger;
        private readonly IFileSystemHelper _fileSystemHelper;

        public BuildDescriptionFinder(ILogger<BuildDescriptionFinder> logger, IFileSystemHelper fileSystemHelper)
        {
            _logger = logger;
            _fileSystemHelper = fileSystemHelper;
        }

        public WorkingCopyBuildDescriptor DiscoverBuildFiles(string repoRoot, string packageName, IReadOnlyList<string> assemblyNames)
        {
            string[] extensionsOfInterest =
                BuildTypeExtensions.ProjectExtensions.Union(BuildTypeExtensions.ScriptExtensions)
                    .Union(new[] { BuildTypeExtensions.SlnExtension }).ToArray();

            var discoveredFiles =
                _fileSystemHelper.EnumerateFiles(repoRoot, extensionsOfInterest)
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
            Script buildScript = GetBuildFile(discoveredFiles, BuildType.BuildScript, () => Script.NullScript,
                candidates => Script.FromPath(FilterBuildScripts(repoRoot, candidates)));

            List<string> unifiedAssemblyNames = assemblyNames.Select(Path.GetFileNameWithoutExtension)
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

        private T GetBuildFile<T>(
            Dictionary<BuildType, List<string>> discoveredFiles,
            BuildType buildType,
            Func<T> nullFactory,
            Func<List<string>, T> valueFactory)
        {
            if (!discoveredFiles.TryGetValue(BuildType.BuildScript, out List<string>? candidates))
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
}
