// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenLiveWriter.PostEditor.PostHtmlEditing;

namespace OpenLiveWriter.UnitTest.PostEditor
{
    [TestClass]
    public class StylePreserverTest
    {
        [TestMethod]
        public void RestoreStyleAttributes_RestoresDisplayGrid()
        {
            string html = "<div style=\"display:grid;grid-template-columns:1fr 1fr;gap:12px\"><p>One</p><p>Two</p></div>";

            string preserved = StylePreserver.PreserveStyleAttributes(html);
            string restored = StylePreserver.RestoreStyleAttributes(preserved);

            Assert.IsTrue(preserved.Contains("wlstyle="));
            Assert.IsTrue(preserved.Contains("display:table"));
            Assert.IsTrue(restored.Contains("display:grid"));
            Assert.IsTrue(restored.Contains("grid-template-columns:1fr 1fr"));
            Assert.IsFalse(restored.Contains("wlstyle"));
            Assert.IsFalse(restored.Contains("olw-fc"));
        }

        [TestMethod]
        public void RestoreStyleAttributes_StripsChildSentinel()
        {
            string html = "<div style=\"display:grid\"><span style=\"color:red\">One</span></div>";

            string preserved = StylePreserver.PreserveStyleAttributes(html);
            string restored = StylePreserver.RestoreStyleAttributes(preserved);

            Assert.IsTrue(preserved.Contains("display:table-cell"));
            Assert.IsTrue(restored.Contains("style=\"color:red\""));
            Assert.IsFalse(restored.Contains("display:table-cell"));
        }
    }
}
