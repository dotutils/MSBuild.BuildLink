// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.BuildLink.IO;
using Microsoft.Extensions.Logging;

namespace BuildUtils
{
    // TODO: might be more effective with Microsoft.IO.Redist and EnumerationOptions.IgnoreInaccessible
    // https://learn.microsoft.com/en-us/dotnet/api/system.io.enumerationoptions.ignoreinaccessible?view=net-7.0#system-io-enumerationoptions-ignoreinaccessible

    internal class FileSystemHelper : IFileSystemHelper
    {
        private readonly ILogger<FileSystemHelper> _logger;
        private readonly IFileSystem _fileSystem;

        public FileSystemHelper(ILogger<FileSystemHelper> logger, IFileSystem fileSystem) => (_logger, _fileSystem) = (logger, fileSystem);

        public void CopyFilesRecursively(string sourcePath, string targetPath, bool skipTopDotDirs = false, CancellationToken token = default)
        {
            foreach (string dirPath in EnumerateDirectories(sourcePath, SearchOption.AllDirectories, skipTopDotDirs, token))
            {
                _fileSystem.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            foreach (string newPath in EnumerateFiles(sourcePath, token: token))
            {
                _fileSystem.FileCopy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        public IEnumerable<string> EnumerateFiles(string inputDir, IReadOnlyCollection<string>? allowedExtensions = null, bool skipTopDotDirs = false, CancellationToken token = default)
        {
            var res = EnumerateFiles(inputDir, "*", SearchOption.AllDirectories, skipTopDotDirs, token);

            if (allowedExtensions != null && allowedExtensions.Any())
            {
                res = res.Where(file => allowedExtensions.Contains(Path.GetExtension(file), StringComparer.CurrentCultureIgnoreCase));
            }

            return res;
        }

        public IEnumerable<string> EnumerateFiles(string inputDir, string pattern, SearchOption searchOption, bool skipDotDirs = false, CancellationToken token = default)
        {
            return
                EnumerateFilesInternal(inputDir, pattern, SearchOption.TopDirectoryOnly, token)
                    .Concat(
                        searchOption == SearchOption.TopDirectoryOnly
                            ? Enumerable.Empty<string>()
                            : EnumerateTopLevelDirectories(inputDir, skipDotDirs, token)
                                .SelectMany(d => EnumerateFilesInternal(d, pattern, searchOption, token))
                    );
        }
        
        public IEnumerable<string> EnumerateDirectories(string inputDir, SearchOption searchOption, bool skipDotDirs = false, CancellationToken token = default)
        {
            // enumerate top - as list, then concat it with enumeration per each
            List<string> dirs = EnumerateTopLevelDirectories(inputDir, skipDotDirs, token);

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

        private IEnumerable<string> EnumerateFilesInternal(string inputDir, string pattern, SearchOption searchOption, CancellationToken token)
        {
            IEnumerable<string> files;
            // Need to realize enumeration with ToList - in case of errors
            if (!TryRunIoOperation(inputDir, () => _fileSystem.EnumerateFiles(inputDir, pattern, new EnumerationOptions()
                {
                    AttributesToSkip = default,
                    RecurseSubdirectories = searchOption == SearchOption.AllDirectories
            }).ToList(), token, out files))
            {
                files = EnumerateFilesHelper(inputDir, pattern, searchOption, token);
            }

            return files;
        }

        private static List<string> emptyList = new List<string>();

        private List<string> EnumerateTopLevelDirectories(string inputDir, bool skipDotDirs, CancellationToken token)
        {
            List<string> dirs;
            // Need to realize enumeration with ToList - in case of errors
            if (!TryRunIoOperation(inputDir, () => _fileSystem.EnumerateDirectories(inputDir, "*",
                        new EnumerationOptions()
                        {
                            AttributesToSkip = default,
                            RecurseSubdirectories = false
                        })
                    .Where(p => !skipDotDirs || !Path.GetFileName(p).StartsWith('.')).ToList(), token, out dirs))
            {
                dirs = emptyList;
            }

            return dirs;
        }

        private bool TryRunIoOperation<T>(string inputDir, Func<T> func, CancellationToken token, out T retVal)
        {
            token.ThrowIfCancellationRequested();
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
                _logger.LogError(ioe,
                    $"IO issue with path (possibly invalid junction point) {inputDir}, cannot search it recursively.");
            }
            finally
            {
                token.ThrowIfCancellationRequested();
            }

            retVal = default(T);
            return false;
        }

        private IEnumerable<string> EnumerateFilesHelper(string inputDir, string pattern, SearchOption searchOption, CancellationToken token)
        {
            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                return Enumerable.Empty<string>();
            }

            return
                EnumerateFilesInternal(inputDir, pattern, SearchOption.TopDirectoryOnly, token)
                    .Concat(
                        EnumerateTopLevelDirectories(inputDir, false, token)
                            .SelectMany(d => EnumerateFilesInternal(d, pattern, searchOption, token))
                    );
        }
    }
}
