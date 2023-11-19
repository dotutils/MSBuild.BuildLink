// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DotUtils.MsBuild.BuildLink.Reporting
{
    [Serializable]
    public class BuildLinkException : Exception
    {
        public BuildLinkException(string message, BuildLinkErrorCode buildLinkErrorCode) : base(message)
        {
            BuildLinkErrorCode = buildLinkErrorCode;
        }

        public BuildLinkException(string message, BuildLinkErrorCode buildLinkErrorCode, Exception inner) : base(message, inner)
        {
            BuildLinkErrorCode = buildLinkErrorCode;
        }

        protected BuildLinkException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        public BuildLinkErrorCode BuildLinkErrorCode { get; init; }
    }
}
