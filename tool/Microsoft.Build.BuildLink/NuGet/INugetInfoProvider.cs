// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using NuGetPe;
using System.Threading;

namespace Microsoft.Build.BuildLink.NuGet
{
    internal interface INugetInfoProvider
    {
        Task<NugetInfo> FetchNugetInfoAsync(NugetInfoRequest nugetInfoRequest, CancellationToken token);
    }
}
