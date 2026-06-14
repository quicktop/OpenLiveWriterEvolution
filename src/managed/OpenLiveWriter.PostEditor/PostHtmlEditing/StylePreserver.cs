// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using OpenLiveWriter.HtmlParser.Parser;

namespace OpenLiveWriter.PostEditor.PostHtmlEditing
{
    /// <summary>
    /// Preserves CSS flex/grid inline styles across MSHTML's IE9-mode editing pass.
    ///
    /// Strategy:
    ///   PreserveStyleAttributes (called before MSHTML loads the content):
    ///     - For each element with style="display:flex/grid …":
    ///         1. Saves the original value in a wlstyle attribute (for restoration on save).
    ///         2. Rewrites the style attribute to display:table + compatible properties
    ///            so MSHTML renders columns side-by-side without CSS magic.
    ///         3. Adds the marker class "olw-fc" (flex column container) so that the
    ///            injected CSS rule ".olw-fc > *" can give direct children display:table-cell.
    ///     - Direct children of flex containers are identified via depth tracking and get
    ///       display:table-cell prepended to their style (belt-and-suspenders on top of CSS).
    ///
    ///   RestoreStyleAttributes (called when retrieving HTML for save/publish):
    ///     - Restores original style from wlstyle.
    ///     - Removes the olw-fc marker class.
    ///     - Strips the display:table-cell sentinel added to child styles.
    ///
    /// CSS companion (injected by PostHtmlEditingSettings):
    ///     .olw-fc > * { display:table-cell !important; vertical-align:top; }
    /// </summary>
    public static class StylePreserver
    {
        // CSS class added to flex/grid containers so the injected stylesheet can target children
        private const string FlexContainerClass = "olw-fc";

        // Sentinel prefix added to direct-child style values so we can strip it on restore
        private const string ChildSentinel = "display:table-cell;vertical-align:top;";

        private static readonly Regex FlexGridDisplayRx = new Regex(
            @"\bdisplay\s*:\s*(?:inline-)?(?:flex|grid)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // HTML void elements that never have a matching end tag
        private static readonly HashSet<string> VoidElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "area","base","br","col","embed","hr","img","input","keygen",
            "link","meta","param","source","track","wbr"
        };

        // ------------------------------------------------------------------ //
        //  PreserveStyleAttributes
        // ------------------------------------------------------------------ //

        public static string PreserveStyleAttributes(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            var sb = new StringBuilder(html.Length + html.Length / 4);
            var parser = new SimpleHtmlParser(html);

            // Stack of absolute depths at which flex containers were opened
            var flexContainerDepths = new Stack<int>();
            int depth = 0;

            Element el;
            while ((el = parser.Next()) != null)
            {
                if (el is BeginTag bt)
                {
                    bool isVoid = bt.Complete || VoidElements.Contains(bt.Name);
                    string styleValue = bt.GetAttributeValue("style") ?? string.Empty;
                    bool isFlex = FlexGridDisplayRx.IsMatch(styleValue);

                    // Is this a DIRECT child of an open flex container?
                    bool isFlexChild = !isFlex &&
                                       flexContainerDepths.Count > 0 &&
                                       depth == flexContainerDepths.Peek() + 1;

                    if (isFlex)
                    {
                        EmitFlexContainer(sb, bt, styleValue);
                        if (!isVoid)
                        {
                            flexContainerDepths.Push(depth);
                            depth++;
                        }
                    }
                    else if (isFlexChild)
                    {
                        EmitFlexChild(sb, bt, styleValue);
                        if (!isVoid) depth++;
                    }
                    else
                    {
                        sb.Append(bt.ToString());
                        if (!isVoid) depth++;
                    }
                }
                else if (el is EndTag et)
                {
                    depth--;
                    // Pop the stack if this closing tag exits a flex container
                    if (flexContainerDepths.Count > 0 && depth == flexContainerDepths.Peek())
                        flexContainerDepths.Pop();
                    sb.Append(et.ToString());
                }
                else
                {
                    sb.Append(el.ToString());
                }
            }

            return sb.ToString();
        }

        // ------------------------------------------------------------------ //
        //  RestoreStyleAttributes
        // ------------------------------------------------------------------ //

        public static string RestoreStyleAttributes(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            var sb = new StringBuilder(html.Length);
            var parser = new SimpleHtmlParser(html);

            Element el;
            while ((el = parser.Next()) != null)
            {
                if (el is BeginTag bt)
                {
                    string wlStyle = bt.GetAttributeValue("wlstyle");
                    string currentStyle = bt.GetAttributeValue("style") ?? string.Empty;
                    bool needsRestore = wlStyle != null;
                    bool hasChildSentinel = currentStyle.StartsWith(ChildSentinel, StringComparison.OrdinalIgnoreCase);

                    if (needsRestore || hasChildSentinel)
                    {
                        sb.Append("<").Append(bt.Name);
                        foreach (Attr attr in bt.Attributes)
                        {
                            if (attr == null) continue;
                            if (attr.NameEquals("wlstyle")) continue;

                            if (attr.NameEquals("style"))
                            {
                                // Container: restore original from wlstyle
                                // Child sentinel: strip the prepended table-cell prefix
                                string restored = needsRestore ? wlStyle : currentStyle.Substring(ChildSentinel.Length);
                                if (!string.IsNullOrWhiteSpace(restored))
                                    sb.Append(" style=\"").Append(HtmlUtils.EscapeEntities(restored)).Append("\"");
                                continue;
                            }

                            if (attr.NameEquals("class"))
                            {
                                string cls = RemoveOlwClass(attr.Value ?? string.Empty);
                                if (!string.IsNullOrWhiteSpace(cls))
                                    sb.Append(" class=\"").Append(HtmlUtils.EscapeEntities(cls)).Append("\"");
                                continue;
                            }

                            sb.Append(" ").Append(attr.ToString());
                        }
                        if (bt.HasResidue) sb.Append(bt.Residue);
                        if (bt.Complete) sb.Append(" /");
                        sb.Append(">");
                    }
                    else
                    {
                        sb.Append(bt.ToString());
                    }
                }
                else
                {
                    sb.Append(el.ToString());
                }
            }

            return sb.ToString();
        }

        // ------------------------------------------------------------------ //
        //  Private helpers
        // ------------------------------------------------------------------ //

        private static void EmitFlexContainer(StringBuilder sb, BeginTag bt, string originalStyle)
        {
            // Compute IE9-compatible style: replace display:flex/grid → display:table
            string ie9Style = ConvertContainerStyle(originalStyle);

            sb.Append("<").Append(bt.Name);

            bool hadStyle = false;
            bool hadClass = false;

            foreach (Attr attr in bt.Attributes)
            {
                if (attr == null) continue;
                if (attr.NameEquals("wlstyle")) continue;

                if (attr.NameEquals("style"))
                {
                    sb.Append(" style=\"").Append(HtmlUtils.EscapeEntities(ie9Style)).Append("\"");
                    hadStyle = true;
                    continue;
                }

                if (attr.NameEquals("class"))
                {
                    string cls = AddOlwClass(attr.Value ?? string.Empty);
                    sb.Append(" class=\"").Append(HtmlUtils.EscapeEntities(cls)).Append("\"");
                    hadClass = true;
                    continue;
                }

                sb.Append(" ").Append(attr.ToString());
            }

            if (!hadStyle && !string.IsNullOrEmpty(ie9Style))
                sb.Append(" style=\"").Append(HtmlUtils.EscapeEntities(ie9Style)).Append("\"");

            if (!hadClass)
                sb.Append(" class=\"").Append(FlexContainerClass).Append("\"");

            // Preserve original for round-trip restoration
            sb.Append(" wlstyle=\"").Append(HtmlUtils.EscapeEntities(originalStyle)).Append("\"");

            if (bt.HasResidue) sb.Append(bt.Residue);
            if (bt.Complete) sb.Append(" /");
            sb.Append(">");
        }

        private static void EmitFlexChild(StringBuilder sb, BeginTag bt, string originalStyle)
        {
            // Prepend table-cell display to child's style so it renders side-by-side.
            // ChildSentinel prefix makes it easy to strip on RestoreStyleAttributes.
            string childStyle = ChildSentinel + originalStyle;
            // Remove flex-item-specific properties that are meaningless under table layout
            childStyle = StripFlexItemProperties(childStyle);

            sb.Append("<").Append(bt.Name);

            bool hadStyle = false;
            foreach (Attr attr in bt.Attributes)
            {
                if (attr == null) continue;
                if (attr.NameEquals("style"))
                {
                    sb.Append(" style=\"").Append(HtmlUtils.EscapeEntities(childStyle)).Append("\"");
                    hadStyle = true;
                    continue;
                }
                sb.Append(" ").Append(attr.ToString());
            }
            if (!hadStyle)
                sb.Append(" style=\"").Append(HtmlUtils.EscapeEntities(childStyle)).Append("\"");

            if (bt.HasResidue) sb.Append(bt.Residue);
            if (bt.Complete) sb.Append(" /");
            sb.Append(">");
        }

        private static string ConvertContainerStyle(string style)
        {
            // Replace display:flex/grid with display:table
            string result = FlexGridDisplayRx.Replace(style, "display:table");

            // Remove flex/grid-specific properties that have no meaning in table layout
            result = Regex.Replace(result, @"\bflex(?:-wrap|-direction|-flow|-grow|-shrink|-basis)?\s*:[^;]+;?", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bgrid-(?:template(?:-\w+)?|auto-(?:flow|columns|rows)|column|row|area|gap)\s*:[^;]+;?", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\b(?:column-gap|row-gap|gap)\s*:[^;]+;?", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\balign-(?:items|content|self)\s*:[^;]+;?", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bjustify-(?:content|items|self)\s*:[^;]+;?", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bplace-(?:items|content|self)\s*:[^;]+;?", "", RegexOptions.IgnoreCase);

            // Ensure the container has a width so table-layout:fixed has effect
            result = result.TrimEnd().TrimEnd(';');
            if (!Regex.IsMatch(result, @"\bwidth\s*:", RegexOptions.IgnoreCase))
                result += ";width:100%";
            if (!Regex.IsMatch(result, @"\btable-layout\s*:", RegexOptions.IgnoreCase))
                result += ";table-layout:fixed";

            return result.TrimStart(';').Trim();
        }

        private static string StripFlexItemProperties(string style)
        {
            // Remove flex-item properties (flex-grow/shrink/basis, order, align-self)
            style = Regex.Replace(style, @"\bflex(?:-grow|-shrink|-basis)?\s*:[^;]+;?", "", RegexOptions.IgnoreCase);
            style = Regex.Replace(style, @"\border\s*:\s*\d[^;]*;?", "", RegexOptions.IgnoreCase);
            return style;
        }

        private static string AddOlwClass(string existing)
        {
            if (existing.IndexOf(FlexContainerClass, StringComparison.OrdinalIgnoreCase) >= 0)
                return existing;
            return string.IsNullOrWhiteSpace(existing) ? FlexContainerClass : existing + " " + FlexContainerClass;
        }

        private static string RemoveOlwClass(string existing)
        {
            if (existing.IndexOf(FlexContainerClass, StringComparison.OrdinalIgnoreCase) < 0)
                return existing;
            string result = Regex.Replace(existing, @"\bolw-fc\b\s*", "", RegexOptions.IgnoreCase).Trim();
            return result;
        }
    }
}
