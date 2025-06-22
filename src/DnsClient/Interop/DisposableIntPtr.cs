// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DnsClient
{
    internal sealed class DisposableIntPtr : IDisposable
    {
        private nint _ptr;

        public nint Ptr { get; }
        public bool IsValid { get; private set; } = true;

        private DisposableIntPtr()
        {
        }

        public static unsafe DisposableIntPtr Alloc(nuint size)
        {
            var ptr = new DisposableIntPtr();
            try
            {
                ptr._ptr = (nint)NativeMemory.Alloc(size);
            }
            catch (OutOfMemoryException)
            {
                ptr.IsValid = false;
            }
            return ptr;
        }

        public unsafe void Dispose()
        {
            var ptr = Interlocked.Exchange(ref this._ptr, 0);
            NativeMemory.Free((void*)ptr);
        }
    }
}
