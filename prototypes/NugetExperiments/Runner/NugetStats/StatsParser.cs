// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json.Linq;
using NugetUtils;

namespace Runner.NugetStats
{
    internal static class StatsParser
    {
        public const string StatsPath = @"NugetStats\packages-stats.csv";

        public static IEnumerable<(NugetStatsRecord, GithubRepoLocationInfo?)> FetchTopStats(string path = StatsPath)
        {
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<NugetStatsRecordMap>();
            foreach (var record in csv.GetRecords<NugetStatsRecord>())
            {
                GithubRepoLocationInfo? repoLocation = null;
                // exclude '[]'
                if ((record.SourceLinkResult == SourceLinkResult.ValidExternal || record.SourceLinkResult == SourceLinkResult.Valid)
                    &&
                    !string.IsNullOrEmpty(record.SourceUrlRepoInfo) && record.SourceUrlRepoInfo.Length > 2)
                {
                    var arr = JArray.Parse(record.SourceUrlRepoInfo);
                    //var obj = JObject.Parse(record.SourceUrlRepoInfo);
                    if ((arr.First as JObject).Count != 3)
                    {
                        throw new Exception("Unexpected data");
                    }

                    //Repo, FileCount, Example
                    JProperty repoProp = (arr.First as JObject).Properties().First(p => p.Name == "Repo");
                    string repoType = GetChildPropertyValue(repoProp, "Type");
                    if (repoType != "GitHub")
                    {
                        throw new Exception("Unsupported repo type: " + repoType);
                    }
                    string revisionRef = GetChildPropertyValue(repoProp, "Ref");
                    string owner = GetChildPropertyValue(repoProp, "Owner");
                    string repoName = GetChildPropertyValue(repoProp, "Repo");

                    repoLocation = new GithubRepoLocationInfo(owner, repoName, revisionRef);
                }
                yield return (record, repoLocation);
            }
        }

        private static string GetChildPropertyValue(JProperty property, string childPropName)
        {
            return property.Values().Cast<JProperty>().First(v => v.Name == childPropName).Value.ToString();
        }

        public sealed class NugetStatsRecordMap : ClassMap<NugetStatsRecord>
        {
            public NugetStatsRecordMap()
            {
                AutoMap(CultureInfo.InvariantCulture);
                // Map(m => m.SourceLocation).Ignore();
            }
        }
    }

    public class NugetStatsRecord
    {
        public string Id { get; set; }
        public SourceLinkResult SourceLinkResult { get; set; }
        public CompilerFlagsResult CompilerFlagsResult { get; set; }
        public string SourceUrlRepoInfo { get; set; }
    }

    public enum SourceLinkResult
    {
        HasUntrackedSources, //The package has valid SourceLink but has untracked sources
        InvalidSourceLink, //The package has invalid SourceLink metadata
        NoSourceLink, //The package has missing SourceLink metadata
        NoSymbols, //The package has missing symbols
        NothingToValidate, //The package has no assemblies to validate
        Valid, //The package has valid embedded symbols
        ValidExternal, //The package has valid externally hosted symbols
    }

    public enum CompilerFlagsResult
    {
        Missing, //The package symbols don't have compiler flags
        NothingToValidate, //The package has no assemblies to validate
        Present, //The package symbols have compiler flags but are too old to be reproducible
        Valid, //The package symbols have compiler flags and are reproducible
    }
}
