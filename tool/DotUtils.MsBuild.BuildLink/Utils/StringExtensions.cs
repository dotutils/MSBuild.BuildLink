﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotUtils.MsBuild.BuildLink.Utils
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Concatenates items of input sequence into csv string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">Sequence to be turned into csv string.</param>
        /// <param name="useSpace">Indicates whether space should be inserted between comas and following items.</param>
        /// <returns>Csv string.</returns>
        public static string ToCsvString<T>(this IEnumerable<T>? source, bool useSpace = true)
        {
            return source == null ? "<NULL>" : string.Join("," + (useSpace ? " " : string.Empty), source);
        }

        public static string RemoveFromStart(this string value, string prefix, StringComparison comparison = StringComparison.CurrentCulture)
        {
            if (value.StartsWith(prefix, comparison))
            {
                return value.Substring(prefix.Length);
            }

            return value;
        }

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
