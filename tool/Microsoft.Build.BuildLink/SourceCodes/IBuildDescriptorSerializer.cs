// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Build.BuildLink.SourceCodes;

internal interface IBuildDescriptorSerializer
{
    string WriteToString(WorkingCopyBuildDescriptor buildDescriptor);
    string PrependProperty(string jsonString, string propertyName, string propertyValue);
    WorkingCopyBuildDescriptor? ReadFromString(string value);
    Task<WorkingCopyBuildDescriptor?> ReadFromFileAsync(string filePath, CancellationToken token);
    Task WriteToFile(string filePath, WorkingCopyBuildDescriptor buildDescriptor, CancellationToken token);
}
