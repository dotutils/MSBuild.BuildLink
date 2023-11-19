// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotUtils.MsBuild.BuildLink.SourceCodes
{
    internal class ToolingVersionInfo
    {
        // for deserialization
        private ToolingVersionInfo()
        { }

        public ToolingVersionInfo(bool globalJsonPresent)
        {
            GlobalJsonPresent = globalJsonPresent;
        }

        public ToolingVersionInfo(bool globalJsonPresent, string versionFromCompilerFlags)
        {
            GlobalJsonPresent = globalJsonPresent;
            VersionFromCompilerFlags = versionFromCompilerFlags;
        }

        public bool GlobalJsonPresent { get; init; }
        public string VersionFromCompilerFlags { get; init; }
    }
}
