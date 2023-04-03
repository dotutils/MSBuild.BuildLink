// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BuildUtils
{
    //todo: abstract out
    public class FileSystemHelper
    {
        private readonly ILogger _logger;

        public FileSystemHelper(ILogger logger) => _logger = logger;

        public IEnumerable<string> EnumerateFiles(string inputDir, IReadOnlyCollection<string> allowedExtensions)
        {
            return EnumerateFiles(inputDir, "*", SearchOption.AllDirectories, true)
                .Where(file => allowedExtensions.Contains(Path.GetExtension(file)));
        }

        public IEnumerable<string> EnumerateFiles(string inputDir, string pattern, SearchOption searchOption, bool skipDotDirs = false)
        {
            return
                EnumerateFilesInternal(inputDir, pattern, SearchOption.TopDirectoryOnly)
                    .Concat(
                        searchOption == SearchOption.TopDirectoryOnly
                            ? Enumerable.Empty<string>()
                            : EnumerateDirectories(inputDir, skipDotDirs)
                                .SelectMany(d => EnumerateFilesInternal(d, pattern, searchOption))
                    );
        }

        private IEnumerable<string> EnumerateFilesInternal(string inputDir, string pattern, SearchOption searchOption)
        {
            IEnumerable<string> files;
            // Need to realize enumeration with ToList - in case of errors
            if (!TryRunIoOperation(inputDir, () => Directory.EnumerateFiles(inputDir, pattern, searchOption).ToList(), out files))
            {
                files = EnumerateFilesHelper(inputDir, pattern, searchOption);
            }

            return files;
        }

        private IEnumerable<string> EnumerateDirectories(string inputDir, bool skipDotDirs)
        {
            IEnumerable<string> files;
            // Need to realize enumeration with ToList - in case of errors
            if (!TryRunIoOperation(inputDir, () => Directory.EnumerateDirectories(inputDir).Where(p => !skipDotDirs || !Path.GetFileName(p).StartsWith('.')).ToList(), out files))
            {
                files = Enumerable.Empty<string>();
            }

            return files;
        }

        private bool TryRunIoOperation<T>(string inputDir, Func<T> func, out T retVal)
        {
            try
            {
                retVal = func();
                return true;
            }
            catch (ArgumentException ae)
            {
                _logger.LogError(ae, $"Invalid path {inputDir}, cannot search it recursively.");
            }
            catch (DirectoryNotFoundException dnfe)
            {
                _logger.LogError(dnfe, $"Directory {inputDir} is not found");
            }
            catch (PathTooLongException ptle)
            {
                _logger.LogError(ptle, $"Path is too long, cannot search recursively {inputDir} folder");
            }
            catch (UnauthorizedAccessException uae)
            {
                _logger.LogError(uae, $"Cannot authorize, cannot search recursively {inputDir} folder");
            }
            catch (IOException ioe)
            {
                _logger.LogError(ioe, $"IO issue with path (possibly invalid junction point) {inputDir}, cannot search it recursively.");
            }

            retVal = default(T);
            return false;
        }

        private IEnumerable<string> EnumerateFilesHelper(string inputDir, string pattern, SearchOption searchOption)
        {
            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                return Enumerable.Empty<string>();
            }

            return
                EnumerateFilesInternal(inputDir, pattern, SearchOption.TopDirectoryOnly)
                    .Concat(
                        EnumerateDirectories(inputDir, false)
                            .SelectMany(d => EnumerateFilesInternal(d, pattern, searchOption))
                    );
        }
    }
}
