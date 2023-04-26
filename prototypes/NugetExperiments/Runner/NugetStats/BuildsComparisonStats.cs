// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuildUtils;
using CsvHelper.Configuration;
using CsvHelper;
using Newtonsoft.Json.Linq;
using NugetUtils;

namespace Runner.NugetStats
{
    public class BuildsComparisonStats
    {
        // CSV: NugetId,SdkMajor,BuildType,BuildSuccess,Artifact,HasArtifact,DiffToOfficial,DiffToOtherSdk,DiffToProj,DiffToSln,DiffToScript
        // to be added: HasReusableCompilerFlags, PointsToRepo, PointsToSha

        public string NugetId { get; set; }
        public SdkVer? SdkMajor { get; set; }
        public BuildType? BuildType { get; set; }
        public bool BuildSuccess { get; set; }
        public string? Artifact { get; set; }
        public bool HasArtifact { get; set; }
        public double DiffToOfficial { get; set; }
        public double DiffToOtherSdk { get; set; }
        public double DiffToProj { get; set; }
        public double DiffToSln { get; set; }
        public double DiffToScript { get; set; }
    }

    public class NugetSupplementalData
    {
        public string NugetId { get; set; }
        public int PopularityRank { get; set; }
        public bool HasReusableCompilerFlags { get; set; }
        public bool PointsToRepo { get; set; }
        public bool PointsToSha { get; set; }
    }

    public class BuildsComparisonStatsEx : BuildsComparisonStats
    {
        public BuildsComparisonStatsEx(BuildsComparisonStats? other)
        {
            if (other == null)
            {
                return;
            }

            this.NugetId = other.NugetId;
            this.SdkMajor = other.SdkMajor;
            this.BuildType = other.BuildType;
            this.BuildSuccess = other.BuildSuccess;
            this.Artifact = other.Artifact;
            this.HasArtifact = other.HasArtifact;
            this.DiffToOfficial = other.DiffToOfficial;
            this.DiffToOtherSdk = other.DiffToOtherSdk;
            this.DiffToProj = other.DiffToProj;
            this.DiffToSln = other.DiffToSln;
            this.DiffToScript = other.DiffToScript;
        }

        public BuildsComparisonStatsEx(BuildsComparisonStats? other, NugetSupplementalData supplementalData)
            : this(other)
        {
            if (other != null && other.NugetId != supplementalData.NugetId)
            {
                throw new Exception(
                    $"Attempt to mix and match 2 nugets stats ({other.NugetId} and {supplementalData.NugetId})");
            }

            this.PopularityRank = supplementalData.PopularityRank;
            this.HasReusableCompilerFlags = supplementalData.HasReusableCompilerFlags;
            this.PointsToRepo = supplementalData.PointsToRepo;
            this.PointsToSha = supplementalData.PointsToSha;
            this.NugetId = supplementalData.NugetId;
        }

        public int PopularityRank { get; set; }
        public bool HasReusableCompilerFlags { get; set; }
        public bool PointsToRepo { get; set; }
        public bool PointsToSha { get; set; }
    }

    internal static class BuildsComparisonStatsParser
    {
        public static IEnumerable<BuildsComparisonStats> FetchStats(string path)
        {
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            //csv.Context.RegisterClassMap<NugetStatsRecordMap>();
            foreach (var record in csv.GetRecords<BuildsComparisonStats>())
            {
                yield return record;
            }
        }

        public static IEnumerable<BuildsComparisonStatsEx> FetchExStats(string path)
        {
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            //csv.Context.RegisterClassMap<NugetStatsRecordMap>();
            foreach (var record in csv.GetRecords<BuildsComparisonStatsEx>())
            {
                yield return record;
            }
        }
    }
}
