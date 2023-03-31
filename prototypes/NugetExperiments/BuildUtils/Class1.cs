// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BuildUtils
{
    public enum BuildType
    {
        SolutionFile,
        ProjectFile,
        BuildScript
    }

    public class BuildRecipe
    {
        private List<(BuildType, string)> buildFiles;
    }

    public static class BuildRecipeFinder
    {
        public static Dictionary<BuildType, List<string>> DiscoverBuildFiles(string repoRoot, string packageName)
        {
            //Directory.EnumerateFiles(repoRoot, "*.sln")
            string slnExtension = ".sln";
            string[] projectExtensions = new[] {".csproj", ".vbproj", ".fsproj"};
            string[] scriptExtensions = new[] { "*.cmd", "*.ps1", "*.bat", "*.sh" };
            //string[] buildScriptName = new[] { "build", "runbuild" };
            string[] extensionsOfInterest =
                projectExtensions.Union(scriptExtensions).Union(new[] { slnExtension }).ToArray();

            var result =
                Directory.EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories)
                    .Where(
                        p => extensionsOfInterest.Contains(Path.GetExtension(p),
                            StringComparer.CurrentCultureIgnoreCase))
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
                    result[buildType] = new List<string>();
                }
                else if(buildType != BuildType.BuildScript)
                {
                    if (candidates.Count > 1)
                    {
                        var filtered = candidates.Where(p =>
                            Path.GetFileNameWithoutExtension(p)
                                .Equals(packageName, StringComparison.CurrentCultureIgnoreCase)).ToList();
                        if (filtered.Count > 1)
                        {
                            Console.WriteLine("Multiple files with same name.");
                        }
                        else if (filtered.Count == 0)
                        {
                            Console.WriteLine("Multiple files, but no with matching name.");
                        }
                        else
                        {
                            result[buildType] = filtered;
                        }
                    }
                }
            }

            return result;
        }
    }
}
