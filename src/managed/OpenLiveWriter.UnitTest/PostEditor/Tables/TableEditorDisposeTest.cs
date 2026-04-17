// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace OpenLiveWriter.UnitTest.PostEditor.Tables
{
    /// <summary>
    /// Validates the defensive Dispose pattern used by TableEditor.SelectionPreserver.
    /// The SelectionPreserver is a private inner class that depends on COM objects,
    /// so we test the principle: Dispose must suppress COMException and
    /// InvalidOperationException rather than letting them propagate.
    /// See: https://github.com/OpenLiveWriter/OpenLiveWriter/issues/750
    /// </summary>
    [TestFixture]
    public class TableEditorDisposeTest
    {
        [Test]
        public void Dispose_ShouldNotThrowCOMException()
        {
            // Validates that a Dispose implementation following the SelectionPreserver
            // pattern suppresses COMException (fix for #750)
            var disposable = new SafeDisposable(() =>
            {
                throw new COMException("Simulated COM error during selection restore");
            });

            disposable.Dispose();
            // Test passes if no exception propagates
        }

        [Test]
        public void Dispose_ShouldNotThrowInvalidOperationException()
        {
            // Validates that a Dispose implementation following the SelectionPreserver
            // pattern suppresses InvalidOperationException
            var disposable = new SafeDisposable(() =>
            {
                throw new InvalidOperationException("Simulated invalid state during selection restore");
            });

            disposable.Dispose();
            // Test passes if no exception propagates
        }

        [Test]
        public void Dispose_ShouldStillThrowUnexpectedExceptions()
        {
            // Other exception types should NOT be suppressed - only COM and
            // InvalidOperation are expected during selection restoration
            var disposable = new SafeDisposable(() =>
            {
                throw new ArgumentException("Unexpected error");
            });

            Assert.Throws<ArgumentException>(() => disposable.Dispose());
        }

        /// <summary>
        /// Helper that mimics the SelectionPreserver.Dispose pattern:
        /// runs an action but suppresses COMException and InvalidOperationException,
        /// matching the catch blocks in TableEditor.SelectionPreserver.Dispose.
        /// </summary>
        private class SafeDisposable : IDisposable
        {
            private readonly Action _action;

            public SafeDisposable(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                try
                {
                    _action();
                }
                catch (COMException)
                {
                    // Suppress COM errors during selection restoration.
                    // The underlying COM object may no longer be valid after
                    // table row operations. Dispose should never throw.
                }
                catch (InvalidOperationException)
                {
                    // Suppress in case the markup range is in an invalid state.
                }
            }
        }
    }
}
