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

        public void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            foreach (string dirPath in EnumerateDirectories(sourcePath, SearchOption.AllDirectories, true))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            foreach (string newPath in EnumerateFiles(sourcePath))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        public IEnumerable<string> EnumerateFiles(string inputDir, IReadOnlyCollection<string>? allowedExtensions = null)
        {
            var res = EnumerateFiles(inputDir, "*", SearchOption.AllDirectories, true);

            if (allowedExtensions != null && allowedExtensions.Any())
            {
                res = res.Where(file => allowedExtensions.Contains(Path.GetExtension(file), StringComparer.CurrentCultureIgnoreCase));
            }

            return res;
        }

        public IEnumerable<string> EnumerateFiles(string inputDir, string pattern, SearchOption searchOption, bool skipDotDirs = false)
        {
            return
                EnumerateFilesInternal(inputDir, pattern, SearchOption.TopDirectoryOnly)
                    .Concat(
                        searchOption == SearchOption.TopDirectoryOnly
                            ? Enumerable.Empty<string>()
                            : EnumerateTopLevelDirectories(inputDir, skipDotDirs)
                                .SelectMany(d => EnumerateFilesInternal(d, pattern, searchOption))
                    );
        }
        
        public IEnumerable<string> EnumerateDirectories(string inputDir, SearchOption searchOption, bool skipDotDirs = false)
        {
            // enumerate top - as list, then concat it with enumeration per each
            List<string> dirs = EnumerateTopLevelDirectories(inputDir, skipDotDirs);

            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                return dirs;
            }

            return
                dirs
                    .Concat(
                        dirs
                            .SelectMany(d => EnumerateDirectories(d, SearchOption.AllDirectories, false))
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

        private static List<string> emptyList = new List<string>();

        private List<string> EnumerateTopLevelDirectories(string inputDir, bool skipDotDirs)
        {
            List<string> dirs;
            // Need to realize enumeration with ToList - in case of errors
            if (!TryRunIoOperation(inputDir, () => Directory.EnumerateDirectories(inputDir).Where(p => !skipDotDirs || !Path.GetFileName(p).StartsWith('.')).ToList(), out dirs))
            {
                dirs = emptyList;
            }

            return dirs;
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
                        EnumerateTopLevelDirectories(inputDir, false)
                            .SelectMany(d => EnumerateFilesInternal(d, pattern, searchOption))
                    );
        }
    }
}
