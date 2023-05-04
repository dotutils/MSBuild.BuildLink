// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Build.BuildLink.Reporting
{
    /// <summary>
    /// BuildLink error codes in thrown <see cref="BuildLinkException"/>. Correspond to BuildLink root command exit codes.
    ///
    /// Exit codes based on
    ///  * https://tldp.org/LDP/abs/html/exitcodes.html
    ///  * https://github.com/openbsd/src/blob/master/include/sysexits.h.
    /// related reference: dotnet new exit codes: https://aka.ms/templating-exit-codes.
    /// Future exit codes should be allocated in a range of 107 - 113. If not sufficient, a range of 79 - 99 may be used as well.
    /// </summary>
    public enum BuildLinkErrorCode
    {
        Success = 0,

        /// <summary>
        /// Indicates failed verification - assertions defined for the scenarios were not met.
        /// E.g. unexpected exit code, stdout/stderr output or created templates content.
        /// </summary>
        VerificationFailed = 65,

        /// <summary>
        /// Unexpected internal error in BuildLink. This might indicate a bug.
        /// </summary>
        InternalError = 70,

        /// <summary>
        /// Configured working directory already exists and is not empty - so command cannot proceed without destructive changes.
        /// </summary>
        FileSystemWriteFailed = 73,

        /// <summary>
        /// Unrecognized option(s) and/or argument(s) for a command.
        /// </summary>
        InvalidOption = 127,

        /// <summary>
        /// 
        /// </summary>
        OperationTerminatedByUser = 130,

        DotnetCommandError = 107,

        NotEnoughInformationToProceed = 108,

        UnsupportedScenario = 109,

    }
}
