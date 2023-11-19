// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DotUtils.MsBuild.BuildLink.NuGet;

namespace DotUtils.MsBuild.BuildLink.SourceCodes;

internal interface ISourceFetcher
{
    string FetchRepository(RepositoryMetadata repositoryMetadata, string destinationDir);
}
