// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
//using Xunit.Abstractions;

namespace Microsoft.Build.BuildLink.CommandExecutionUtils
{
    internal class DotnetBuildCommand : DotnetCommand
    {
        internal DotnetBuildCommand(ILogger log, params string[] args) : base(log, "build", args)
        {
        }

        //internal DotnetBuildCommand(ITestOutputHelper log, params string[] args) : base(log, "build", args)
        //{
        //}
    }
}
