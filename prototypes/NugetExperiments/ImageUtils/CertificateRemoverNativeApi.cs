// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace ImageUtils
{
    internal static class CertificateRemoverNativeApi
    {
        public const uint CERT_SECTION_TYPE_ANY = 255;

        [DllImport("Imagehlp.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ImageEnumerateCertificates(SafeFileHandle hFile, uint wTypeFilter,
            ref uint dwCertCount, IntPtr pIndices, IntPtr pIndexCount);

        [DllImport("Imagehlp.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ImageRemoveCertificate(SafeFileHandle hFile, uint dwCertCount);
    }
}
