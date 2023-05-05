// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Build.BuildLink.IO
{
    internal class PhysicalFileSystem : IFileSystem
    {
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);
        public bool FileExists(string path) => File.Exists(path);
        public bool PathExists(string path) => Path.Exists(path);

        public void RenameFile(string original, string @new) => File.Move(original, @new);
        public void DeleteDirectory(string path) => Directory.Delete(path);
        public IEnumerable<string> EnumerateDirectories(string dir) => Directory.EnumerateDirectories(dir);
    }
}
