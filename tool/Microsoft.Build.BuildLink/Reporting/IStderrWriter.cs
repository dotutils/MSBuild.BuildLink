// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Build.BuildLink.Reporting;

internal interface IStderrWriter
{
    void Write(string message);
    void WriteLine(string message);
    void WriteLine();
}
