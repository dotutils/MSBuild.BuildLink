// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NuGet.Versioning;
using System.Xml.Linq;
using NuGet.Packaging;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

namespace Microsoft.Build.BuildLink.NuGet
{
    /*
     *
     *     This code was copied from NugetPackageExplorer
     *     Alternatively the binaries from that project can be referenced (but those do not ship as nugets)
     * 
     * 
     * 
     */


    public static class XElementExtensions
    {
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
            Justification = "We don't care about base types")]
        public static string? GetOptionalAttributeValue(this XElement element, string localName,
                                                       string? namespaceName = null)
        {
            if (element is null)
                throw new ArgumentNullException(nameof(element));

            XAttribute? attr;
            if (string.IsNullOrEmpty(namespaceName))
            {
                attr = element.Attribute(localName!);
            }
            else
            {
                attr = element.Attribute(XName.Get(localName, namespaceName));
            }
            return attr?.Value;
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
            Justification = "We don't care about base types")]
        public static string? GetOptionalElementValue(this XElement element, string localName,
                                                     string? namespaceName = null)
        {
            if (element is null)
                throw new ArgumentNullException(nameof(element));
            XElement? child;
            if (string.IsNullOrEmpty(namespaceName))
            {
                child = element.Element(localName!);
            }
            else
            {
                child = element.Element(XName.Get(localName, namespaceName));
            }
            return child?.Value;
        }

        public static IEnumerable<XElement> ElementsNoNamespace(this XContainer container, string localName)
        {
            if (container is null)
                throw new ArgumentNullException(nameof(container));
            return container.Elements().Where(e => e.Name.LocalName == localName);
        }

        public static IEnumerable<XElement> ElementsNoNamespace(this IEnumerable<XContainer> source, string localName)
        {
            return source.Elements().Where(e => e.Name.LocalName == localName);
        }

        // REVIEW: We can use a stack if the perf is bad for Except and MergeWith
        public static XElement Except(this XElement source, XElement target)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (target is null)
            {
                return source;
            }

            var attributesToRemove = from e in source.Attributes()
                                     where AttributeEquals(e, target.Attribute(e.Name))
                                     select e;
            // Remove the attributes
            foreach (var a in attributesToRemove.ToList())
            {
                a.Remove();
            }

            foreach (var sourceChild in source.Elements().ToList())
            {
                var targetChild = FindElement(target, sourceChild);
                if (targetChild != null && !HasConflict(sourceChild, targetChild))
                {
                    Except(sourceChild, targetChild);
                    var hasContent = sourceChild.HasAttributes || sourceChild.HasElements;
                    if (!hasContent)
                    {
                        // Remove the element if there is no content
                        sourceChild.Remove();
                        targetChild.Remove();
                    }
                }
            }
            return source;
        }


        public static XElement MergeWith(this XElement source, XElement target)
        {
            return MergeWith(source, target, null);
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "No reason to create a new type")]
        public static XElement MergeWith(this XElement source, XElement? target,
                                         IDictionary<XName, Action<XElement, XElement>>? nodeActions)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            if (target == null)
            {
                return source;
            }

            // Merge the attributes
            foreach (var targetAttribute in target.Attributes())
            {
                var sourceAttribute = source.Attribute(targetAttribute.Name);
                if (sourceAttribute == null)
                {
                    source.Add(targetAttribute);
                }
            }

            // Go through the elements to be merged
            foreach (var targetChild in target.Elements())
            {
                var sourceChild = FindElement(source, targetChild);
                if (sourceChild != null && !HasConflict(sourceChild, targetChild))
                {
                    // Other wise merge recursively
                    sourceChild.MergeWith(targetChild, nodeActions);
                }
                else
                {
                    if (nodeActions != null && nodeActions.TryGetValue(targetChild.Name, out var nodeAction))
                    {
                        nodeAction(source, targetChild);
                    }
                    else
                    {
                        // If that element is null then add that node
                        source.Add(targetChild);
                    }
                }
            }
            return source;
        }

        private static XElement? FindElement(XElement source, XElement targetChild)
        {
            // Get all of the elements in the source that match this name
            var sourceElements = source.Elements(targetChild.Name).ToList();

            // Try to find the best matching element based on attribute names and values
            sourceElements.Sort((a, b) => Compare(targetChild, a, b));

            return sourceElements.FirstOrDefault();
        }

        private static int Compare(XElement target, XElement left, XElement right)
        {
            Debug.Assert(left.Name == right.Name);

            // First check how much attribute names and values match
            var leftExactMathes = CountMatches(left, target, AttributeEquals);
            var rightExactMathes = CountMatches(right, target, AttributeEquals);

            if (leftExactMathes == rightExactMathes)
            {
                // Then check which names match
                var leftNameMatches = CountMatches(left, target, (a, b) => a.Name == b.Name);
                var rightNameMatches = CountMatches(right, target, (a, b) => a.Name == b.Name);

                return rightNameMatches.CompareTo(leftNameMatches);
            }

            return rightExactMathes.CompareTo(leftExactMathes);
        }

        private static int CountMatches(XElement left, XElement right, Func<XAttribute, XAttribute, bool> matcher)
        {
            return (from la in left.Attributes()
                    from ta in right.Attributes()
                    where matcher(la, ta)
                    select la).Count();
        }

        private static bool HasConflict(XElement source, XElement target)
        {
            // Get all attributes as name value pairs
            var sourceAttr = source.Attributes().ToDictionary(a => a.Name, a => a.Value);
            // Loop over all the other attributes and see if there are
            foreach (var targetAttr in target.Attributes())
            {
                // if any of the attributes are in the source (names match) but the value doesn't match then we've found a conflict
                if (sourceAttr.TryGetValue(targetAttr.Name, out var sourceValue) && sourceValue != targetAttr.Value)
                {
                    return true;
                }
            }
            return false;
        }

        public static void RemoveAttributes(this XElement element, Func<XAttribute, bool> condition)
        {
            if (element is null)
                throw new ArgumentNullException(nameof(element));
            if (condition is null)
                throw new ArgumentNullException(nameof(condition));

            element.Attributes()
                .Where(condition)
                .ToList()
                .Remove();

            element.Descendants()
                .ToList()
                .ForEach(e => RemoveAttributes(e, condition));
        }

        private static bool AttributeEquals(XAttribute? source, XAttribute? target)
        {
            if (source is null && target is null)
            {
                return true;
            }

            if (source is null || target is null)
            {
                return false;
            }
            return source.Name == target.Name && source.Value == target.Value;
        }
    }

    public static class ManifestUtility
    {
        private const string TokenStart = "TOKENSTART";
        private const string TokenEnd = "TOKENEND";
        private const string TokenMetadataStart = "0.0.0-" + TokenStart + ".";
        private const string TokenMetadataEnd = "." + TokenEnd;
        private static readonly Regex TokenRegex = new Regex(@"([$])(?:(?=(\\?))\2.)*?\1", RegexOptions.Compiled);
        private static readonly Regex MetadataRegEx = new Regex($@"0\.0\.0\-{TokenStart}\.([^.]+)\.{TokenEnd}", RegexOptions.Compiled);

        public static Stream ReadManifest(string file)
        {
            using var str = File.OpenRead(file);
            return ReadManifest(str);
        }


        public static bool IsTokenized(this NuGetVersion version)
        {
            if (version is null)
                throw new System.ArgumentNullException(nameof(version));
            var labels = version.ReleaseLabels.ToList();

            return labels.Count >= 3 && labels[0] == TokenStart && labels[labels.Count - 1] == TokenEnd;
        }

        public static Stream ReadManifest(Stream stream)
        {
            // This method needs to replace tokens in version fields with a sentinel value
            // since the NuGetVersion object model doesn't support it.
            // Also needs to handle blank versions
            var xdoc = XDocument.Load(stream);
            var ns = xdoc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            // Get the version node
            var version = xdoc.Root?.Descendants(ns + "version").FirstOrDefault();
            if (version is not null)
            {
                version.Value = ReplaceTokenWithMetadata(version.Value);
            }

            // Get dependency nodes
            var deps = xdoc.Root?.Descendants(ns + "dependency");
            if (deps is not null)
            {
                foreach (var dep in deps)
                {
                    var val = dep.GetOptionalAttributeValue("version");
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        dep.SetAttributeValue("version"!, ReplaceTokenWithMetadata(val));
                    }
                    else
                    {
                        // Some packages (like Paket.Core have version="" in the dependencies. NuGet doesn't handle it, so remove it so we can load.
                        dep.RemoveAttributes(a => string.Equals("version", a.Name.LocalName, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }


            // The manifest utility exports licenseUrl for back compat, but it's not intended
            // for round-tripping
            // If license exists, strip licenseUrl


            if (xdoc.Root?.Descendants(ns + "license").Any() == true)
            {
                // Remove licenseUrl
                xdoc.Descendants(ns + "licenseUrl").Remove();
            }

            var ms = new MemoryStream();
            xdoc.Save(ms);
            ms.Position = 0;
            return ms;
        }

        /// <summary>
        /// Goes from $token$ to a marker value with 0.0.0+TOKENSTART.token.TOKENEND
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ReplaceTokenWithMetadata(string value)
        {
            if (value is null)
                throw new System.ArgumentNullException(nameof(value));

            // see if it's a token            

            var matches = TokenRegex.Matches(value);
#pragma warning disable CS8606 // Possible null reference assignment to iteration variable
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            foreach (Match match in matches)
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8606 // Possible null reference assignment to iteration variable
            {
                var token = match!.Value[1..^1];
                value = value.Replace(match.Value, $"{TokenMetadataStart}{token}{TokenMetadataEnd}", System.StringComparison.Ordinal);
            }

            return value;
        }

        /// <summary>
        /// Goes from marker 0.0.0+TOKENSTART.token.TOKENEND to the token $token$
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string? ReplaceMetadataWithToken(string? value)
        {
            if (value == null)
            {
                return value;
            }

            // see if it's a token
            var matches = MetadataRegEx.Matches(value);

#pragma warning disable CS8606 // Possible null reference assignment to iteration variable
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            foreach (Match match in matches)
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8606 // Possible null reference assignment to iteration variable
            {
                var token = match!.Value[TokenMetadataStart.Length..^TokenMetadataEnd.Length];
                value = value.Replace(match.Value, $"${token}$", System.StringComparison.Ordinal);
            }

            return value;
        }

        public static void SaveToStream(Stream sourceStream, Stream destinationStream)
        {
            // This method needs to replace tokens in version fields with a sentinel value
            // since the NuGetVersion object model doesn't support it.
            var xdoc = XDocument.Load(sourceStream);
            var ns = xdoc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            // Get the version node
            var version = xdoc.Root?.Descendants(ns + "version").FirstOrDefault();
            if (version is not null)
            {
                version.Value = ReplaceMetadataWithToken(version.Value)!;
            }

            // Get dependency nodes
            var deps = xdoc.Root?.Descendants(ns + "dependency");
            if (deps is not null)
            {
                foreach (var dep in deps)
                {
                    var val = dep.GetOptionalAttributeValue("version");
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        dep.SetAttributeValue("version"!, ReplaceMetadataWithToken(val));
                    }
                }
            }

            // The manifest utility exports licenseUrl for back compat, but it's not intended
            // for round-tripping
            // If license exists, strip licenseUrl


            if (xdoc.Root?.Descendants(ns + "license").Any() == true)
            {
                // Remove licenseUrl
                xdoc.Descendants(ns + "licenseUrl").Remove();
            }

            xdoc.Save(destinationStream);
        }
    }
}
