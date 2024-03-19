//------------------------------------------------------------
//        File:  BufferPool.cs
//       Brief:  FileSystem enhanced buffer pool.
//
//      Author:  VyronLee, lwz_jz@hotmail.com
//
//    Modified:  2021-07-08 16:15
//   Copyright:  Copyright (c) 2021, VyronLee
//============================================================

using System;
using System.Collections.Concurrent;
using System.Threading;
using vFrame.Core.Loggers;

namespace vFrame.VFS
{
    internal class BufferPool
    {
#if VFRAME_VFS_LARGE_BUFFER
        private const int BucketCount = 4;

        // 16M total
        private static readonly int[,] BucketSizes = new int[BucketCount, 2] {
            {128 * 1024, 8}, // 128K per buffer, 1M total
            {512 * 1024, 6}, // 512K per buffer, 3M total
            {1 * 1024 * 1024, 4}, // 1M per buffer, 4M total
            {4 * 1024 * 1024, 2}, // 4M per buffer, 8M total
        };
#else
        private const int BucketCount = 2;

        // 4M total
        private static readonly int[,] BucketSizes = new int[BucketCount, 2] {
            { 128 * 1024, 8 }, // 128K per buffer, 1M total
            { 512 * 1024, 6 } // 512K per buffer, 3M total
        };
#endif


        private readonly ConcurrentDictionary<int, Bucket<byte>> _buckets =
            new ConcurrentDictionary<int, Bucket<byte>>();

        private readonly bool _logMissing;

        private BufferPool(bool logMissing = false) {
            for (var index = 0; index < BucketCount; index++) {
                if (!_buckets.TryAdd(index, new Bucket<byte>(BucketSizes[index, 0], BucketSizes[index, 1]))) {
                    throw new FileSystemBufferPoolInitFailedException("Bucket add failed, index: " + index);
                }
            }
            _logMissing = logMissing;
        }

        public byte[] Rent(int minimumLength) {
            var idx = SelectBucketToRent(minimumLength);
            if (idx < 0) {
                return new byte[minimumLength];
            }

            byte[] buffer;
            if (_buckets.TryGetValue(idx, out var bucket)) {
                buffer = bucket.Rent();
                if (null != buffer) {
                    return buffer;
                }
            }

            buffer = new byte[minimumLength];
            if (_logMissing) {
                Logger.Warning(FileSystemConst.LogTag,
                    "Rent from buffer pool failed, size: {0}, force create new buffer..", minimumLength);
            }
            return buffer;
        }

        public void Return(byte[] array, bool clearArray = false) {
            if (null == array) {
                throw new ArgumentNullException(nameof(array));
            }

            var idx = SelectBucketToReturn(array.Length);
            if (idx < 0) {
                return;
            }

            if (clearArray) {
                Array.Clear(array, 0, array.Length);
            }

            if (_buckets.TryGetValue(idx, out var bucket)) {
                bucket.Return(array);
            }
        }

        private static int SelectBucketToRent(int minimumLength) {
            for (var i = 0; i < BucketCount; i++) {
                if (minimumLength <= BucketSizes[i, 0]) {
                    return i;
                }
            }
            return -1;
        }

        private static int SelectBucketToReturn(int length) {
            for (var i = 0; i < BucketCount; i++) {
                if (length == BucketSizes[i, 0]) {
                    return i;
                }
            }
            return -1;
        }

        private static BufferPool _sharedInstance;

        public static BufferPool Shared => Volatile.Read(ref _sharedInstance) ?? EnsureSharedCreated();

        private static BufferPool EnsureSharedCreated() {
            Interlocked.CompareExchange(ref _sharedInstance, new BufferPool(), null);
            return _sharedInstance;
        }
    }
}