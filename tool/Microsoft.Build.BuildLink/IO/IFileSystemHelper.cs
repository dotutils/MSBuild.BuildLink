// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BuildUtils;

internal interface IFileSystemHelper
{
    void CopyFilesRecursively(string sourcePath, string targetPath, bool skipTopDotDirs = false);
    IEnumerable<string> EnumerateFiles(string inputDir, IReadOnlyCollection<string>? allowedExtensions = null, bool skipTopDotDirs = false);
    IEnumerable<string> EnumerateFiles(string inputDir, string pattern, SearchOption searchOption, bool skipDotDirs = false);
    IEnumerable<string> EnumerateDirectories(string inputDir, SearchOption searchOption, bool skipDotDirs = false);
}
