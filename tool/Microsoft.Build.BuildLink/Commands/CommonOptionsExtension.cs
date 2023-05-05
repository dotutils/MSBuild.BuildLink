// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static Microsoft.Build.BuildLink.Program;

namespace Microsoft.Build.BuildLink
{
    internal static class CommonOptionsExtension
    {
        private const VerbosityOptions DefaultConsoleVerbosity = VerbosityOptions.normal;
        private const VerbosityOptions DefaultFileVerbosity = VerbosityOptions.quiet;

        internal static readonly Option<VerbosityOptions> s_consoleVerbosityOption = new(
            new string[] { "-v", "--verbosity" },
            () => DefaultConsoleVerbosity,
            "Sets the verbosity level. Allowed values are q[uiet], m[inimal], n[ormal], and diag[nostic]. [default: normal]")
        {
            ArgumentHelpName = "LEVEL"
        };

        internal static readonly Option<VerbosityOptions> s_fileVerbosityOption = new(
            new string[] { "-fv", "--file-verbosity" },
            () => DefaultFileVerbosity,
            "Sets the verbosity level for logging into file log. Allowed values are q[uiet], m[inimal], n[ormal], and diag[nostic]. [default: quiet]")
        {
            ArgumentHelpName = "FILE_LEVEL",
            // Consider hiding this
            // IsHidden = true
        };

        public static VerbosityOptions GetConsoleVerbosityOption(this ParseResult parseResult)
            => parseResult.GetVerbosityOption(s_consoleVerbosityOption, DefaultConsoleVerbosity);

        public static VerbosityOptions GetFileVerbosityOption(this ParseResult parseResult)
            => parseResult.GetVerbosityOption(s_fileVerbosityOption, DefaultFileVerbosity);

        private static VerbosityOptions GetVerbosityOption(this ParseResult parseResult, Option<VerbosityOptions> option, VerbosityOptions @default)
        {
            OptionResult? verbosityOptionResult = parseResult.FindResultFor(option);
            VerbosityOptions verbosity = @default;

            if (verbosityOptionResult != null && !verbosityOptionResult.IsImplicit)
            {
                verbosity = verbosityOptionResult.GetValueOrDefault<VerbosityOptions>();
            }

            return verbosity;
        }

        public static bool IsDetailedOrDiagnostic(this VerbosityOptions verbosity)
        {
            return verbosity.Equals(VerbosityOptions.diag) ||
                verbosity.Equals(VerbosityOptions.diagnostic) ||
                verbosity.Equals(VerbosityOptions.d) ||
                verbosity.Equals(VerbosityOptions.detailed);
        }

        public static bool IsQuiet(this VerbosityOptions verbosity)
        {
            return verbosity.Equals(VerbosityOptions.q) ||
                verbosity.Equals(VerbosityOptions.quiet);
        }
        public static bool IsMinimal(this VerbosityOptions verbosity)
        {
            return verbosity.Equals(VerbosityOptions.m) ||
                verbosity.Equals(VerbosityOptions.minimal);
        }
        public static bool IsNormal(this VerbosityOptions verbosity)
        {
            return verbosity.Equals(VerbosityOptions.normal) ||
                verbosity.Equals(VerbosityOptions.n);
        }

        /// <summary>
        /// Converts <see cref="VerbosityOptions"/> to Microsoft.Extensions.Logging.<see cref="LogLevel"/>.
        /// </summary>
        public static LogLevel ToLogLevel(this VerbosityOptions verbosityOptions)
        {
            LogLevel logLevel = LogLevel.Information;
            switch (verbosityOptions)
            {
                case VerbosityOptions.d:
                case VerbosityOptions.detailed:
                    logLevel = LogLevel.Debug;
                    break;
                case VerbosityOptions.diag:
                case VerbosityOptions.diagnostic:
                    logLevel = LogLevel.Trace;
                    break;
                case VerbosityOptions.m:
                case VerbosityOptions.minimal:
                    logLevel = LogLevel.Error;
                    break;
                case VerbosityOptions.n:
                case VerbosityOptions.normal:
                    logLevel = LogLevel.Information;
                    break;
                case VerbosityOptions.q:
                case VerbosityOptions.quiet:
                    logLevel = LogLevel.None;
                    break;
            }
            return logLevel;
        }
    }
}
