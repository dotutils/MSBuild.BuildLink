// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

        public bool AreEqualExceptSignature(string imagePathOfficial, string imagePathLocal)
        {
            string unsignedCopy = _certificateRemover.GetUnsignedFile(imagePathOfficial);
            _peNullifier.NullifyPESignatureBytes(unsignedCopy);
            // local file should be unsigned
            return FileEquals(unsignedCopy, imagePathLocal);
            // TODO: File.Delete(unsignedCopy);
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
    }
}
