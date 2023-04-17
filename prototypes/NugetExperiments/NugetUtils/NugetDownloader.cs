// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Compression;
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

        public async Task<string> DownloadAndExtractPackage(string packageName, string? version = null)
        {
            string fileName = await DownloadPackage(packageName, version);
            return ExtractPackage(fileName);
        }

        private async Task<string> DownloadPackage(string packageName, string? version = null)
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

        private string ExtractPackage(string filename)
        {
            string destinationDir = Path.Combine(_cacheDir, "extracted", Path.GetFileNameWithoutExtension(filename));

            if (!Directory.Exists(destinationDir))
            {
                ZipFile.ExtractToDirectory(filename, destinationDir);
                foreach (string file in Directory.EnumerateFiles(destinationDir))
                {
                    File.Delete(file);
                }

                foreach (string dir in Directory.EnumerateDirectories(destinationDir)
                             .Where(n => !Path.GetFileName(n).Equals("lib", StringComparison.CurrentCultureIgnoreCase)))
                {
                    Directory.Delete(dir, true);
                }
            }

            return Path.Combine(destinationDir, "lib");
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
