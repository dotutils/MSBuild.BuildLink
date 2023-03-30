// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace NugetUtils
{
    public class SourceFetcher
    {
        private readonly string _sourcesRoot;

        public SourceFetcher(string sourcesRoot = "src") => _sourcesRoot = sourcesRoot;

        public string FetchRepo(GithubRepoLocationInfo locationInfo)
        {
            string localPath = Path.GetFullPath(Path.Combine(_sourcesRoot, GetLocalPath(locationInfo)));
            string repoPath;
            if (!Path.Exists(localPath))
            {
                repoPath = Repository.Clone(locationInfo.Location.ToString(), localPath,
                    new CloneOptions()
                    {
                        //BranchName = revision,
                        Checkout = false, RecurseSubmodules = true,
                    });
            }
            else
            {
                repoPath = Path.Combine(localPath, ".git");
            }

            using var repo = new Repository(repoPath);
            Commands.Checkout(repo, locationInfo.RevisionRef);
            return localPath;
        }

        //"https://github.com/libgit2/libgit2sharp.git"
        private static string GetLocalPath(GithubRepoLocationInfo locationInfo)
        {
            return $"{locationInfo.Owner}-{locationInfo.RepoName}";
        }
    }
}
