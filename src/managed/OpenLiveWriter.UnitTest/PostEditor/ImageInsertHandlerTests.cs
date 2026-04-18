// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenLiveWriter.Extensibility.BlogClient;
using OpenLiveWriter.PostEditor.PostHtmlEditing;

namespace OpenLiveWriter.UnitTest.PostEditor
{
    [TestClass]
    public class ImageInsertHandlerTests
    {
        [TestMethod]
        public void LoadBitmapWithOomProtection_ValidImage_ReturnsBitmap()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                // Create a small valid image
                using (Bitmap bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb))
                {
                    bmp.Save(tempFile, ImageFormat.Png);
                }

                using (Bitmap result = ImageInsertHandler.LoadBitmapWithOomProtection(tempFile))
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
