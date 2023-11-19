// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotUtils.MsBuild.BuildLink.IO;
using IFileSystem = DotUtils.MsBuild.BuildLink.IO.IFileSystem;

namespace DotUtils.MsBuild.BuildLink.Reporting
{
    internal class FileStreamFactory: IFileStreamFactory
    {
        private IO.IFileSystem _fileSystem;

        public FileStreamFactory(IO.IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public T CreateStreamWriter<T>(string path, FileCreateOptions fileCreateOptions) where T: class, IStdStreamWriter
        {
            if(fileCreateOptions == FileCreateOptions.ThrowIfExists && _fileSystem.FileExists(path))
            {
                throw new BuildLinkException($"Cannot write to file [{path}] as it already exists.", BuildLinkErrorCode.FileSystemWriteFailed);
            }
            
            return new FileStreamWriter(_fileSystem.CreateFileStream(path, fileCreateOptions == FileCreateOptions.Append)) as T;
        }

        private class FileStreamWriter : StdStreamWriterBase, IStdStreamWriter, IStdoutWriter, IStderrWriter
        {
            private readonly TextWriter _writer;
            public FileStreamWriter(TextWriter writer)
            {
                _writer = writer;
            }

            protected override TextWriter Writer => _writer;
        }
    }
}
