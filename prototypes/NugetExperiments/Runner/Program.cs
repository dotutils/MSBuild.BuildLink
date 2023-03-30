// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            ImagesComparer imagesComparer = new ImagesComparer(loggerFactory);
            Console.WriteLine(imagesComparer.AreEqualExceptSignature(
                @"C:\Users\jankrivanek\Downloads\newtonsoft.json.13.0.3\lib\net6.0\Newtonsoft.Json.dll",
                @"C:\trash\build\JamesNK-Newtonsoft.Json\Src\Newtonsoft.Json\bin\Release\net6.0\Newtonsoft.Json.dll"));

            return;


            FetchCode();
            return;

            foreach (NugetStatsRecord nugetStatsRecord in StatsParser.FetchTopStats())
            {
                Console.WriteLine(nugetStatsRecord.SourceLocation.Location + "    " + nugetStatsRecord.SourceLocation.RevisionRef);
            }
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
