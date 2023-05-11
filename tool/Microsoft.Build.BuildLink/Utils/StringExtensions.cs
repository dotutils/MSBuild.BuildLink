// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Build.BuildLink.Utils
{
    internal static class StringExtensions
    {
        public static string RemoveFromEnd(this string value, string tail, StringComparison comparison = StringComparison.CurrentCulture)
        {
            if (value.EndsWith(tail, comparison))
            {
                return value[..^tail.Length];
            }

            return value;
        }

        public static string RemoveAnyFromEnd(this string value, IEnumerable<string> tails, StringComparison comparison = StringComparison.CurrentCulture)
        {
            foreach (string tail in tails)
            {
                if (value.EndsWith(tail, comparison))
                {
                    return value[..^tail.Length];
                }
            }

            return value;
        }
    }
}
