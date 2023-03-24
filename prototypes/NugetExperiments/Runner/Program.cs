// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NugetUtils;
using Runner.NugetStats;

namespace Runner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            foreach (NugetStatsRecord nugetStatsRecord in StatsParser.FetchTopStats())
            {
                Console.WriteLine(nugetStatsRecord.RemoteUrl);
            }
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
