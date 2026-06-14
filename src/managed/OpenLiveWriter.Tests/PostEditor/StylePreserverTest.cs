// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using NUnit.Framework;
using OpenLiveWriter.PostEditor.PostHtmlEditing;

namespace OpenLiveWriter.Tests.PostEditor
{
    [TestFixture]
    public class StylePreserverTest
    {
        [Test]
        public void TestPreserveAndRestoreGrid()
        {
            string html = "<div style=\"display:grid; grid-template-columns:1fr 1fr; gap:12px; margin:12px 0 16px;\">" +
                          "<div>Column 1</div><div>Column 2</div></div>";

            // Preserve style attributes
            string preserved = StylePreserver.PreserveStyleAttributes(html);
            
            // It should add wlstyle with the original style value
            Assert.IsTrue(preserved.Contains("wlstyle=\"display:grid; grid-template-columns:1fr 1fr; gap:12px; margin:12px 0 16px;\""));

            // Simulate what MSHTML does (strips display:grid, grid-template-columns, gap, keeping margin)
            string mshtmlOutput = preserved.Replace(
                " style=\"display:grid; grid-template-columns:1fr 1fr; gap:12px; margin:12px 0 16px;\"",
                " style=\"margin:12px 0 16px;\"");

            // Restore style attributes
            string restored = StylePreserver.RestoreStyleAttributes(mshtmlOutput);

            // It should restore the original style value and remove wlstyle
            Assert.AreEqual(html, restored);
        }

        [Test]
        public void TestPreserveAndRestoreFlex()
        {
            string html = "<div style=\"display: flex; justify-content: space-between; align-items: center;\">Content</div>";

            string preserved = StylePreserver.PreserveStyleAttributes(html);
            Assert.IsTrue(preserved.Contains("wlstyle=\"display: flex; justify-content: space-between; align-items: center;\""));

            // Simulate MSHTML stripping flex styles
            string mshtmlOutput = preserved.Replace(
                " style=\"display: flex; justify-content: space-between; align-items: center;\"",
                " style=\"\"");

            string restored = StylePreserver.RestoreStyleAttributes(mshtmlOutput);
            Assert.AreEqual(html, restored);
        }

        [Test]
        public void TestIgnoreNormalStyle()
        {
            string html = "<div style=\"color: red; margin: 10px;\">Text</div>";
            string preserved = StylePreserver.PreserveStyleAttributes(html);

            // Normal styles should not be preserved/modified
            Assert.IsFalse(preserved.Contains("wlstyle"));
            Assert.AreEqual(html, preserved);
        }
    }
}
