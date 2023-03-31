// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ImageUtils
{
    internal class CertificateRemover
    {
        // TODO: abstract I/O
        private readonly ILogger<CertificateRemover> _logger;

        public CertificateRemover(ILoggerFactory loggerFactory)
            : this(loggerFactory.CreateLogger<CertificateRemover>())
        { }

        public CertificateRemover(ILogger<CertificateRemover> logger)
        {
            _logger = logger;
        }

        public string GetUnsignedFile(string originalFile)
        {
            string unsignedFileName = null;

            try
            {
                uint certCount = 0;
                using (var originalFileStream = File.Open(originalFile, FileMode.Open, FileAccess.Read))
                {
                    if (!CertificateRemoverNativeApi.ImageEnumerateCertificates(originalFileStream.SafeFileHandle, CertificateRemoverNativeApi.CERT_SECTION_TYPE_ANY, ref certCount, IntPtr.Zero, IntPtr.Zero))
                    {
                        _logger.LogInformation($"{originalFile} is not a PE32/64 signed file.");
                        return originalFile;
                    }
                    else
                    {
                        if (certCount <= 0)
                        {
                            _logger.LogInformation($"{originalFile} is not signed.");
                            return originalFile;
                        }
                    }
                }

                var tempPath = Path.GetTempPath();

                var tempFileName =
                    $"{Path.GetFileNameWithoutExtension(originalFile)}.{DateTime.UtcNow.Ticks}{Path.GetExtension(originalFile)}";

                unsignedFileName = Path.Combine(tempPath, tempFileName);

                File.Copy(originalFile, unsignedFileName);
                File.SetAttributes(unsignedFileName, FileAttributes.Normal);

                using (var unsignedFileStream = File.Open(unsignedFileName, FileMode.Open, FileAccess.ReadWrite))
                {
                    for (uint certIndex = 0; certIndex < certCount; certIndex++)
                    {
                        if (!CertificateRemoverNativeApi.ImageRemoveCertificate(unsignedFileStream.SafeFileHandle, certIndex))
                        {
                            _logger.LogInformation($"Could not remove certificate from {originalFile} file.");
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                        }
                    }
                }

                return unsignedFileName;
            }
            catch (Exception)
            {
                if (unsignedFileName != null)
                {
                    File.Delete(unsignedFileName);
                }

                throw;
            }
        }
    }
}
