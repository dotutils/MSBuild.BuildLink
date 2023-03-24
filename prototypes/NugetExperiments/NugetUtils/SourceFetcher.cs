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

        public string FetchRepo(string remote, string revision)
        {
            remote = "https://" + remote;
            string localPath = Path.Combine(_sourcesRoot, GetLocalPath(remote));
            string repoPath;
            if (!Path.Exists(localPath))
            {
                repoPath = Repository.Clone(remote, Path.Combine(_sourcesRoot, GetLocalPath(remote)),
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
            Commands.Checkout(repo, revision);
            return localPath;
        }

        //"https://github.com/libgit2/libgit2sharp.git"
        private static string GetLocalPath(string remote)
        {
            if (remote.EndsWith(".git", StringComparison.InvariantCultureIgnoreCase))
            {
                remote = remote.Substring(0, remote.Length - ".git".Length);
            }

            return string.Join('-', remote.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).TakeLast(2));
        }
    }
}
