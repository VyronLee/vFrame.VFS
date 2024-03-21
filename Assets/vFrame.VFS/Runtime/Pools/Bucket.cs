using System;
using System.Threading;

namespace vFrame.VFS
{
    internal sealed class Bucket<T>
    {
        private readonly int _bufferLength;
        private readonly T[][] _buffers;
        private int _index;

        private SpinLock _lock; // do not make this readonly; it's a mutable struct

        /// <summary>
        ///     Creates the pool with numberOfBuffers arrays where each buffer is of bufferLength length.
        /// </summary>
        public Bucket(int bufferLength, int numberOfBuffers) {
            _lock = new SpinLock();
            _buffers = new T[numberOfBuffers][];
            _bufferLength = bufferLength;
        }

        /// <summary>Gets an ID for the bucket to use with events.</summary>
        public int Id => GetHashCode();

        /// <summary>Takes an array from the bucket.  If the bucket is empty, returns null.</summary>
        public T[] Rent() {
            var buffers = _buffers;
            T[] buffer = null;

            // While holding the lock, grab whatever is at the next available index and
            // update the index.  We do as little work as possible while holding the spin
            // lock to minimize contention with other threads.  The try/finally is
            // necessary to properly handle thread aborts on platforms which have them.
            bool lockTaken = false, allocateBuffer = false;
            try {
                _lock.Enter(ref lockTaken);

                if (_index < buffers.Length) {
                    buffer = buffers[_index];
                    buffers[_index++] = null;
                    allocateBuffer = buffer == null;
                }
            }
            finally {
                if (lockTaken) {
                    _lock.Exit(false);
                }
            }

            // While we were holding the lock, we grabbed whatever was at the next available index, if
            // there was one.  If we tried and if we got back null, that means we hadn't yet allocated
            // for that slot, in which case we should do so now.
            if (allocateBuffer) {
                buffer = new T[_bufferLength];
            }

            return buffer;
        }

        /// <summary>
        ///     Attempts to return the buffer to the bucket.  If successful, the buffer will be stored
        ///     in the bucket and true will be returned; otherwise, the buffer won't be stored, and false
        ///     will be returned.
        /// </summary>
        public void Return(T[] array) {
            // Check to see if the buffer is the correct size for this bucket
            if (array.Length != _bufferLength) {
                throw new ArgumentException("Buffer not from pool", nameof(array));
            }

            // While holding the spin lock, if there's room available in the bucket,
            // put the buffer into the next available slot.  Otherwise, we just drop it.
            // The try/finally is necessary to properly handle thread aborts on platforms
            // which have them.
            var lockTaken = false;
            try {
                _lock.Enter(ref lockTaken);

                // We not cache the array if bucket is full
                if (_index < _buffers.Length && _index > 0) {
                    _buffers[--_index] = array;
                }
            }
            finally {
                if (lockTaken) {
                    _lock.Exit(false);
                }
            }
        }
    }
}