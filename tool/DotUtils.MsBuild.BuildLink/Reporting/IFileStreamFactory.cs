// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotUtils.MsBuild.BuildLink.IO;

namespace DotUtils.MsBuild.BuildLink.Reporting
{
    internal interface IFileStreamFactory
    {
        T CreateStreamWriter<T>(string path, FileCreateOptions fileCreateOptions) where T : class, IStdStreamWriter;
    }
}
