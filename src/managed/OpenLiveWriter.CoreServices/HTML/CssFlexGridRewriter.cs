// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenLiveWriter.CoreServices.HTML
{
    /// <summary>
    /// Rewrites CSS flex/grid layout rules into IE9-compatible display:table equivalents.
    /// MSHTML is locked to IE9 emulation mode (required for Element Behaviors used by the editor),
    /// so display:flex and display:grid are silently ignored. This rewriter converts those rules
    /// so that columns appear side-by-side in the editor and preview tabs.
    /// </summary>
    public static class CssFlexGridRewriter
    {
        private static readonly Regex FlexGridDisplayPattern = new Regex(
            @"\bdisplay\s*:\s*(?:inline-)?(?:flex|grid)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Rewrites flex/grid rules in a CSS file in-place. No-op if the file contains no flex/grid.
        /// </summary>
        public static void RewriteFile(string cssFilePath)
        {
            try
            {
                string original = File.ReadAllText(cssFilePath, Encoding.UTF8);
                if (!FlexGridDisplayPattern.IsMatch(original))
                    return;

                string rewritten = RewriteCss(original);
                if (rewritten != original)
                    File.WriteAllText(cssFilePath, rewritten, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("CssFlexGridRewriter.RewriteFile failed for " + cssFilePath + ": " + ex.Message);
            }
        }

        /// <summary>
        /// Rewrites flex/grid rules in all &lt;style&gt; blocks within an HTML string.
        /// </summary>
        public static string RewriteStyleBlocks(string html)
        {
            if (string.IsNullOrEmpty(html) || !FlexGridDisplayPattern.IsMatch(html))
                return html;

            return Regex.Replace(html,
                @"(<style\b[^>]*>)([\s\S]*?)(</style>)",
                m => m.Groups[1].Value + RewriteCss(m.Groups[2].Value) + m.Groups[3].Value,
                RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Rewrites flex/grid display rules in a CSS string.
        /// For each rule with display:flex/grid:
        ///   - Converts display to display:table
        ///   - Removes incompatible flex/grid properties
        ///   - Emits an additional child rule with display:table-cell
        /// </summary>
        public static string RewriteCss(string css)
        {
            if (string.IsNullOrEmpty(css) || !FlexGridDisplayPattern.IsMatch(css))
                return css;

            var childRules = new StringBuilder();
            string result = RewriteBlock(css, childRules);
            return result + childRules.ToString();
        }

        // Parses a CSS block using brace counting (handles @media nesting).
        private static string RewriteBlock(string css, StringBuilder childRules)
        {
            var sb = new StringBuilder(css.Length + 256);
            int i = 0;

            while (i < css.Length)
            {
                // Skip /* ... */ comments
                if (i + 1 < css.Length && css[i] == '/' && css[i + 1] == '*')
                {
                    int end = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    if (end < 0) { sb.Append(css, i, css.Length - i); return sb.ToString(); }
                    sb.Append(css, i, end - i + 2);
                    i = end + 2;
                    continue;
                }

                int brace = css.IndexOf('{', i);
                if (brace < 0)
                {
                    sb.Append(css, i, css.Length - i);
                    break;
                }

                // Find the matching closing brace
                int depth = 1;
                int j = brace + 1;
                while (j < css.Length && depth > 0)
                {
                    if (j + 1 < css.Length && css[j] == '/' && css[j + 1] == '*')
                    {
                        int ce = css.IndexOf("*/", j + 2, StringComparison.Ordinal);
                        j = ce < 0 ? css.Length : ce + 2;
                        continue;
                    }
                    if (css[j] == '{') depth++;
                    else if (css[j] == '}') depth--;
                    j++;
                }

                string selector = css.Substring(i, brace - i);
                string inner = css.Substring(brace + 1, j - brace - 2);
                bool isAtRule = selector.TrimStart().StartsWith("@", StringComparison.Ordinal);

                if (isAtRule && selector.IndexOf("media", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Recurse into @media block, collect extra child rules inside the media block
                    var mediaExtra = new StringBuilder();
                    string processedMedia = RewriteBlock(inner, mediaExtra);
                    sb.Append(selector).Append('{');
                    sb.Append(processedMedia);
                    if (mediaExtra.Length > 0)
                        sb.Append(mediaExtra);
                    sb.Append('}');
                }
                else if (!isAtRule && FlexGridDisplayPattern.IsMatch(inner))
                {
                    RewriteFlexRule(selector, inner, sb, childRules);
                }
                else
                {
                    sb.Append(css, i, j - i);
                }

                i = j;
            }

            return sb.ToString();
        }

        private static void RewriteFlexRule(string selector, string body, StringBuilder result, StringBuilder childRules)
        {
            // Replace display:flex/grid with display:table
            body = FlexGridDisplayPattern.Replace(body, "display:table");

            // Remove flex-specific properties (they are meaningless/harmful under table layout)
            body = StripProperties(body,
                "flex-wrap", "flex-direction", "flex-flow", "flex-grow", "flex-shrink",
                "flex-basis", "flex", "align-items", "align-content", "align-self",
                "justify-content", "justify-items", "justify-self", "place-items",
                "place-content", "place-self");

            // Remove grid-specific properties
            body = StripProperties(body,
                "grid-template-columns", "grid-template-rows", "grid-template-areas",
                "grid-template", "grid-auto-columns", "grid-auto-rows", "grid-auto-flow",
                "grid-column", "grid-row", "grid-area", "grid-column-start",
                "grid-column-end", "grid-row-start", "grid-row-end", "grid");

            // Remove gap properties
            body = StripProperties(body, "gap", "column-gap", "row-gap");

            // Add table-layout for even column distribution
            if (body.IndexOf("table-layout", StringComparison.OrdinalIgnoreCase) < 0)
                body = body.TrimEnd().TrimEnd(';') + ";width:100%;table-layout:fixed";

            result.Append(selector).Append('{').Append(body).Append('}');

            // Generate a child rule so immediate children become table cells
            string trimSel = selector.Trim();
            if (!string.IsNullOrEmpty(trimSel))
            {
                // Build child selector for comma-separated groups
                var parts = trimSel.Split(',');
                var childParts = new List<string>(parts.Length);
                foreach (string part in parts)
                {
                    string p = part.Trim();
                    if (!string.IsNullOrEmpty(p))
                        childParts.Add(p + ">*");
                }
                if (childParts.Count > 0)
                {
                    childRules.Append(string.Join(",", childParts.ToArray()));
                    childRules.AppendLine("{display:table-cell!important;vertical-align:top}");
                }
            }
        }

        private static string StripProperties(string css, params string[] propertyNames)
        {
            foreach (string name in propertyNames)
            {
                // Match vendor-prefixed variants too (-webkit-, -ms-, etc.)
                string pattern = @"(?:^|(?<=;|{))\s*(?:-\w+-)?(?:" + Regex.Escape(name) + @")\s*:[^;]*;?";
                css = Regex.Replace(css, pattern, "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }
            return css;
        }
    }
}
