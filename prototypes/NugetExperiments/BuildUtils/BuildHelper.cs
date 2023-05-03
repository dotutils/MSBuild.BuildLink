// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.BuildLink.CommandExecutionUtils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BuildUtils
{
    public enum SdkVer
    {
        net6,
        net7,
    }

    public class BuildHelperFactory
    {
        public const string DefaultRoot = @"C:\nugets-build-noSha";
        private readonly string _buildRoot;
        private readonly ILogger _logger;

        public BuildHelperFactory(ILogger logger, string buildRootRelativeOrAbsolute = DefaultRoot)
        {
            _buildRoot = Path.IsPathRooted(buildRootRelativeOrAbsolute)
                ? buildRootRelativeOrAbsolute
                : Path.Combine(DefaultRoot, buildRootRelativeOrAbsolute);
            _logger = logger;
        }

        public BuildHelper CreateBuildHelper(string repoRoot, string nugetId, BuildRecipe buildRecipe)
        {
            string nugetBuildRoot = Path.Combine(_buildRoot, nugetId);
            if (Directory.Exists(nugetBuildRoot) && Directory.EnumerateFiles(nugetBuildRoot, "*", SearchOption.AllDirectories).Any())
            {
                throw new Exception($"Build destination root directory already exists and is nonempty: {nugetBuildRoot}");
            }

            if (!Directory.Exists(repoRoot))
            {
                throw new Exception($"Repo root doesn't exist: {repoRoot}");
            }

            foreach (string buildFile in buildRecipe.BuildFiles.Values)
            {
                if (!File.Exists(buildFile))
                {
                    throw new Exception($"Build file doesn't exist: {buildFile}");
                }

                if (!buildFile.StartsWith(repoRoot, StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new Exception($"Build file [{buildFile}] isn't located within given repo root [{repoRoot}].");
                }
            }

            return new BuildHelper(nugetBuildRoot, repoRoot, nugetId, buildRecipe, _logger);
        }
    }

    public class BuildResult
    {
        public BuildResult(SdkVer sdkVer, BuildType buildType, bool succeeded, string buildFolder)
        {
            SdkVer = sdkVer;
            BuildType = buildType;
            Succeeded = succeeded;
            BuildFolder = buildFolder;
        }

        public SdkVer SdkVer { get; }
        public BuildType BuildType { get; }
        public bool Succeeded { get; }
        public string BuildFolder { get; }
    }

    public class BuildHelper
    {
        private readonly string _buildRoot;
        private readonly string _repoRoot;
        private readonly string _nugetId;
        private readonly BuildRecipe _buildRecipe;
        private readonly FileSystemHelper _fsHelper = new FileSystemHelper(NullLogger.Instance);
        private readonly ILogger _logger;
        private int initialized = 0;

        private const string GlobalJsonFmt = """
{{
  "sdk": {{
    "version": "{0}"
  }}
}}
""";

        internal BuildHelper(string buildRoot, string repoRoot, string nugetId, BuildRecipe buildRecipe, ILogger logger) =>
            (_buildRoot, _repoRoot, _nugetId, _buildRecipe, _logger) = (buildRoot, repoRoot, nugetId, buildRecipe, logger);

        public IEnumerable<BuildResult> RunBuilds(SdkVer? sdkVer = null)
        {
            IEnumerable<SdkVer> versions = sdkVer != null ? new[] { sdkVer.Value } : Enum.GetValues<SdkVer>();

            // Greedy enumerate - to prevent swapping with other tasks
            List<BuildResult> results = new List<BuildResult>();
            foreach (var sdk in versions)
            {
                foreach (var recipe in _buildRecipe.BuildFiles)
                {
                    results.Add(TryBuild(recipe.Key, sdk, recipe.Value));
                }
            }

            return results;
        }

        //Hardcoded hack (should get installed versions, possibly install needed one)
        private void PrepareDirs()
        {
            if (Interlocked.CompareExchange(ref initialized, 1, 0) == 1)
            {
                return;
            }

            Directory.CreateDirectory(GetBuildRoot(SdkVer.net6));
            File.WriteAllText(Path.Combine(GetBuildRoot(SdkVer.net6), "global.json"), string.Format(GlobalJsonFmt, "6.0.408"));

            Directory.CreateDirectory(GetBuildRoot(SdkVer.net7));
            File.WriteAllText(Path.Combine(GetBuildRoot(SdkVer.net7), "global.json"), string.Format(GlobalJsonFmt, "7.0.203"));
        }

        private string GetBuildRoot(SdkVer sdkVer) => Path.Combine(_buildRoot, sdkVer.ToString());

        private BuildResult TryBuild(BuildType buildType, SdkVer sdkVer, string buildFile)
        {
            PrepareDirs();

            string destFolder = Path.Combine(GetBuildRoot(sdkVer), buildType.ToShortString());

            if (Directory.Exists(destFolder))
            {
                throw new Exception($"Build destination directory already exists: {destFolder}");
            }

            _fsHelper.CopyFilesRecursively(_repoRoot, destFolder);
            buildFile = buildFile.Replace(_repoRoot, destFolder);

            TestCommand cmd;
            if (buildType == BuildType.BuildScript)
            {
                cmd = new PowershellCommand(_logger, buildFile);
            }
            else
            {
                cmd = new DotnetBuildCommand(_logger, buildFile, "-c", "Release");
            }

            cmd.WorkingDirectory = Path.GetDirectoryName(buildFile);
            cmd.MaxTimespanToWait = TimeSpan.FromMinutes(8);
            string cwd = Environment.CurrentDirectory;
            CommandResult result = cmd.Execute();
            Environment.CurrentDirectory = cwd;
            File.WriteAllText(Path.Combine(destFolder, "ReconstructionBuild.log"),
                $"Command args: {string.Join(' ', cmd.Arguments)}" + Environment.NewLine + result.ToString());
            //still might not mean success - as script can have errors but running of the powershell process succeeds
            bool succeeded = result.ExitCode == 0;

            return new BuildResult(sdkVer, buildType, succeeded, destFolder);
        }
    }
}
