// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.BuildLink.NuGet;

namespace Microsoft.Build.BuildLink.SourceCodes;

internal interface ISourceFetcher
{
    string FetchRepository(RepositoryMetadata repositoryMetadata, string destinationDir);
}
