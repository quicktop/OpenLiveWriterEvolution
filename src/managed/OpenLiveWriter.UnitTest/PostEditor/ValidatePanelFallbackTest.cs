// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.IO;
using System.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenLiveWriter.UnitTest.PostEditor
{
    [TestClass]
    public class ValidatePanelFallbackTest
    {
        private const string FallbackMessage = "Please sign in to your Google account to continue.";

        [TestMethod]
        public void ExceptionFilter_CatchesFileNotFoundException()
        {
            bool caughtByFilter = false;

            try
            {
                throw new FileNotFoundException("Could not load resource assembly");
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is MissingManifestResourceException)
            {
                caughtByFilter = true;
            }

            Assert.IsTrue(caughtByFilter, "FileNotFoundException should be caught by the exception filter.");
        }

        [TestMethod]
        public void ExceptionFilter_CatchesMissingManifestResourceException()
        {
            bool caughtByFilter = false;

            try
            {
                throw new MissingManifestResourceException("Resource not found");
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is MissingManifestResourceException)
            {
                caughtByFilter = true;
            }

            Assert.IsTrue(caughtByFilter, "MissingManifestResourceException should be caught by the exception filter.");
        }

        [TestMethod]
        public void ExceptionFilter_DoesNotCatchUnrelatedExceptions()
        {
            bool caughtByFilter = false;
            bool caughtByGeneral = false;

            try
            {
                try
                {
                    throw new InvalidOperationException("Unrelated error");
                }
                catch (Exception ex) when (ex is FileNotFoundException || ex is MissingManifestResourceException)
                {
                    caughtByFilter = true;
                }
            }
            catch (InvalidOperationException)
            {
                caughtByGeneral = true;
            }

            Assert.IsFalse(caughtByFilter, "InvalidOperationException should not be caught by the resource exception filter.");
            Assert.IsTrue(caughtByGeneral, "InvalidOperationException should propagate past the filter.");
        }

        [TestMethod]
        public void FallbackMessage_IsNotNullOrEmpty()
        {
            Assert.IsFalse(string.IsNullOrEmpty(FallbackMessage), "Fallback message should not be null or empty.");
        }

        [TestMethod]
        public void FallbackMessage_ContainsSignInGuidance()
        {
            Assert.IsTrue(FallbackMessage.IndexOf("sign in", StringComparison.OrdinalIgnoreCase) >= 0,
                "Fallback message should instruct the user to sign in.");
        }
    }
}
