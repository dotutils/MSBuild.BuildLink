// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotUtils.MsBuild.BuildLink.CommandExecutionUtils;
using Microsoft.Extensions.Logging;

namespace BuildUtils
{
    public class GetSourcesHelper
    {
        private ILogger _logger;

        public GetSourcesHelper(ILogger logger)
        {
            _logger = logger;
        }

        public GetSourcesHelper()
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<GetSourcesHelper>();
        }

        public void GetSources(IEnumerable<string> packageNames)
        {
            string outputFile = "build-links.json";
            foreach (var package in packageNames)
            {
                AppendLine(outputFile, $"----------------- {package} ---------------------");
                GetSourcesCommand gsc = new GetSourcesCommand(package, outputFile, _logger);
                var res = gsc.Execute();
                AppendLine(outputFile, $"----------------- {package} exit code: {res.ExitCode} ---------------------");
            }
        }

        private static void AppendLine(string file, string line)
        {
            using (StreamWriter w = File.AppendText(file))
            {
                w.WriteLine(line);
            }
        }

        //public void GetSources(string packageName, string outputFile)
        //{
        //    GetSourcesCommand gsc = new GetSourcesCommand(packageName, outputFile, _logger);
        //    var res = gsc.Execute();
            
        //}

        private class GetSourcesCommand : ExecutableCommand
        {
            public GetSourcesCommand(string packageName, string outputFile, ILogger log)
                : base(log)
            {
                Arguments.Add("get-sources");
                Arguments.Add(packageName);

                Arguments.Add("--checkout-base-dir");
                Arguments.Add("C:\\src-trsh");
                Arguments.Add("--flush-buildlink-path");
                Arguments.Add(outputFile);
                Arguments.Add("--buildlink-append");
                Arguments.Add("-vq");
                Arguments.Add("-fv");
                Arguments.Add("diag");
            }

            protected override string ExecutableFilePath => "\"C:\\src\\PackagesSandbox\\tool\\Microsoft.Build.BuildLink\\bin\\Debug\\net7.0\\Microsoft.Build.BuildLink.exe\"";
        }
    }
}
