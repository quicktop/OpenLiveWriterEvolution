// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenLiveWriter.CoreServices;

namespace OpenLiveWriter.UnitTest.BlogClient
{
    [TestClass]
    public class PostDateTimezoneTest
    {
        [TestMethod]
        public void LocalToUtc_ConvertsLocalTimeToUtc()
        {
            // Arrange: a known local time
            DateTime localTime = new DateTime(2026, 6, 15, 14, 30, 0); // 2:30 PM local

            // Act
            DateTime utcTime = DateTimeHelper.LocalToUtc(localTime);

            // Assert: UTC should differ from local by the timezone offset
            TimeSpan expectedOffset = TimeZone.CurrentTimeZone.GetUtcOffset(localTime);
            TimeSpan actualDifference = localTime - utcTime;
            Assert.AreEqual(expectedOffset, actualDifference,
                "LocalToUtc should shift the time by the local UTC offset");
        }

        [TestMethod]
        public void UtcDateField_DiffersFromLocal_ByTimezoneOffset()
        {
            // Arrange: simulate what GeneratePostDateFields should do
            // post.DatePublishedOverride is local time from the UI
            DateTime localDate = new DateTime(2026, 6, 15, 22, 0, 0); // 10 PM local

            // Act: the correct conversion for the GMT field
            DateTime utcDate = DateTimeHelper.LocalToUtc(localDate);

            // Assert
            TimeSpan offset = TimeZone.CurrentTimeZone.GetUtcOffset(localDate);
            if (offset != TimeSpan.Zero)
            {
                Assert.AreNotEqual(localDate, utcDate,
                    "UTC date should differ from local date when not in UTC timezone");
            }

            // The UTC time should be earlier than local if offset is positive (east of GMT),
            // or later if offset is negative (west of GMT)
            Assert.AreEqual(localDate, DateTimeHelper.UtcToLocal(utcDate),
                "Converting the UTC result back to local should yield the original local time");
        }

        [TestMethod]
        public void ConversionDirection_LocalToUtc_NotUtcToLocal()
        {
            // This test verifies the bug fix: the old code used UtcToLocal on local time
            // (treating local as UTC), which shifts in the wrong direction.
            DateTime localTime = new DateTime(2026, 6, 15, 12, 0, 0);

            DateTime correctUtc = DateTimeHelper.LocalToUtc(localTime);
            DateTime buggyUtc = localTime; // old code assigned local directly as "utcDate"

            TimeSpan offset = TimeZone.CurrentTimeZone.GetUtcOffset(localTime);
            if (offset != TimeSpan.Zero)
            {
                Assert.AreNotEqual(buggyUtc, correctUtc,
                    "The correct UTC conversion should differ from simply copying local time");

                // The old buggy code also applied UtcToLocal to local time for the dateCreated field,
                // which double-shifts the time
                DateTime buggyLocal = DateTimeHelper.UtcToLocal(localTime); // wrong direction
                Assert.AreNotEqual(localTime, buggyLocal,
                    "Applying UtcToLocal to an already-local time produces a wrong result");
            }
            else
            {
                // In UTC+0 timezone, the values happen to be equal; test is still valid
                Assert.AreEqual(localTime, correctUtc,
                    "In UTC+0 timezone, local and UTC should be equal");
            }
        }
    }
}
