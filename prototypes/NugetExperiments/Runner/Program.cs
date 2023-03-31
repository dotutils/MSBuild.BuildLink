// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using ImageUtils;
using Microsoft.Extensions.Logging;
using NugetUtils;
using Runner.NugetStats;

namespace Runner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
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

            FetchAllCodes();

            //FetchCode();
            //return;
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
