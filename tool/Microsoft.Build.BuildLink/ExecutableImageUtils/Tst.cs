// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BuildUtils;
using Microsoft.Build.BuildLink.CommandExecutionUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Build.BuildLink.ExecutableImageUtils
{
    internal class Tst
    {
        public void Foo()
        {
            long.TryParse("123", out _);
        }
        
    }

    internal interface ISymbolLoader
    {
        public string TryFindSymbols(string assemblyPath);
        public string TryFindSymbols(string assemblyPath, out string details);
    }

    public class SymbolLoader : ISymbolLoader
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly ILogger<SymbolsFinder> _logger;
        private readonly IFileSystemHelper _fileSystemHelper;
        private readonly string _symbolsServerPath;
        private readonly IHashComputer _hashComputer;
        private readonly Dictionary<string, (string symbolPath, string symchkDetails)> _cache =
            new Dictionary<string, (string symbolPath, string symchkDetails)>();

        private const string _symchkPath = ".";
        private const string _symchk = "symchk";

        private const string _regexPattern =
            @"^SYMCHK: (?<file>.+?) \[(?<version>.+?)\s*\] (?<result>.+?)\s*- (?<details>(PDB: (?<pdb>.*?) DBG: )?.*)";

        private static readonly Regex _regex = new Regex(_regexPattern, RegexOptions.Compiled);

        public SymbolLoader(
            ICommandOptions<BundlerOptions> options,
            IProcessRunner processRunner,
            ILogger<SymbolsFinder> logger,
            IFileSystemHelper fileSystemHelper,
            IHashComputerBuilder hashComputerBuilder)
        {
            _processRunner = processRunner;
            _logger = logger;
            _fileSystemHelper = fileSystemHelper;
            _symbolsServerPath = options.Options.SymbolsServerPath;
            _hashComputer = hashComputerBuilder.WithAlgorithm("SHA256").Build();
            if (!string.IsNullOrEmpty(_symbolsServerPath))
            {
                CheckSymchkWorks();
            }
        }

        private void CheckSymchkWorks()
        {
            if (!_fileSystemHelper.TryFileExists("symchk.exe"))
                throw new Exception("Symchk tool not found - please make sure it's copied and runnable in working folder.");

            try
            {
                _processRunner.RunProcess(_symchk, _symchkPath, null, true, out int _);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    $"{_symchk} couldn't be run from working folder. Symbols finder cannot proceed.");
                throw;
            }
        }

        public string TryFindSymbols(string assemblyPath)
        {
            return TryFindSymbols(assemblyPath, out string _);
        }

        public string TryFindSymbols(string assemblyPath, out string details)
        {
            if (string.IsNullOrEmpty(_symbolsServerPath))
            {
                details = "[--symbols-server-path] not configured";
                return null;
            }

            string fileHash = _hashComputer.ComputeExactHashForFile(assemblyPath);
            (string symbolPath, string symchkDetails) result;
            if (!_cache.TryGetValue(fileHash, out result))
            {
                string output = string.Join(Environment.NewLine,
                    _processRunner.RunProcess(_symchk, _symchkPath,
                        $"\"{assemblyPath}\" /s \"{this._symbolsServerPath}\" /od /ov /os", out int _));

                var match = _regex.Match(output);
                details = match.Groups["result"].Value;
                if (string.IsNullOrEmpty(match.Groups["pdb"].Value))
                {
                    details += " - " + match.Groups["details"];
                }

                result = (symbolPath: match.Groups["pdb"].Value, symchkDetails: details.Trim());

                _cache[fileHash] = result;
            }

            details = result.symchkDetails;
            return result.symbolPath;
        }
    }
}
