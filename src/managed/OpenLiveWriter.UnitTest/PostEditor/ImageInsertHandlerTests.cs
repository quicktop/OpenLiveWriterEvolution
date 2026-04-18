// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenLiveWriter.Extensibility.BlogClient;
using OpenLiveWriter.PostEditor.PostHtmlEditing;

namespace OpenLiveWriter.UnitTest.PostEditor
{
    [TestClass]
    public class ImageInsertHandlerTests
    {
        // Minimal valid 1x1 white PNG (67 bytes)
        private static readonly byte[] MinimalPng = {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, 0xDE, 0x00, 0x00, 0x00,
            0x0C, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
            0x00, 0x00, 0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC, 0x33, 0x00, 0x00, 0x00,
            0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        };

        [TestMethod]
        public void LoadBitmapWithOomProtection_ValidImage_ReturnsBitmap()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".png");
            try
            {
                File.WriteAllBytes(tempFile, MinimalPng);

                using (var result = ImageInsertHandler.LoadBitmapWithOomProtection(tempFile))
                {
                    Assert.IsNotNull(result);
                    Assert.AreEqual(1, result.Width);
                    Assert.AreEqual(1, result.Height);
                }
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void LoadBitmapWithOomProtection_InvalidImage_ThrowsBlogClientFileTransferException()
        {
            // GDI+ throws OutOfMemoryException for corrupt/unsupported image files
            string tempFile = Path.GetTempFileName();
            try
            {
                // Write garbage data that is not a valid image format
                File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 });

                try
                {
                    ImageInsertHandler.LoadBitmapWithOomProtection(tempFile);
                    Assert.Fail("Expected BlogClientFileTransferException was not thrown.");
                }
                catch (BlogClientFileTransferException)
                {
                    // Expected: OOM from GDI+ is caught and wrapped
                }
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void LoadBitmapWithOomProtection_NonExistentFile_ThrowsArgumentException()
        {
            // Bitmap constructor throws ArgumentException for files that don't exist
            string fakePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".png");

            try
            {
                ImageInsertHandler.LoadBitmapWithOomProtection(fakePath);
                Assert.Fail("Expected an exception for non-existent file.");
            }
            catch (ArgumentException)
            {
                // Expected: non-OOM exceptions pass through unmodified
            }
        }
    }
}
