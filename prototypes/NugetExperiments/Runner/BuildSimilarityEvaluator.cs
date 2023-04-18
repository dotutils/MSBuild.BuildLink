// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuildUtils;
using ImageUtils;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using NugetUtils;
using Runner.NugetStats;

namespace Runner
{
    internal static class FileUtils
    {
        public static void AppendLineToFile(string filePath, string lineToAppend)
        {
            File.AppendAllLines(filePath, new[] { lineToAppend });
        }
    }

    internal class BuildSimilarityEvaluator
    {
        public static void CheckBuildsSimilarity()
        {
            CheckBuildsSimilarityHelper(
                new SourceFetcher(),
                StatsParser.FetchTopStats()
                    .Where(nugetStatsRecord => nugetStatsRecord.Item2 != null &&
                                               !string.IsNullOrEmpty(nugetStatsRecord.Item2.RevisionRef))
                //.OrderBy(n => n.Item2.Owner + "#" + n.Item2.RepoName)
                );
        }

        public static void CheckBuildsSimilarity_NoSha()
        {
            CheckBuildsSimilarityHelper(
                new SourceFetcher(),
                StatsParser.FetchTopStats()
                    .Where(nugetStatsRecord => nugetStatsRecord.Item2 != null &&
                                               string.IsNullOrEmpty(nugetStatsRecord.Item2.RevisionRef))
            //.OrderBy(n => n.Item2.Owner + "#" + n.Item2.RepoName)
            );
        }

        static void CheckBuildsSimilarityHelper(
            SourceFetcher sf,
            IEnumerable<(NugetStatsRecord, GithubRepoLocationInfo)> nugetStatsRecords)
        {
            Stopwatch swOverall = Stopwatch.StartNew();

            string outputCsvPath = $"BuildsComparison_{DateTime.UtcNow.ToString("MMdd-HHmmss")}.csv";
            File.Create(outputCsvPath).Dispose();
            FileUtils.AppendLineToFile(outputCsvPath,
                "NugetId,SdkMajor,BuildType,BuildSuccess,Artifact,HasArtifact,DiffToOfficial,DiffToOtherSdk,DiffToProj,DiffToSln,DiffToScript");

            NugetDownloader nugetDownloader = new NugetDownloader();

            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            ILogger logger = loggerFactory.CreateLogger<Program>();
            BuildHelperFactory bhf = new BuildHelperFactory(logger);
            var imagesComparer = new ImagesComparer(loggerFactory);

            // Get the stats for top 150 nugets
            foreach ((NugetStatsRecord, GithubRepoLocationInfo) nugetStatsRecord in nugetStatsRecords)
            {
                Stopwatch swCurrent = Stopwatch.StartNew();
                // Get the github repos for the nugets
                string repoRoot = sf.FetchRepo(nugetStatsRecord.Item2);
                string packageName = nugetStatsRecord.Item1.Id;
                string packageVersion = nugetStatsRecord.Item1.Version;
                string nugetId = $"{packageName}-{packageVersion}";

                // Infer the build description files
                var res = BuildRecipeFinder.DiscoverBuildFiles(repoRoot, packageName, Path.GetFileNameWithoutExtension(nugetStatsRecord.Item1.Path));
                BuildRecipe buildRecipe = new BuildRecipe(res);

                // Run the comnbination of builds for sdks and various discovered build descriptions
                var buildHelper = bhf.CreateBuildHelper(repoRoot, nugetId, buildRecipe);
                var buildResults = buildHelper.RunBuilds();

                // Download and extract expected artifacts from nuget.org
                string extractedLibPath = nugetDownloader.DownloadAndExtractPackage(packageName, packageVersion).Result;
                FileSystemHelper fsh = new FileSystemHelper(NullLogger.Instance);
                List<Artifact> artifactsToSearch = ExtractArtifactsToSearch(fsh, nugetId, extractedLibPath);

                // Find and combine build artifacts from performed builds
                foreach (BuildResult buildResult in buildResults)
                {
                    CollectArtifacts(fsh, buildResult, artifactsToSearch, imagesComparer, logger);
                }

                // Run the comparison scoring matrixes
                ProcessArtifactsScorings(artifactsToSearch, imagesComparer);

                // Output results

                Console.WriteLine($"========================= {nugetId} =========================");
                Console.WriteLine($"------------------------- BUILDS: ---------------------------");
                foreach (var buildIdx in GetAllBuildsCombinations())
                {
                    Console.WriteLine(
                        $"{buildIdx.sdk} {buildIdx.buildType.ToShortAlignedString()}: Build={(!artifactsToSearch.Any() ? "false" : artifactsToSearch.First().BuildSuccess[(buildIdx.sdk, buildIdx.buildType)])} Artifacts={ArtifactsStatusToString(artifactsToSearch.Select(a => a.FullPathsToBuiltArtifact[buildIdx] != null))}");
                }
                Console.WriteLine($"------------------------ ARTIFACTS DIFFS: ---------------------------");
                foreach (Artifact artifact in artifactsToSearch)
                {
                    Console.WriteLine($"------------------------- {artifact.SubPath} ---------------------------");
                    foreach (var buildIdx in GetAllBuildsCombinations())
                    {
                        Console.Write(
                            $"{buildIdx.sdk} {buildIdx.buildType.ToShortAlignedString()}: Official={artifact.SimilarityScorings[buildIdx].Official:0.000} OtherSdk={artifact.SimilarityScorings[buildIdx].OtherSdk:0.000}");
                        foreach (BuildType buildType in Enum.GetValues<BuildType>())
                        {
                            Console.Write($" {buildType.ToShortAlignedString()}={artifact.SimilarityScorings[buildIdx].OtherBuilds[buildType]:0.000}");
                        }

                        Console.WriteLine();

                        // Flush to csv
                        string statsLine = $"{nugetId},{buildIdx.sdk},{buildIdx.buildType},{artifact.BuildSuccess[buildIdx]},{artifact.SubPath},{artifact.FullPathsToBuiltArtifact[buildIdx] != null},{artifact.SimilarityScorings[buildIdx].Official},{artifact.SimilarityScorings[buildIdx].OtherSdk},{artifact.SimilarityScorings[buildIdx].OtherBuilds[BuildType.ProjectFile]},{artifact.SimilarityScorings[buildIdx].OtherBuilds[BuildType.SolutionFile]},{artifact.SimilarityScorings[buildIdx].OtherBuilds[BuildType.BuildScript]}";
                        FileUtils.AppendLineToFile(outputCsvPath, statsLine);
                    }
                }
                Console.WriteLine($"====================== (Duration: {swCurrent.Elapsed}, Total: {swOverall.Elapsed}) =========================");
                Console.WriteLine();
            }
        }

        private static string ArtifactsStatusToString(IEnumerable<bool> enm)
        {
            bool any = false, all = true, hasAny = false;
            foreach (bool b in enm)
            {
                any = any || b;
                all = all && b;
                hasAny = true;
            }

            return hasAny ? "NONE" : (all ? "ALL " : (any ? "ANY " : "NONE"));
        }

        private class SimilarityScoring
        {
            public static readonly SimilarityScoring Empty = new SimilarityScoring(0, 0);

            public SimilarityScoring(double official, double otherSdk)
                => (Official, OtherSdk) = (official, otherSdk);

            public double Official { get; }

            // todo: this should ideally be lookup as well (keyed by SdkVer) - for simplicity single value
            public double OtherSdk { get; }

            public Dictionary<BuildType, double> OtherBuilds { get; } =
                Enum.GetValues<BuildType>().ToDictionary(b => b, b => 0.0);
        }

        private class Artifact
        {
            public Artifact(string nugetId, string subPath, string fullPathToNugetFile)
            {
                NugetId = nugetId;
                SubPath = subPath;
                FullPathToNugetFile = fullPathToNugetFile;
            }

            public string NugetId { get; }
            public string SubPath { get; }
            public string FullPathToNugetFile { get; }

            public Dictionary<(SdkVer sdk, BuildType buildType), bool> BuildSuccess =
                GetAllBuildsCombinations().ToDictionary(k => k, k => false);

            public Dictionary<(SdkVer sdk, BuildType buildType), string?> FullPathsToBuiltArtifact =
                GetAllBuildsCombinations().ToDictionary(k => k, k => (string?)null);

            public Dictionary<(SdkVer sdk, BuildType buildType), SimilarityScoring> SimilarityScorings { get; } =
                GetAllBuildsCombinations().ToDictionary(k => k, k => SimilarityScoring.Empty);
        }

        static IEnumerable<(SdkVer sdk, BuildType buildType)> GetAllBuildsCombinations() =>
            Enum.GetValues<SdkVer>().SelectMany(s => Enum.GetValues<BuildType>().Select(b => (s, b)));

        static List<Artifact> ExtractArtifactsToSearch(FileSystemHelper fsh, string nugetId, string extractedLibPath)
        {
            return fsh.EnumerateFiles(extractedLibPath, new[] { ".dll", ".exe" })
                //HACK: [lib netX dll] - skipping the localized binaries 
                .Select(p => (fullpath: p, subPathParts: p.Substring(extractedLibPath.Length).Split('\\', StringSplitOptions.RemoveEmptyEntries)))
                //HACK: [lib netX dll] - skipping the localized binaries 
                .Where(p => p.subPathParts.Length < 4)
                .Select(p => p.fullpath)
                .Select(p => new Artifact(nugetId, Path.Combine(Path.GetFileName(Path.GetDirectoryName(p)), Path.GetFileName(p)), p))
                .ToList();
        }

        static void ProcessArtifactsScorings(List<Artifact> artifactsToScore, ImagesComparer imagesComparer)
        {
            foreach (Artifact artifact in artifactsToScore)
            {
                foreach (var buildIdx in GetAllBuildsCombinations())
                {
                    string? buildArtifact = artifact.FullPathsToBuiltArtifact[buildIdx];
                    if (buildArtifact == null)
                    {
                        continue;
                    }

                    double officialScoring =
                        imagesComparer.GetSimilarityScore(artifact.FullPathToNugetFile, buildArtifact);
                    double otherSdkScore = GetComparisonScore(imagesComparer, artifact, buildArtifact,
                        buildIdx with { sdk = Enum.GetValues<SdkVer>().First(v => v != buildIdx.sdk) },
                        sc => sc.OtherSdk);

                    SimilarityScoring scoring = new SimilarityScoring(officialScoring, otherSdkScore);

                    foreach (BuildType buildType in Enum.GetValues<BuildType>())
                    {
                        double buildComparisonScoring = 0;
                        if (buildType == buildIdx.buildType)
                        {
                            buildComparisonScoring = 1;
                        }
                        else
                        {
                            buildComparisonScoring = GetComparisonScore(imagesComparer, artifact, buildArtifact,
                                buildIdx with { buildType = buildType },
                                sc => sc.OtherBuilds[buildIdx.buildType]);
                        }

                        scoring.OtherBuilds[buildType] = buildComparisonScoring;
                    }

                    artifact.SimilarityScorings[buildIdx] = scoring;
                }
            }
        }

        private static double GetComparisonScore(
            ImagesComparer imagesComparer,
            Artifact artifact,
            string baseArtifactPath,
            (SdkVer, BuildType) buildIdxOfArtifactToCompare,
            Func<SimilarityScoring, double> possibleExistingScoringExtractor)
        {
            // Half of the comparisons can be used from the previous one (similarity(a, b) == similarity(b, a))
            double possibleExistingScoring = possibleExistingScoringExtractor(artifact.SimilarityScorings[buildIdxOfArtifactToCompare]);
            if (possibleExistingScoring != 0.0)
            {
                return possibleExistingScoring;
            }

            string? comparingBuildArtifact = artifact.FullPathsToBuiltArtifact[buildIdxOfArtifactToCompare];
            if (comparingBuildArtifact == null)
            {
                return 0;
            }
            else
            {
                return imagesComparer.GetSimilarityScore(comparingBuildArtifact, baseArtifactPath);
            }
        }

        static void CollectArtifacts(
            FileSystemHelper fsh,
            BuildResult buildResult,
            List<Artifact> artifactsToSearch,
            ImagesComparer imageComparer,
            ILogger logger)
        {
            foreach (Artifact artifact in artifactsToSearch)
            {
                artifact.BuildSuccess[(buildResult.SdkVer, buildResult.BuildType)] = buildResult.Succeeded;
            }

            foreach (string possibleArtifact in fsh.EnumerateFiles(buildResult.BuildFolder, new[] { ".dll", ".exe" }))
            {
                var match = artifactsToSearch.FirstOrDefault(a =>
                    possibleArtifact.EndsWith(a.SubPath, StringComparison.CurrentCultureIgnoreCase));
                if (match != null)
                {
                    if (match.FullPathsToBuiltArtifact[(buildResult.SdkVer, buildResult.BuildType)] != null)
                    {
                        // if this is obj/bin tuple - that's acceptable; as well as identical binaries
                        string file1 = match.FullPathsToBuiltArtifact[(buildResult.SdkVer, buildResult.BuildType)]!;
                        string file2 = possibleArtifact;
                        string obj = Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar;
                        string bin = Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar;
                        if (file1.Replace(obj, bin).Equals(file2.Replace(obj, bin),
                                StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (file2.Contains(bin))
                            {
                                match.FullPathsToBuiltArtifact[(buildResult.SdkVer, buildResult.BuildType)] = file2;
                            }
                        }
                        else if (!ImagesComparer.AreContentsIdentical(file1, file2))
                        {
                            logger.LogError(
                                $"Artifact {match.SubPath} found twice in built location: [{match.FullPathsToBuiltArtifact[(buildResult.SdkVer, buildResult.BuildType)]}] and [{possibleArtifact}]");

                            double score1 = imageComparer.GetSimilarityScore(match.FullPathToNugetFile, file1);
                            double score2 = imageComparer.GetSimilarityScore(match.FullPathToNugetFile, file2);

                            if (score2 > score1)
                            {
                                match.FullPathsToBuiltArtifact[(buildResult.SdkVer, buildResult.BuildType)] = file2;
                            }
                        }
                    }
                    else
                    {
                        match.FullPathsToBuiltArtifact[(buildResult.SdkVer, buildResult.BuildType)] = possibleArtifact;
                    }
                }
            }
        }
    }
}
