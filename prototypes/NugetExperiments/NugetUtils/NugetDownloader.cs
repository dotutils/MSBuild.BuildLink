// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using Newtonsoft.Json.Linq;

namespace NugetUtils
{
    public class NugetDownloader
    {
        private readonly string _baseUrl;
        private readonly string _cacheDir;

        public NugetDownloader(string? baseUrl = null, string? cacheDir = null)
        {
            _baseUrl = baseUrl ?? "https://api.nuget.org/v3-flatcontainer/";
            _cacheDir = cacheDir ?? @"C:\nugets";
        }

        //public NugetDownloader() { }

        public async Task<string> DownloadPackage(string packageName, string? version = null)
        {
            version ??= await GetLatestVersion(packageName);

            string fileName = Path.Combine(_cacheDir, $"{packageName}-{version}.zip");

            if (!File.Exists(fileName))
            {
                string downloadEndpoint = $"{_baseUrl}{packageName}/{version}/{packageName}.{version}.nupkg".ToLower();
                await HttpClientExtensions.SharedHttpClient.DownloadFileAsync(new Uri(downloadEndpoint), fileName);
            }

            return fileName;
        }

        public async Task<string> GetLatestVersion(string packageName)
        {
            string json = await HttpClientExtensions.SharedHttpClient.GetStringAsync($"{_baseUrl}{packageName}/index.json");
            var obj = JObject.Parse(json);
            string latestVersion = obj.Values().First(v => v.Path == "versions").Last().ToString();
            return latestVersion;
        }
    }
}
