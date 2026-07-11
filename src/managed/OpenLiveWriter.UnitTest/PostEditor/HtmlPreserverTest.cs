// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using OpenLiveWriter.PostEditor.PostHtmlEditing;

namespace OpenLiveWriter.UnitTest.PostEditor
{
    [TestFixture]
    public class HtmlPreserverTest
    {
        private const string OBJECT_WITH_EMBED = "<object width=\"425\" height=\"355\"><param name=\"movie\" value=\"http://www.youtube.com/v/AIObOihJDB8&rel=1\"></param><param name=\"wmode\" value=\"transparent\"></param><embed src=\"http://www.youtube.com/v/AIObOihJDB8&rel=1\" type=\"application/x-shockwave-flash\" wmode=\"transparent\" width=\"425\" height=\"355\"></embed></object>";

        private const string OBJECT_WITH_EMBED_IN_SMART_CONTENT =
            "<div class=\"wlWriterSmartContent\">" + OBJECT_WITH_EMBED + "</div>";

        private const string SCRIPT = "<script language=\"JavaScript\">document.write('foo');</script>";

        private const string YOUTUBE_IFRAME = "<iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/XNbc2HhL7J4\" title=\"YouTube video player\" frameborder=\"0\" allow=\"accelerometer; autoplay; encrypted-media\" referrerpolicy=\"strict-origin-when-cross-origin\" allowfullscreen></iframe>";

        // What MSHTML gives back after a round trip: same attributes, reordered, minimized
        // attributes expanded. Semantically equivalent but not what the user typed.
        private const string YOUTUBE_IFRAME_MANGLED = "<iframe title=\"YouTube video player\" height=\"315\" allowfullscreen=\"allowfullscreen\" src=\"https://www.youtube.com/embed/XNbc2HhL7J4\" frameborder=\"0\" width=\"560\" referrerpolicy=\"strict-origin-when-cross-origin\" allow=\"accelerometer; autoplay; encrypted-media\"></iframe>";

        [Test]
        public void Test1()
        {
            HtmlPreserver hp = new HtmlPreserver();
            string scanned = hp.ScanAndPreserve(OBJECT_WITH_EMBED);
            Assert.AreNotEqual(OBJECT_WITH_EMBED, scanned);
            Assert.AreEqual(
                OBJECT_WITH_EMBED,
                hp.RestorePreserved(scanned.Replace(OBJECT_WITH_EMBED, "<object></object>")));
        }

        [Test]
        public void IgnoreSmartContent()
        {
            HtmlPreserver hp = new HtmlPreserver();
            Assert.AreEqual(
                OBJECT_WITH_EMBED_IN_SMART_CONTENT,
                hp.ScanAndPreserve(OBJECT_WITH_EMBED_IN_SMART_CONTENT));

            hp.Reset();
            Assert.AreEqual(
                OBJECT_WITH_EMBED_IN_SMART_CONTENT,
                hp.RestorePreserved(hp.ScanAndPreserve(OBJECT_WITH_EMBED_IN_SMART_CONTENT)));
        }

        [Test]
        public void ScriptTest()
        {
            HtmlPreserver hp = new HtmlPreserver();
            string working = hp.ScanAndPreserve(SCRIPT);
            working = working.Replace(SCRIPT, SCRIPT + "foo");
            Assert.AreEqual(
                SCRIPT,
                hp.RestorePreserved(working));
        }

        [Test]
        public void IframeSurvivesEditorRoundTrip()
        {
            HtmlPreserver hp = new HtmlPreserver();
            string scanned = hp.ScanAndPreserve(YOUTUBE_IFRAME);
            Assert.AreNotEqual(YOUTUBE_IFRAME, scanned);
            Assert.AreEqual(
                YOUTUBE_IFRAME,
                hp.RestorePreserved(scanned.Replace(YOUTUBE_IFRAME, YOUTUBE_IFRAME_MANGLED)));
        }
    }
}
