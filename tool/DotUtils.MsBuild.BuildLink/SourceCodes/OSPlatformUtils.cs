// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.CommandLine;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using DotUtils.MsBuild.BuildLink.Reporting;
using LibGit2Sharp.Handlers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace DotUtils.MsBuild.BuildLink.SourceCodes
{
    internal static class OSPlatformUtils
    {
        public static readonly OSPlatform AnyPlatform = OSPlatform.Create("ALL-PLATFORMS");

        private static readonly Dictionary<string, OSPlatform> s_platformMap = new Dictionary<string, OSPlatform>(StringComparer.OrdinalIgnoreCase)
        {
            { "Windows",  OSPlatform.Windows },
            { "Linux",  OSPlatform.Linux },
            { "OSX",  OSPlatform.OSX },
            { AnyPlatform.ToString(), AnyPlatform }
        };

        public static OSPlatform? Parse(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (s_platformMap.TryGetValue(value, out OSPlatform parsedValue))
            {
                return parsedValue;
            }

            throw new BuildLinkException(
                $"Unsupported value for Platform: {value}. Supported values: {string.Join(',', s_platformMap.Keys)}",
                BuildLinkErrorCode.UnsupportedScenario);
        }
    }
}
