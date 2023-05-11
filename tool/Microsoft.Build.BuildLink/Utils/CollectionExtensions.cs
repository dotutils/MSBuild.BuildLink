// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Build.BuildLink.Utils
{
    internal static class CollectionExtensions
    {
        internal static IReadOnlyDictionary<string, TValue> FilterByKey<TValue>(
            this IReadOnlyDictionary<string, TValue> dict, string key,
            StringComparison comparison = StringComparison.CurrentCultureIgnoreCase)
            => dict.Where(kp => kp.Key.Equals(key, comparison)).ToDictionary(kp => kp.Key, kp => kp.Value);
    }
}
