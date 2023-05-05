// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using LibGit2Sharp;
using Microsoft.Build.BuildLink.IO;
using Microsoft.Build.BuildLink.NuGet;
using Microsoft.Build.BuildLink.Reporting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Build.BuildLink.SourceCodes
{
    internal class SourceFetcher : ISourceFetcher
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<SourceFetcher> _logger;

        public SourceFetcher(IFileSystem fileSystem, ILogger<SourceFetcher> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public string FetchRepository(RepositoryMetadata repositoryMetadata, string destinationDir)
        {
            if (repositoryMetadata.Type.Equals("git", StringComparison.InvariantCultureIgnoreCase) &&
                repositoryMetadata.Type.Equals("github", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new BuildLinkException($"Unsupported repository type: {repositoryMetadata.Type}",
                    BuildLinkErrorCode.UnsupportedScenario);
            }

            try
            {
                string repoPath;
                bool repoExisted = _fileSystem.PathExists(destinationDir);
                if (!repoExisted)
                {
                    _logger.LogDebug("Cloning {Url} to {destinationDir}", repositoryMetadata.Url, destinationDir);
                    repoPath = Repository.Clone(repositoryMetadata.Url, destinationDir,
                        new CloneOptions()
                        {
                            //BranchName = revision,
                            Checkout = false,
                            RecurseSubmodules = true,
                        });
                }
                else
                {
                    repoPath = Path.Combine(destinationDir, ".git");
                    _logger.LogInformation("Destination directory [{destinationDir}] already exists - attempting to interpret it as git repository", destinationDir);
                    if (!_fileSystem.PathExists(repoPath))
                    {
                        throw new BuildLinkException(
                            $"The target path [{destinationDir}] exists, but it doesn't contain git repository.",
                            BuildLinkErrorCode.FileSystemWriteFailed);
                    }
                }

                // do not replace with using statement (unless wrapped in other block) - as in debug mode it pushes dispose
                //  behind the leftover folder deletion which causes issues in LibGitSharp
                using var repo = new Repository(repoPath);
                string commitHashOrBranch = GetCommitHashOrBranch(repositoryMetadata, repo);
                if (repoExisted)
                {
                    _logger.LogInformation(
                        "Attempting to checkout [{commitHashOrBranch}]. Local repository currently on {branch}@{commit}",
                        commitHashOrBranch, repo.Head.FriendlyName, repo.Head.Tip.Sha);
                }

                var status = repo.RetrieveStatus();
                if (status.IsDirty)
                {
                    throw new BuildLinkException(
                        $"Local source codes have uncommitted changes ({status.Modified.Count()} modifications, {status.Untracked.Count()} untracked, {status.Added.Count()} added, {status.Removed.Count()} removed), cannot proceed checking out",
                        BuildLinkErrorCode.FileSystemWriteFailed);
                }
                LibGit2Sharp.Commands.Checkout(repo, commitHashOrBranch);
            }
            catch (LibGit2SharpException e)
            {
                throw new BuildLinkException("Checking out of source codes failed: " + e.Message,
                    BuildLinkErrorCode.FileSystemWriteFailed, e);
            }

            string GetCommitHashOrBranch(RepositoryMetadata repositoryMetadata, LibGit2Sharp.Repository repo)
            {
                string revisionRef = string.IsNullOrEmpty(repositoryMetadata.Commit) ? repositoryMetadata.Branch : repositoryMetadata.Commit;
                return string.IsNullOrEmpty(revisionRef)
                    ? (repo.Head.FriendlyName.Equals("(no branch)", StringComparison.InvariantCultureIgnoreCase)
                        ? repo.Head.Reference.TargetIdentifier
                        : repo.Head.FriendlyName)
                    : revisionRef;
            }

            // https://github.com/dotnet/Nerdbank.GitVersioning/issues/396 workaround
            {
                string? leftoverDir = _fileSystem.EnumerateDirectories(destinationDir).FirstOrDefault(p =>
                    Path.GetFileName(p).Contains("_git2_", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(leftoverDir))
                {
                    _fileSystem.DeleteDirectory(leftoverDir);
                }
            }

            return destinationDir;
        }
    }
}
