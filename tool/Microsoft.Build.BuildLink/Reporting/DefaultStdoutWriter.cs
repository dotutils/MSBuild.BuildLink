// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Build.BuildLink.Reporting
{
    internal class DefaultStdoutWriter : IStdoutWriter
    {
        public void Write(string message) => Console.Out.Write(message);

        public void WriteLine(string message) => Console.Out.WriteLine(message);

        public void WriteLine() => Console.Out.WriteLine();
    }
}
