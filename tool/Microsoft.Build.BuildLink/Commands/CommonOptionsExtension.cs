// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.BuildLink.Reporting;
using Microsoft.Build.BuildLink.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static Microsoft.Build.BuildLink.Program;

namespace Microsoft.Build.BuildLink
{
    internal static class CommonOptionsExtension
    {
        private const VerbosityOptions DefaultConsoleVerbosity = VerbosityOptions.normal;
        public const VerbosityOptions DefaultFileVerbosity = VerbosityOptions.quiet;

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

        private static bool IsGlobalVerbose()
        {
            bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE"), out bool globalVerbose);
            return globalVerbose;
        }

        public static VerbosityOptions GetConsoleVerbosityOptionOrDefault(this ParseResult parseResult)
            => parseResult.GetVerbosityOption(s_consoleVerbosityOption) ?? (IsGlobalVerbose() ? VerbosityOptions.diagnostic : DefaultConsoleVerbosity);

        public static VerbosityOptions? GetFileVerbosityOption(this ParseResult parseResult)
            => parseResult.GetVerbosityOption(s_fileVerbosityOption);

        public static VerbosityOptions GetFileVerbosityOptionOrDefault(this ParseResult parseResult)
            => parseResult.GetVerbosityOption(s_fileVerbosityOption) ?? DefaultFileVerbosity;

        private static VerbosityOptions? GetVerbosityOption(this ParseResult parseResult, Option<VerbosityOptions> option)
        {
            OptionResult? verbosityOptionResult = parseResult.FindResultFor(option);
            VerbosityOptions? verbosity = null;

            if (verbosityOptionResult != null && !verbosityOptionResult.IsImplicit)
            {
                verbosity = verbosityOptionResult.GetValueOrDefault<VerbosityOptions>();
            }

            return verbosity;
        }

        internal static IHostBuilder AddCancellationTokenProvider(this IHostBuilder builder)
        {
            if (!builder.Properties.TryGetValue(typeof(InvocationContext), out object? val) ||
                val is not InvocationContext invocationContext)
            {
                throw new BuildLinkException("HostBuilder doesn't contain InvocationContext",
                    BuildLinkErrorCode.InternalError);
            }

            builder.ConfigureServices(services =>
                services.AddSingleton(new CancellationTokenHolder(invocationContext.GetCancellationToken())));

            return builder;
        }

        internal static ILoggingBuilder ConfigureBuildLinkLogging(this ILoggingBuilder logging, IHostBuilder host)
        {
            logging.ClearProviders();

            ParseResult parseResult = (host.Properties[typeof(InvocationContext)] as InvocationContext).ParseResult;

            var consoleLogLevel = parseResult.GetConsoleVerbosityOptionOrDefault().ToLogLevel();
            var fileLogLevelOrNull = parseResult.GetFileVerbosityOption().ToLogLevel();
            var fileLogLevel = fileLogLevelOrNull ??
                               CommonOptionsExtension.DefaultFileVerbosity.ToLogLevel();


            if (consoleLogLevel < LogLevel.None)
            {
                logging.AddConsole();
            }

            // if logging is not specified on command line - try to see if specified in appsettings.json
            if (!fileLogLevelOrNull.HasValue || fileLogLevelOrNull < LogLevel.None)
            {
                //TODO: Arch: do we want to try to fetch app.json everytime when file logging is not explicitly disabled?
                //  the file logging is disabled by default, but what if someone wants to add app.json?
                //  maybe - allow the option without value?
                var loggingSection = FetchConfiguration()?.GetSection("Logging");
                if (loggingSection != null)
                {
                    if (fileLogLevelOrNull.HasValue)
                    {
                        loggingSection.GetSection("File")["MinLevel"] = fileLogLevelOrNull.ToString();
                    }

                    logging.AddFile(loggingSection);
                }
                else if (fileLogLevel < LogLevel.None)
                {
                    logging.AddFile("build-link.log", cfg =>
                    {
                        cfg.Append = true;
                        cfg.MinLevel = fileLogLevel;
                        cfg.FileSizeLimitBytes = 10000;
                        cfg.MaxRollingFiles = 3;
                    });
                }
            }

            var minLevel = (LogLevel)Math.Min((int)consoleLogLevel, (int)fileLogLevel);
            logging.SetMinimumLevel(minLevel);
            // get rid of chatty logs from system librarires
            logging.AddFilter("Microsoft", LogLevel.Warning);
            logging.AddFilter("Microsoft.Build.BuildLink", minLevel);

            return logging;
        }

        private static IConfigurationRoot? FetchConfiguration()
        {
            var currentDir = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(currentDir, "appsettings.json")))
            {
                currentDir = Directory.GetParent(currentDir)?.FullName;
                if (string.IsNullOrWhiteSpace(currentDir))
                {
                    return null;
                }
            }

            return new ConfigurationBuilder()
                .SetBasePath(currentDir)
                .AddJsonFile("appsettings.json", false)
                .Build();
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

        public static LogLevel? ToLogLevel(this VerbosityOptions? verbosityOptions) =>
            verbosityOptions?.ToLogLevel();
    }
}
