// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using BuildUtils;
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

    public static class BuildTypeExtensions
    {
        public static string ToShortString(this BuildType buildType)
            =>
                buildType switch
                {
                    BuildType.ProjectFile => "Proj",
                    BuildType.SolutionFile => "Sln",
                    BuildType.BuildScript => "Script",
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
    }

    public class BuildRecipe
    {
        public BuildRecipe(Dictionary<BuildType, List<string>> buildFiles)
        {
            BuildFiles = buildFiles
                .Where(d => d.Value is { Count: 1 })
                .ToDictionary(p => p.Key, p => p.Value[0]);
        }

        public IReadOnlyDictionary<BuildType, string> BuildFiles { get; }
    }

    public class BuildRecipeFinder
    {
        private readonly ILogger<BuildRecipeFinder> _logger;

        public Dictionary<BuildType, List<string>> DiscoverBuildFiles(string repoRoot, string packageName, string assemblyName)
        {
            FileSystemHelper fsHelper = new FileSystemHelper(NullLogger.Instance);

            //Directory.EnumerateFiles(repoRoot, "*.sln")
            string slnExtension = ".sln";
            string[] projectExtensions = new[] {".csproj", ".vbproj", ".fsproj"};
            string[] scriptExtensions = new[] { ".cmd", ".ps1", ".bat", ".sh", ".cake" };
            //string[] buildScriptName = new[] { "build", "runbuild" };
            string[] extensionsOfInterest =
                projectExtensions.Union(scriptExtensions).Union(new[] { slnExtension }).ToArray();

            var result =
                fsHelper.EnumerateFiles(repoRoot, extensionsOfInterest)
                    .GroupBy(p => Path.GetExtension(p) switch
                    {
                        { } extension when extension.Equals(slnExtension, StringComparison.CurrentCultureIgnoreCase) =>
                            BuildType.SolutionFile,
                        { } extension when projectExtensions.Contains(extension,
                            StringComparer.CurrentCultureIgnoreCase) => BuildType.ProjectFile,
                        { } extension when scriptExtensions.Contains(extension, StringComparer.CurrentCultureIgnoreCase)
                            => BuildType.BuildScript,
                        { } x => throw new ArgumentException($"Unknown extension: {x}"),
                    })
                    .Select(s => (s.Key, s.ToList()))
                    .ToDictionary(k => k.Key, k => k.Item2);

            foreach (BuildType buildType in Enum.GetValues<BuildType>())
            {
                List<string> candidates;
                if (!result.TryGetValue(buildType, out candidates))
                {
                    _logger.LogInformation("Couldn't find any match for {buildType}", buildType);
                    result[buildType] = new List<string>();
                }
                else if(buildType == BuildType.BuildScript)
                {
                    result[buildType] = FilterBuildScripts(repoRoot, buildType, candidates, assemblyName, packageName);
                }
                else
                {
                    result[buildType] = FilterProjAndSlnList(repoRoot, buildType, candidates, assemblyName, packageName);
                }
            }

            return result;
        }

        private List<string> FilterBuildScripts(
            string repoRoot,
            BuildType buildType,
            List<string> candidates,
            string assemblyName,
            string packageName)
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
                    default:
                        return filterOutRank;
                }

                return locationRank*100 + nameRank*10 + extensionRank;
            }

            var bestMatch = candidates
                .Select(path => (path, rank: GetScriptFileAscendingRank(path)))
                .Where(p => p.rank != filterOutRank)
                .OrderBy(p => p.rank)
                .Select(p => p.path)
                .FirstOrDefault();

            if (bestMatch != null)
            {
                return new List<string>() { bestMatch };
            }
            else
            {
                _logger.LogInformation("Couldn't find exactly one match for {buildType}", buildType);
                return candidates;
            }
        }

        private List<string> FilterProjAndSlnList(
            string repoRoot,
            BuildType buildType,
            List<string> candidates,
            string assemblyName,
            string packageName)
        {
            // filter by assembly name
            var filtered = candidates.Where(p =>
                Path.GetFileNameWithoutExtension(p)
                    .Equals(assemblyName, StringComparison.CurrentCultureIgnoreCase)).ToList();

            if (filtered.Count == 1)
            {
                return filtered;
            }

            // filter by package name
            filtered = candidates.Where(p =>
                Path.GetFileNameWithoutExtension(p)
                    .Equals(packageName, StringComparison.CurrentCultureIgnoreCase)).ToList();

            if (filtered.Count == 1)
            {
                return filtered;
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
                    return filteredByContnt;
                }

                // filter by package name
                filteredByContnt = prefilteredByContnt.Where(p =>
                        p.explicitAssemblyName.Equals(packageName, StringComparison.CurrentCultureIgnoreCase))
                    .Select(p => p.path)
                    .ToList();

                if (filteredByContnt.Count == 1)
                {
                    return filteredByContnt;
                }
            }

            var rootFiles = FilterPathsByParentFolder(repoRoot, candidates);

            if (rootFiles.Count == 1)
            {
                return rootFiles;
            }

            var srcFiles = FilterPathsByParentFolder(Path.Combine(repoRoot, "src"), candidates);

            if (srcFiles.Count == 1)
            {
                return srcFiles;
            }

            var sourceFiles = FilterPathsByParentFolder(Path.Combine(repoRoot, "sources"), candidates);

            if (sourceFiles.Count == 1)
            {
                return sourceFiles;
            }

            _logger.LogInformation("Couldn't find exactly one match for {buildType}", buildType);

            return candidates;
        }

        private static List<string> FilterPathsByParentFolder(string parentFolder, List<string> paths)
        {
            //TODO: OSes with case sensitive paths
            return paths.Select(p => (dir:Path.GetDirectoryName(p), path:p))
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
