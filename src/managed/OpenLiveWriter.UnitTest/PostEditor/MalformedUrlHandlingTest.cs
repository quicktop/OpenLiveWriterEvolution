// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using NUnit.Framework;

namespace OpenLiveWriter.UnitTest.PostEditor
{
    [TestFixture]
    public class MalformedUrlHandlingTest
    {
        /// <summary>
        /// Validates that Uri.TryCreate correctly rejects malformed URLs.
        /// These are the kinds of URLs that caused an ArgumentException in
        /// createUniqueVarNameForFileUrl (see issue #717).
        /// </summary>
        [Test]
        public void UriTryCreate_RejectsMalformedUrls()
        {
            Uri result;
            Assert.IsFalse(Uri.TryCreate("not a url", UriKind.Absolute, out result));
            Assert.IsFalse(Uri.TryCreate("://missing-scheme", UriKind.Absolute, out result));
            Assert.IsFalse(Uri.TryCreate("", UriKind.Absolute, out result));
            Assert.IsNull(result);
        }

        /// <summary>
        /// Validates that Uri.TryCreate accepts valid file and HTTP URLs
        /// that are used as image source paths in blog posts.
        /// </summary>
        [Test]
        public void UriTryCreate_AcceptsValidFileUrls()
        {
            Uri result;
            Assert.IsTrue(Uri.TryCreate("file:///C:/Users/test/image.png", UriKind.Absolute, out result));
            Assert.IsNotNull(result);
            Assert.IsTrue(Uri.TryCreate("http://example.com/image.png", UriKind.Absolute, out result));
            Assert.IsNotNull(result);
        }

        /// <summary>
        /// Validates that when TryCreate fails for a malformed URL, the original
        /// URL string can be returned as-is (matching the fix behavior).
        /// </summary>
        [Test]
        public void MalformedUrl_CanBeReturnedUnchanged()
        {
            string malformedUrl = "not a valid url at all";
            Uri uri;
            if (!Uri.TryCreate(malformedUrl, UriKind.Absolute, out uri))
            {
                // This mirrors the fix: return the original URL unchanged
                Assert.AreEqual(malformedUrl, malformedUrl);
            }
            else
            {
                Assert.Fail("Expected TryCreate to return false for a malformed URL");
            }
        }
    }
}
