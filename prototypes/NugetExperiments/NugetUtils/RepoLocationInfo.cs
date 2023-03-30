// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetUtils
{
    public class GithubRepoLocationInfo
    {
        public GithubRepoLocationInfo(string owner, string repoName, string revisionRef)
        {
            Owner = owner;
            RepoName = repoName;
            RevisionRef = revisionRef;
        }

        public string Owner { get; }
        public string RepoName { get; }
        public string RevisionRef { get; }
        public Uri Location => new Uri($"https://github.com/{Owner}/{RepoName}.git", UriKind.Absolute);
    }
}
