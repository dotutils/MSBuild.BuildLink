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
        public const string StatsPath = @"NugetStats\packages-stats-new.csv";

        public static IEnumerable<(NugetStatsRecord, GithubRepoLocationInfo?)> FetchTopStats(string path = StatsPath)
        {
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<NugetStatsRecordMap>();
            foreach (var record in csv.GetRecords<NugetStatsRecord>())
            {
                GithubRepoLocationInfo? repoLocation = null;
                // exclude '[]'
                if (!string.IsNullOrEmpty(record.SourceUrlRepoInfo) && record.SourceUrlRepoInfo.Length > 2)
                {
                    // [{"Repo":{"Type":"GitHub","Owner":"nunit","Repo":"nunit","Ref":"af8ca8bc779072b4fa5432e8620f06ca796d13a3"},"FileCount":448,"Example":"https://raw.githubusercontent.com/nunit/nunit/af8ca8bc779072b4fa5432e8620f06ca796d13a3/src/NUnitFramework/framework/ActionTargets.cs"}]

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
                else if(!string.IsNullOrEmpty(record.RepositoryMetadata) && record.RepositoryMetadata.Length > 2)
                {
                    // {"Type":"git","Url":"https://github.com/autofac/Autofac","Branch":"","Commit":"1910177140fb7da375b6b554e5c31c71a3fb38e4"}

                    var repoProp = JObject.Parse(record.RepositoryMetadata);
                    string repoType = GetChildPropertyValue(repoProp, "Type");
                    if (repoType != "git")
                    {
                        throw new Exception("Unsupported repo type: " + repoType);
                    }

                    string revisionRef = GetChildPropertyValue(repoProp, "Commit");
                    string url = GetChildPropertyValue(repoProp, "Url");

                    string[] urlParts = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (!urlParts[^3].Equals("github.com", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception("Unsupported repo url: " + url);
                    }

                    string owner = urlParts[^2];
                    string repoName = urlParts.Last();
                    if (repoName.EndsWith(".git", StringComparison.Ordinal))
                    {
                        repoName = repoName[..^".git".Length];
                    }

                    repoLocation = new GithubRepoLocationInfo(owner, repoName, revisionRef);
                }
                yield return (record, repoLocation);
            }
        }

        private static string GetChildPropertyValue(JProperty property, string childPropName)
        {
            return property.Values().Cast<JProperty>().First(v => v.Name == childPropName).Value.ToString();
        }

        private static string GetChildPropertyValue(JObject property, string childPropName)
        {
            return property.Values().Cast<JValue>().First(v => v.Path == childPropName).Value.ToString();
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
        public string RepositoryMetadata { get; set; }
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
