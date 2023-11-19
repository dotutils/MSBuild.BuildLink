// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotUtils.MsBuild.BuildLink.NuGet
{
    public class RepositoryMetadata
    {
        public RepositoryMetadata(string type, string url, string branch, string commit)
        {
            Type = type;
            Url = url;
            Branch = branch;
            Commit = commit;
        }

        public static RepositoryMetadata? FromRepositoryMetadata(global::NuGet.Packaging.Core.RepositoryMetadata? other)
        {
            return other == null ? null : new RepositoryMetadata(other.Type, other.Url, other.Branch, other.Commit);
        }

        public string Type { get; init; }

        public string Url { get; init; }

        public string Branch { get; init; }

        public string Commit { get; init; }
    }

    internal class NugetInfo
    {
        public NugetInfo(string id, string version, RepositoryMetadata? repository, IEnumerable<string> libFiles, string downloadedFilePath)
        {
            Id = id;
            Version = version;
            Repository = repository;
            LibFiles = libFiles;
            DownloadedFilePath = downloadedFilePath;
        }

        public string Id { get; init; }
        public string Version { get; init; }
        public RepositoryMetadata? Repository { get; init; }
        public IEnumerable<string> LibFiles { get; init; }
        public string DownloadedFilePath { get; init; }
    }
}
