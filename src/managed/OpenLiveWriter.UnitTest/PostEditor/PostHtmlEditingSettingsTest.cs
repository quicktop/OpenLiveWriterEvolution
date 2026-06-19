// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenLiveWriter.PostEditor.PostHtmlEditing;

namespace OpenLiveWriter.UnitTest.PostEditor
{
    [TestClass]
    public class PostHtmlEditingSettingsTest
    {
        [TestMethod]
        public void EnsureMshtmlCompatibilityMeta_UpdatesExistingMeta()
        {
            string html = "<html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=EmulateIE9\" /></head><body></body></html>";

            string result = PostHtmlEditingSettings.EnsureMshtmlCompatibilityMeta(html);

            Assert.IsTrue(result.Contains("content=\"IE=11\""));
            Assert.IsFalse(result.Contains("IE=EmulateIE9"));
        }

        [TestMethod]
        public void EnsureMshtmlCompatibilityMeta_HandlesReorderedAttributes()
        {
            string html = "<html><head><meta content='IE=9' http-equiv='X-UA-Compatible'></head><body></body></html>";

            string result = PostHtmlEditingSettings.EnsureMshtmlCompatibilityMeta(html);

            Assert.IsTrue(result.Contains("content=\"IE=11\""));
            Assert.IsFalse(result.Contains("IE=9"));
        }

        [TestMethod]
        public void EnsureMshtmlCompatibilityMeta_InsertsMetaWhenMissing()
        {
            string html = "<html><head><title>Post</title></head><body></body></html>";

            string result = PostHtmlEditingSettings.EnsureMshtmlCompatibilityMeta(html);

            Assert.IsTrue(result.Contains("<meta http-equiv=\"X-UA-Compatible\" content=\"IE=11\" />"));
        }
    }
}
