﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Protocol.Plugins;

namespace Microsoft.Build.BuildLink.Reporting
{
    internal class DefaultStderrWriter : StdStreamWriterBase, IStderrWriter
    {
        protected override TextWriter Writer => Console.Error;
        public override void Dispose() => Writer.Flush();
    }
}
