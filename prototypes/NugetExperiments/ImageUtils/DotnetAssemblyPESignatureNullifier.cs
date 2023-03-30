// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ImageUtils
{
    internal class DotnetAssemblyPESignatureNullifier
    {
        // TODO: abstract I/O
        private readonly ILogger<DotnetAssemblyPESignatureNullifier> logger;

        private const int Offset = 0x000000D8; // location of checksum
        private readonly byte[] bytesToWrite = new byte[4]; // empty bytes to write

        public DotnetAssemblyPESignatureNullifier(ILoggerFactory loggerFactory)
            : this(loggerFactory.CreateLogger<DotnetAssemblyPESignatureNullifier>())
        { }

        public DotnetAssemblyPESignatureNullifier(ILogger<DotnetAssemblyPESignatureNullifier> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// (TODO: Copies assembly to temp path)
        /// Nullifies PE checksum for .NET assembly and returns filepath for the copied file.
        /// Assembly has to be .NET otherwise this will rewritte 4 bytes in file from position 216.
        /// </summary>
        /// <exception cref="IOException">
        /// Thrown when new checksum is not nullified, i.e. assembly is not .NET.
        /// </exception>
        public void NullifyPESignatureBytes(string filePath)
        {
            var fileCheckSum = GetPEHeaderChecksum(filePath);
            if (fileCheckSum == 0)
            {
                logger.LogInformation("Checksum for file '{0}' is already 0, returning original path", filePath);
                return;
            }

            using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                var newPos = fileStream.Seek(Offset, SeekOrigin.Begin);
                if (newPos < 0)
                    throw new IOException($"Could not find offset {Offset} in file {filePath}");

                fileStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            }

            var newChecksum = GetPEHeaderChecksum(filePath);
            if (newChecksum != 0)
            {
                var errorMessage = string.Format("Checksum for file '{0}' was removed but resulting checksum is not 0. New checksum is {1}", filePath, newChecksum);
                logger.LogError(errorMessage);
                throw new IOException(errorMessage);
            }

            logger.LogInformation("PE signature checksum nullified for file {0}", filePath);
        }

        private static uint GetPEHeaderChecksum(string originalFileLocation)
        {
            using (var fileStream = File.Open(originalFileLocation, FileMode.Open, FileAccess.Read))
            {
                var peHeaders = new PEHeaders(fileStream);
                return peHeaders.PEHeader.CheckSum;
            }
        }
    }
}
