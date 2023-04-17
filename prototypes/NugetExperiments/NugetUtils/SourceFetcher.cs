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
        public const string DefaultRoot = @"C:\src-nugets";
        public const string AlternativeRoot = @"C:\src-nugets-noref";
        private readonly string _sourcesRoot;
        

        public SourceFetcher(string sourcesRoot = DefaultRoot) => _sourcesRoot = sourcesRoot;

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

            // do not replace with using statement - as in debug mode it pushes dispose
            //  behind the leftover folder deletion which causes issues in LibGitSharp
            using (var repo = new Repository(repoPath))
            {
                Commands.Checkout(repo, GetCommitHashOrBranch(locationInfo.RevisionRef, repo));
            }

            string GetCommitHashOrBranch(string revisionRef, LibGit2Sharp.Repository repo)
            {
                return string.IsNullOrEmpty(locationInfo.RevisionRef)
                    ? (repo.Head.FriendlyName.Equals("(no branch)", StringComparison.InvariantCultureIgnoreCase)
                        ? repo.Head.Reference.TargetIdentifier
                        : repo.Head.FriendlyName)
                    : locationInfo.RevisionRef;
            }

            // https://github.com/dotnet/Nerdbank.GitVersioning/issues/396 workaround
            {
                string? leftoverDir = Directory.EnumerateDirectories(localPath).FirstOrDefault(p =>
                    Path.GetFileName(p).Contains("_git2_", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(leftoverDir))
                {
                    Directory.Delete(leftoverDir);
                }
            }

            return localPath;
        }

        //"https://github.com/libgit2/libgit2sharp.git"
        private static string GetLocalPath(GithubRepoLocationInfo locationInfo)
        {
            return $"{locationInfo.Owner}#{locationInfo.RepoName}";
        }
    }
}
