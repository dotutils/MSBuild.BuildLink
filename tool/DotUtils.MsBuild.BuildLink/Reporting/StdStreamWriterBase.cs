// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotUtils.MsBuild.BuildLink.Reporting
{
    internal abstract class StdStreamWriterBase : IStdStreamWriter
    {
        protected abstract TextWriter Writer { get; }

        public virtual void Dispose() => Writer.Dispose();
        public void Write(string message) => Writer.Write(message);

        public void WriteLine(string message) => Writer.WriteLine(message);

        public void WriteLine() => Writer.WriteLine();
    }
}
