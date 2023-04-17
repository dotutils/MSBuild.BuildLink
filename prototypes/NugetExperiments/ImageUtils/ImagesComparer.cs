// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ImageUtils
{
    public class ImagesComparer
    {
        private readonly DotnetAssemblyPESignatureNullifier _peNullifier;
        private readonly CertificateRemover _certificateRemover;

        public ImagesComparer(ILoggerFactory loggerFactory)
        {
            _peNullifier = new DotnetAssemblyPESignatureNullifier(loggerFactory);
            _certificateRemover = new CertificateRemover(loggerFactory);
        }

        public static bool AreContentsIdentical(string file1, string file2)
            => File.ReadAllBytes(file1).SequenceEqual(File.ReadAllBytes(file2));

        public bool AreEqualExceptSignature(string imagePathOfficial, string imagePathLocal)
        {
            // local file should be unsigned - but it might not
            return FileEquals(GetUnsignedCopy(imagePathOfficial), GetUnsignedCopy(imagePathLocal));
            // TODO: File.Delete(unsignedCopy); - if it's copied
        }

        public double GetSimilarityScore(string imagePathOfficial, string imagePathLocal)
        {
            string officialUnsigned = GetUnsignedCopy(imagePathOfficial);
            string localUnsigned = GetUnsignedCopy(imagePathLocal);

            string similarities = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

            if (!WindiffWrapper.DumpSimilaritiesFile(officialUnsigned, localUnsigned, similarities))
            {
                return -1;
            }

            string localProcessed = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            if (!WindiffWrapper.DumpSimilaritiesFile(localUnsigned, localUnsigned, localProcessed))
            {
                localProcessed = localUnsigned;
            }

            long localSize = new FileInfo(localProcessed).Length;
            long similaritiesSize = new FileInfo(similarities).Length;

            return ((double)similaritiesSize) / localSize;
        }

        private string GetUnsignedCopy(string filePath)
        {
            string unsignedCopy = _certificateRemover.GetUnsignedFile(filePath);
            _peNullifier.NullifyPESignatureBytes(unsignedCopy);

            return unsignedCopy;
        }

        static bool FileEquals(string fileName1, string fileName2)
        {
            // Check the file size and CRC equality here.. if they are equal...    
            using var file1 = new FileStream(fileName1, FileMode.Open);
            using var file2 = new FileStream(fileName2, FileMode.Open);
            return FileStreamEquals(file1, file2);
        }

        static bool FileStreamEquals(Stream stream1, Stream stream2)
        {
            return SHA256.Create().ComputeHash(stream1).Equals(SHA256.Create().ComputeHash(stream2));
        }

        static bool FileStreamEquals2(Stream stream1, Stream stream2)
        {
            const int bufferSize = 2048;
            byte[] buffer1 = new byte[bufferSize]; //buffer size
            byte[] buffer2 = new byte[bufferSize];
            while (true)
            {
                int count1 = stream1.Read(buffer1, 0, bufferSize);
                int count2 = stream2.Read(buffer2, 0, bufferSize);

                if (count1 != count2)
                    return false;

                if (count1 == 0)
                    return true;

                // You might replace the following with an efficient "memcmp"
                if (!buffer1.Take(count1).SequenceEqual(buffer2.Take(count2)))
                    return false;
            }
        }


        private static class WindiffWrapper
        {
            private static readonly bool _isWindiffAvailable = IsWindiffAvailable();

            public static bool DumpSimilaritiesFile(string file1, string file2, string similaritiesFileToCreate)
            {
                if (!_isWindiffAvailable)
                {
                    return false;
                }

                try
                {
                    return RunWindiff($"-FIX \"{similaritiesFileToCreate}\" \"{file1}\" \"{file2}\"");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            private static bool IsWindiffAvailable()
            {
                return RunWindiff(string.Empty);
            }

            private static bool RunWindiff(string args)
            {
                var p = new ProcessStartInfo
                {
                    FileName = "windiff",
                    Arguments = args,
                    //RedirectStandardInput = true,
                    //RedirectStandardError = true,
                    //RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                var process = Process.Start(p);

                if (process == null)
                {
                    return false;
                }

                if (string.IsNullOrEmpty(args) || !process.WaitForExit(TimeSpan.FromSeconds(2*60)))
                {
                    process.Kill(true);
                }

                return string.IsNullOrEmpty(args) || process.ExitCode == 0;
            }
        }
    }
}
