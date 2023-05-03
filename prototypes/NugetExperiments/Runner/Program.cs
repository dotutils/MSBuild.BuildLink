// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BuildUtils;
using ImageUtils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NugetUtils;
using Runner.NugetStats;

namespace Runner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            LibStats();
            return;


            ExperimentWithStats();

            //MergeStats();

            return;

            //ImagesComparer imagesComparer = new ImagesComparer(loggerFactory);
            ////Console.WriteLine(imagesComparer.AreEqualExceptSignature(
            ////    @"C:\Users\jankrivanek\Downloads\newtonsoft.json.13.0.3\lib\net6.0\Newtonsoft.Json.dll",
            ////    @"C:\trash\build\JamesNK-Newtonsoft.Json3\Src\Newtonsoft.Json\bin\Release\Newtonsoft.Json.dll"));

            //Console.WriteLine(imagesComparer.GetSimilarityScore(
            //    @"C:\Users\jankrivanek\Downloads\newtonsoft.json.13.0.3\lib\net6.0\Newtonsoft.Json.dll",
            //    @"C:\trash\build\JamesNK-Newtonsoft.Json3\Src\Newtonsoft.Json\bin\Release\net6.0\Newtonsoft.Json.dll"));
            //Console.WriteLine(imagesComparer.GetSimilarityScore(
            //    @"C:\trash\build\JamesNK-Newtonsoft.Json3\Src\Newtonsoft.Json\bin\Release\net6.0\Newtonsoft.Json.dll",
            //    @"C:\trash\build\JamesNK-Newtonsoft.Json4\Src\Newtonsoft.Json\bin\Release\net6.0\Newtonsoft.Json.dll"));
            //Console.WriteLine(imagesComparer.GetSimilarityScore(
            //    @"C:\trash\build\JamesNK-Newtonsoft.Json3\Src\Newtonsoft.Json\bin\Release\net6.0\Newtonsoft.Json.dll",
            //    @"C:\trash\build\JamesNK-Newtonsoft.Json5\Src\Newtonsoft.Json\bin\Release\net6.0\Newtonsoft.Json.dll"));

            //return;

            //int i = 0;
            //int skippedCount = 0;
            //foreach ((NugetStatsRecord, GithubRepoLocationInfo?) nugetStatsRecord in StatsParser.FetchTopStats())
            //{
            //    if (nugetStatsRecord.Item2 == null)
            //    {
            //        Console.WriteLine(++i + ": skipped - no repo info");
            //        skippedCount++;
            //        continue;
            //    }

            //    Console.WriteLine(++i + ": " + nugetStatsRecord.Item2.Location + "    " + nugetStatsRecord.Item2.RevisionRef);
            //}
            //Console.WriteLine($"Total: {i}, Skipped: {skippedCount}");

            //FetchAllCodes();

            //GetNoShaBuildFiles();

            //GetBuildFiles();

            //TestNugetDownloader().Wait();

            //TestBuild_Grpc();
            // TestBuild_Newtonsoft();

            //FetchCode();
            //return;


            BuildSimilarityEvaluator.CheckBuildsSimilarity_NoSha();

            Console.WriteLine("All done");
            Console.ReadKey();
        }

        private static void LibStats(string path = @"C:\nugets\extracted")
        {
            //extensions
            Console.WriteLine(string.Join(',',
                Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Select(Path.GetExtension).GroupBy(e => e).Select(g => g.Key)
            ));

            // nugets with distinct libs
            //Console.WriteLine(
            //    string.Join(
            //        Environment.NewLine,
            //        Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories)
            //            .Select(p => (fullPath: p, nugetName: p.Split('\\', 5)[3]))
            //            .GroupBy(i => i.nugetName)
            //            .Where(g => g.GroupBy(i => Path.GetFileName(i.fullPath)).Count() > 1)
            //            .Select(i => i.Key)
            //    ));

            Console.WriteLine(
                string.Join(
                    Environment.NewLine,
                    Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories)
                        .Select(p => (fullPath: p, nugetName: p.Split('\\', 5)[3]))
                        .GroupBy(i => i.nugetName)
                        .Where(g => g.GroupBy(i => Path.GetFileName(i.fullPath)).Count() > 1)
                        .SelectMany(g => g.Select(i => (nugetName: i.nugetName, lib: i.fullPath.Split('\\', 6)[5])))
                        .OrderBy(i => i.nugetName + i.lib)
                        .Select(i => i.nugetName + " " + i.lib)
                ));
        }

        private static void ExperimentWithStats()
        {
            //foreach (var st in BuildsComparisonStatsParser.FetchExStats(@"C:\synced\OneDrive - Microsoft\issues\packageSourcing\stats\MergedStatsEx.csv"))
            //{
            //    Console.WriteLine(st.Artifact);
            //}

            var stats = BuildsComparisonStatsParser
                .FetchExStats(@"C:\synced\OneDrive - Microsoft\issues\packageSourcing\stats\MergedStatsEx.csv")
                .ToList();

            //var diff = stats.GroupBy(s => s.NugetId + "-" + s.BuildType + "-" + s.SdkMajor)
            //    .Where(g => g.Select(s => s.HasArtifact).Distinct().Count() > 1)
            //    .Select(g => g.Key);

            //Console.WriteLine(string.Join(',', diff));

            //Console.WriteLine(stats.Where(s => !s.PointsToRepo).Count());
            //Console.WriteLine(stats.Where(s => !s.PointsToRepo && s.HasReusableCompilerFlags).Count());
            //Console.WriteLine(stats.Where(s => !s.HasReusableCompilerFlags).GroupBy(g => g.NugetId).Count());

            //Console.WriteLine(stats.Where(s => !s.HasReusableCompilerFlags && s.HasArtifact).GroupBy(g => g.NugetId).Count());

            //Console.WriteLine(string.Join(',',
            //    stats.GroupBy(s => s.NugetId).Where(g => g.All(si => !si.HasArtifact)).Select(g => g.Key)
            //));

            //Console.WriteLine(stats.GroupBy(s => s.NugetId).Where(g => g.All(si => !si.HasArtifact)).Count());
            //Console.WriteLine(stats.GroupBy(s => s.NugetId).Where(g => g.All(si => !si.HasArtifact) && g.Any(f => f.HasReusableCompilerFlags)).Count());

            //Console.WriteLine(string.Join(',',
            //    stats.GroupBy(s => s.NugetId).Where(g => g.All(si => !si.HasArtifact) && g.Any(f => f.HasReusableCompilerFlags)).Select(g => g.Key)
            //));

            //
            // Counts of nugets that has all, none or some artifacts buildable
            //Console.WriteLine(stats.GroupBy(s => s.NugetId).Count(g => g.Any(v => v.HasArtifact)));
            //Console.WriteLine(stats.GroupBy(s => s.NugetId).Count(g => g.All(v => v.HasArtifact)));
            //Console.WriteLine(stats.GroupBy(s => s.NugetId).Count(g => g.Any(v => !v.HasArtifact)));

            //
            // Counts of packages buildable by compiler flags; and subset of those that are not buildable by build-guess
            //
            //Console.WriteLine(stats.GroupBy(s => s.NugetId).Count(g => g.Any(v => v.HasReusableCompilerFlags)));
            //Console.WriteLine(stats.GroupBy(s => s.NugetId).Count(g => g.Any(v => v.HasReusableCompilerFlags) && g.All(v => !v.HasArtifact)));


            //
            // Counts of packages with repo info that are not buildable by compiler flags nor any build guess
            //
            //Console.WriteLine(
            //    stats.GroupBy(s => s.NugetId).Count(g => g.Any(v => !v.HasReusableCompilerFlags && v.PointsToRepo) && g.All(v => !v.HasArtifact))
            //);

            //
            // Count of nugets buildable by project
            //
            //Console.WriteLine(
            //    stats.GroupBy(s => s.NugetId).Count(g => g.Any(v => v.BuildType == BuildType.ProjectFile && v.HasArtifact))
            //);

            //
            // Get the ordered (by popularity) list of packages and their best build type and similarity score
            //
            //Console.WriteLine(
            //    string.Join(Environment.NewLine,
            //        stats.GroupBy(s => s.NugetId)
            //            .OrderBy(g => g.First().PopularityRank)
            //            .Select(g => (g.Key, GetBest(g)))
            //            .Where(g => g.Item2.bestBuildType != null)
            //            .Select(d => $"{d.Key},{d.Item2.bestBuildType.Value.ToShortString()},{d.Item2.topScore}")
            //    ));


            //
            // Get the ordered list of biggest diffs between proj nad script
            //
            //Console.WriteLine(
            //    string.Join(Environment.NewLine,
            //        stats
            //            .Where(s => s.BuildType == BuildType.ProjectFile && s.HasArtifact)
            //            .Where(s => s.DiffToScript > 0)
            //            .OrderBy(s => s.DiffToScript)
            //            .Select(s => $"{s.NugetId}-{s.Artifact}-{s.SdkMajor}   {s.DiffToScript}")
            //    ));

            //
            // Ordered list of artifact by their proj build score less bes build score
            //
            //Console.WriteLine(
            //    string.Join(Environment.NewLine,
            //        stats
            //            .GroupBy(s => (s.NugetId, s.Artifact, s.SdkMajor))
            //            .Where(g => g.Any(s => s.BuildType == BuildType.ProjectFile && s.HasArtifact))
            //            .Select(g => (key: g.Key, best: GetBest(g),
            //                projScore: g.Single(v => v.BuildType == BuildType.ProjectFile).DiffToOfficial))
            //            .Select(g => (data: g, difference: g.best.topScore - g.projScore))
            //            .OrderByDescending(g => g.difference)
            //            .Select(g =>
            //                $"{g.difference:0.###},({g.data.best.topScore:0.###} - {g.data.projScore:0.###}),{g.data.best.bestBuildType.Value.ToShortString()},{g.data.key.NugetId}:{g.data.key.Artifact}:{g.data.key.SdkMajor}")
            //    ));

            //int nugetsWithProjBuildIssues = stats
            //    .GroupBy(s => (s.NugetId, s.Artifact, s.SdkMajor))
            //    //not buildable by project
            //    .Where(g => g.All(s => s.BuildType != BuildType.ProjectFile || !s.HasArtifact))
            //    //but buildable somehow
            //    .Where(g => g.Any(s => s.HasArtifact))
            //    .GroupBy(g => g.Key.NugetId)
            //    .Count();

            // Packages (artifacts) with some proj build issues
            //
            Console.WriteLine(
                string.Join(Environment.NewLine,
                    stats
                        .GroupBy(s => (s.NugetId, s.Artifact, s.SdkMajor))
                        //not buildable by project
                        .Where(g => g.All(s => s.BuildType != BuildType.ProjectFile || !s.HasArtifact))
                        //but buildable somehow
                        .Where(g => g.Any(s => s.HasArtifact))
                        .Select(s => $"{s.Key.NugetId}:{s.Key.Artifact}:{s.Key.SdkMajor}")
                ));
        }

        private static (BuildType? bestBuildType, double topScore) GetBest(IEnumerable<BuildsComparisonStatsEx> stats)
        {
            var s = stats.OrderByDescending(s => s.DiffToOfficial).First();
            if (s.DiffToOfficial > 0)
            {
                return (s.BuildType, s.DiffToOfficial);
            }

            return (null, 0);
        }

        private static void MergeStats()
        {
            string resultPath = @"C:\synced\OneDrive - Microsoft\issues\packageSourcing\stats\MergedStatsEx.csv";

            IEnumerable<BuildsComparisonStats> buildComparisonStats = BuildsComparisonStatsParser.FetchStats(
                    @"C:\synced\OneDrive - Microsoft\issues\packageSourcing\stats\BuildsComparison_sha_packages.csv")
                .Concat(
                    BuildsComparisonStatsParser.FetchStats(
                        @"C:\synced\OneDrive - Microsoft\issues\packageSourcing\stats\BuildsComparison_noSha_packages.csv"));

            //foreach (var stat in buildComparisonStats)
            //{
            //    Console.WriteLine(stat.Artifact);
            //}

            Dictionary<string, List<BuildsComparisonStats>> buildStats =
                buildComparisonStats
                    .GroupBy(s => s.NugetId)
                    .ToDictionary(
                g => g.Key, g => g.ToList());

            Dictionary<string, NugetSupplementalData> supplementalStats = new Dictionary<string, NugetSupplementalData>();

            int rank = 0;
            foreach ((NugetStatsRecord, GithubRepoLocationInfo?) nugetStat in StatsParser.FetchTopStats())
            {
                rank++;
                string nugetId = $"{nugetStat.Item1.Id}-{nugetStat.Item1.Version}";
                NugetSupplementalData supplementalData = new NugetSupplementalData()
                {
                    PopularityRank = rank,
                    HasReusableCompilerFlags = nugetStat.Item1.CompilerFlagsResult == CompilerFlagsResult.Valid,
                    NugetId = nugetId,
                    PointsToRepo = nugetStat.Item2 != null,
                    PointsToSha = !string.IsNullOrEmpty(nugetStat.Item2?.RevisionRef)
                };

                supplementalStats.Add(nugetId, supplementalData);
            }

            //validate the supplemental data is full superset
            string unmatchedKeys = string.Join(',', buildStats.Keys.Where(k => !supplementalStats.ContainsKey(k)));
            if (!string.IsNullOrEmpty(unmatchedKeys))
            {
                throw new Exception($"Unexpected unmatched keys: {unmatchedKeys}");
            }

            //now merge
            IEnumerable<BuildsComparisonStatsEx> extendedStats =
                supplementalStats
                    .SelectMany(kp => buildStats.GetValueOrDefault(kp.Key) == null ? new[]{new BuildsComparisonStatsEx(null, kp.Value)} : buildStats.GetValueOrDefault(kp.Key).Select(v => new BuildsComparisonStatsEx(v, kp.Value)))
                    //.Select(kp => new BuildsComparisonStatsEx(buildStats.GetValueOrDefault(kp.Key), kp.Value))
                    .OrderBy(GetSortKey)
                    .ToList();

            string GetSortKey(BuildsComparisonStatsEx stat)
            {
                return $"{stat.PopularityRank:000}-{stat.NugetId}-{stat.Artifact}-{stat.SdkMajor}-{stat.BuildType}";
            }

            FileUtils.AppendLineToFile(resultPath, "PopularityRank,NugetId,HasReusableCompilerFlags,PointsToRepo,PointsToSha,Artifact,SdkMajor,BuildType,BuildSuccess,HasArtifact,DiffToOfficial,DiffToOtherSdk,DiffToProj,DiffToSln,DiffToScript");
            foreach (BuildsComparisonStatsEx stat in extendedStats)
            {
                //ineffective - but hack it - just one-off
                FileUtils.AppendLineToFile(resultPath,
                    $"{stat.PopularityRank:000},{stat.NugetId},{stat.HasReusableCompilerFlags},{stat.PointsToRepo},{stat.PointsToSha},{stat.Artifact},{(string.IsNullOrEmpty(stat.Artifact) ? string.Empty : stat.SdkMajor)},{(string.IsNullOrEmpty(stat.Artifact) ? string.Empty : stat.BuildType)},{stat.BuildSuccess},{stat.HasArtifact},{stat.DiffToOfficial},{stat.DiffToOtherSdk},{stat.DiffToProj},{stat.DiffToSln},{stat.DiffToScript}");
            }
        }

        //TODO: extract the lib folder for each package, then get the binaries as list and search in build outputs
        // - evaluate build success as combination of success and presence of the binaries (what if just some?)
        // - get the comparisons
        // - flush the list

        private static async Task TestNugetDownloader()
        {
            NugetDownloader nd = new NugetDownloader();
            //string s = await nd.GetLatestVersion("newtonsoft.json");

            //string path = await nd.DownloadPackage("newtonsoft.json");

            foreach ((NugetStatsRecord, GithubRepoLocationInfo) tuple in StatsParser.FetchTopStats()
                         .Where(nugetStatsRecord => nugetStatsRecord.Item2 != null))
            {
                await nd.DownloadAndExtractPackage(tuple.Item1.Id, tuple.Item1.Version);
            }
        }
        private static void TestBuild_Newtonsoft()
        {
            BuildRecipe recipe = new BuildRecipe(
                new Dictionary<BuildType, List<string>>()
                {
                    { BuildType.SolutionFile, new List<string>() { @"C:\src-nugets\JamesNK#Newtonsoft.Json\Src\Newtonsoft.Json.sln" } },
                    { BuildType.ProjectFile, new List<string>() { @"C:\src-nugets\JamesNK#Newtonsoft.Json\Src\Newtonsoft.Json\Newtonsoft.Json.csproj" } },
                    { BuildType.BuildScript, new List<string>() { @"C:\src-nugets\JamesNK#Newtonsoft.Json\Build\localbuild.ps1" } },
                });

            TestBuild(recipe, @"C:\src-nugets\JamesNK#Newtonsoft.Json", "Newtonsoft.Json-13.0.3");
        }

        private static void TestBuild_Grpc()
        {
            BuildRecipe recipe = new BuildRecipe(
                new Dictionary<BuildType, List<string>>()
                {
                    { BuildType.SolutionFile, new List<string>() { @"C:\src-nugets\grpc#grpc-dotnet\Grpc.DotNet.sln" } },
                    { BuildType.ProjectFile, new List<string>() { @"C:\src-nugets\grpc#grpc-dotnet\src\Grpc.Core.Api\Grpc.Core.Api.csproj" } },
                    { BuildType.BuildScript, new List<string>() },
                });

            TestBuild(recipe, @"C:\src-nugets\grpc#grpc-dotnet", "Grpc.Core.Api-2.52.0");
        }

        private static void TestBuild(BuildRecipe recipe, string repoRoot, string nugetId)
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            ILogger logger = loggerFactory.CreateLogger<Program>();

            BuildHelperFactory bhf = new BuildHelperFactory(logger);
            var buildHelper = bhf.CreateBuildHelper(repoRoot, nugetId, recipe);

            //buildHelper.TryBuild(BuildType.ProjectFile, SdkVer.net6, recipe.BuildFiles[BuildType.ProjectFile]);
            var r = buildHelper.RunBuilds(SdkVer.net6);
        }

        private static void LogError(Exception e, string s)
        {
            Console.WriteLine($"{s} |||| Exc: {e.Message}");
        }

        //public static IEnumerable<string> EnumerateFiles(string inputDir, string pattern, SearchOption searchOption)
        //{

        //    try
        //    {
        //        return Directory.EnumerateFiles(inputDir, pattern, searchOption);

        //    }
        //    catch (ArgumentException ae)
        //    {
        //        _logger.LogError(ae, $"Invalid path {inputDir}, cannot search it recursively.");
        //    }
        //    catch (DirectoryNotFoundException dnfe)
        //    {
        //        _logger.LogError(dnfe, $"Directory {inputDir} is not found");
        //    }
        //    catch (PathTooLongException ptle)
        //    {
        //        _logger.LogError(ptle, $"Path is too long, cannot search recurisively {inputDir} folder");
        //    }
        //    catch (UnauthorizedAccessException uae)
        //    {
        //        _logger.LogError(uae, $"Cannot authorize, cannot search recurisively {inputDir} folder");
        //    }

        //    return Enumerable.Empty<string>();
        //}

        static void GetBuildFiles()
        {
            GetBuildFilesHelper(
                new SourceFetcher(),
                StatsParser.FetchTopStats()
                    .Where(nugetStatsRecord => nugetStatsRecord.Item2 != null &&
                                               !string.IsNullOrEmpty(nugetStatsRecord.Item2.RevisionRef))
                    .OrderBy(n => n.Item2.Owner + "#" + n.Item2.RepoName));
        }

        static void GetNoShaBuildFiles()
        {
            GetBuildFilesHelper(
                new SourceFetcher(SourceFetcher.AlternativeRoot),
                StatsParser.FetchTopStats()
                    .Where(nugetStatsRecord => nugetStatsRecord.Item2 != null &&
                                               string.IsNullOrEmpty(nugetStatsRecord.Item2.RevisionRef))
                    .OrderBy(n => n.Item2.Owner + "#" + n.Item2.RepoName));
        }

        static void GetBuildFilesHelper(SourceFetcher sf, IEnumerable<(NugetStatsRecord, GithubRepoLocationInfo)> nugetStatsRecords)
        {
            foreach ((NugetStatsRecord, GithubRepoLocationInfo) nugetStatsRecord in nugetStatsRecords)
            {
                string repoRoot = sf.FetchRepo(nugetStatsRecord.Item2);
                string packageName = nugetStatsRecord.Item1.Id;

                var res = BuildRecipeFinder.DiscoverBuildFiles(repoRoot, packageName, Path.GetFileNameWithoutExtension(nugetStatsRecord.Item1.Path));

                Console.WriteLine($"========================= {packageName} =========================");
                foreach (BuildType bt in Enum.GetValues(typeof(BuildType)))
                {
                    Console.WriteLine($"{bt.ToShortAlignedString()}: {string.Join(',', res[bt])}");
                }
                Console.WriteLine($"===================================================================");
            }
        }

        static void FetchAllCodes()
        {
            SourceFetcher sf = new SourceFetcher();
            int i = 0;
            int skippedCount = 0;
            foreach ((NugetStatsRecord, GithubRepoLocationInfo?) nugetStatsRecord in StatsParser.FetchTopStats())
            {
                if (nugetStatsRecord.Item2 == null || string.IsNullOrEmpty(nugetStatsRecord.Item2.RevisionRef))
                {
                    Console.WriteLine($"{++i} : skipped - no {(nugetStatsRecord.Item2 == null ? "repo" : "revision")} info");
                    skippedCount++;
                    continue;
                }

                Console.WriteLine(++i + ": " + nugetStatsRecord.Item2.Location + "    " + nugetStatsRecord.Item2.RevisionRef);
                sf.FetchRepo(nugetStatsRecord.Item2);
            }

            Console.WriteLine();
            Console.WriteLine(" ======================= done ========================");
            Console.WriteLine();
            Console.WriteLine($"Total: {i}, Skipped: {skippedCount}");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(" =====================================================");
        }

        static void FetchAllCodesWithoutSha()
        {
            SourceFetcher sf = new SourceFetcher(SourceFetcher.AlternativeRoot);
            int i = 0;
            int skippedCount = 0;
            foreach ((NugetStatsRecord, GithubRepoLocationInfo?) nugetStatsRecord in StatsParser.FetchTopStats())
            {
                if (nugetStatsRecord.Item2 == null || !string.IsNullOrEmpty(nugetStatsRecord.Item2.RevisionRef))
                {
                    Console.WriteLine($"{++i} : skipped - no {(nugetStatsRecord.Item2 == null ? "repo" : "revision")} info");
                    skippedCount++;
                    continue;
                }

                Console.WriteLine(++i + ": " + nugetStatsRecord.Item2.Location + "    " /*+ nugetStatsRecord.Item2.RevisionRef*/);
                sf.FetchRepo(nugetStatsRecord.Item2);
            }

            Console.WriteLine();
            Console.WriteLine(" ======================= done ========================");
            Console.WriteLine();
            Console.WriteLine($"Total: {i}, Skipped: {skippedCount}");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(" =====================================================");
        }

        static void FetchCode()
        {
            //github.com/JamesNK/Newtonsoft.Json.git    0a2e291c0d9c0c7675d445703e51750363a549ef

            SourceFetcher sf  = new SourceFetcher();
            string res = sf.FetchRepo(
                new GithubRepoLocationInfo("JamesNK", "Newtonsoft.Json", "0a2e291c0d9c0c7675d445703e51750363a549ef"));
            Console.WriteLine(res);
        }

        static async void FetchTopStatsFromNugetOrg()
        {
            Console.WriteLine("Hello, World!");
            //await new NugetStats().AsyncMethod();

            NugetUtils.NugetStats nugetStats = new();
            await nugetStats.WaitForInitialization().ConfigureAwait(false);
            foreach (PackageStatsInfo nugetStatsCommunityPackage in nugetStats.CommunityPackages)
            {
                Console.WriteLine(nugetStatsCommunityPackage.Name + ":" + nugetStatsCommunityPackage.DownloadsCount);
            }
        }
    }
}
