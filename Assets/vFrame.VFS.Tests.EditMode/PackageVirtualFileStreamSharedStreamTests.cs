// ------------------------------------------------------------
//         File: PackageVirtualFileStreamSharedStreamTests.cs
//        Brief: Regression tests for shared-stream monitor safety (R13).
//
//       Author: VyronLee, lwz_jz@hotmail.com
//
//      Created: 2026-8-12
//    Copyright: Copyright (c) 2026, VyronLee
// ============================================================

using System;
using System.IO;
using System.Text;
using System.Threading;
using NUnit.Framework;
using vFrame.VFS;

namespace vFrame.VFS.Tests.EditMode
{
    /// <summary>
    ///     Guards the shared-stream read path of stream-mounted packages (R13).
    ///
    ///     <para>
    ///         When a package is mounted over one caller-supplied stream
    ///         (<c>Open(stream, leaveOpen: true)</c>), every block read seeks and copies
    ///         through that single shared stream under a monitor. The monitor used to be
    ///         released by a manual <c>Unlock()</c> after the copy; if the underlying
    ///         stream threw mid-copy, the monitor stayed held forever and every later
    ///         async read of the package blocked indefinitely.
    ///     </para>
    /// </summary>
    [TestFixture]
    public class PackageVirtualFileStreamSharedStreamTests
    {
        private const int BoundedWaitMs = 10_000;

        /// <summary>
        ///     Packs the named files (content = file name) into a real .vpk via the
        ///     production writer, then returns the package bytes in memory. Building
        ///     with <c>CreatePackage</c>/<c>AddStream</c>/<c>Flush</c> keeps the
        ///     on-disk layout correct by construction.
        /// </summary>
        private static MemoryStream CreateValidPackageStream(params string[] files)
        {
            var tempPath = Path.Combine(Path.GetTempPath(),
                "vframe_vfs_r13_" + Guid.NewGuid().ToString("N") + ".vpk");
            try {
                var package = PackageVirtualFileSystem.CreatePackage(tempPath);
                try {
                    foreach (var file in files) {
                        using var data = new MemoryStream(Encoding.UTF8.GetBytes(file));
                        package.AddStream(file, data, encryptType: 0, encryptKey: 0, compressType: 0);
                    }
                    package.Flush();
                }
                finally {
                    package.Close();
                }

                return new MemoryStream(File.ReadAllBytes(tempPath));
            }
            finally {
                File.Delete(tempPath);
            }
        }

        /// <summary>
        ///     A seekable stream that throws while reading a chosen byte range, then
        ///     behaves normally once the read position leaves that range. Scope the
        ///     range to a single data block so header, block-table and file-list
        ///     reads during <c>Open()</c> stay intact and the fault lands exactly in
        ///     <c>PackageVirtualFileStream.InternalOpen</c>'s locked copy.
        /// </summary>
        private sealed class FaultInRangeStream : Stream
        {
            private readonly MemoryStream _origin;
            private long _faultFrom;
            private long _faultTo;
            private bool _faulted;

            public FaultInRangeStream(MemoryStream origin, long faultFrom, long faultTo)
            {
                _origin = origin;
                _faultFrom = faultFrom;
                _faultTo = faultTo;
            }

            /// <summary>Whether the fault has fired at least once.</summary>
            public bool HasFaulted => _faulted;

            /// <summary>
            ///     (Re-)targets the faulting range. Pass an empty range to disable
            ///     faulting (e.g. while the file system parses header/table/list).
            /// </summary>
            public void SetFaultRange(long faultFrom, long faultTo)
            {
                _faultFrom = faultFrom;
                _faultTo = faultTo;
            }

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => _origin.Length;

            public override long Position {
                get => _origin.Position;
                set => _origin.Position = value;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (!_faulted && _origin.Position >= _faultFrom && _origin.Position < _faultTo) {
                    _faulted = true;
                    throw new IOException("Simulated underlying-stream failure (R13).");
                }
                return _origin.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _origin.Seek(offset, origin);
            }

            public override void Flush() { }
            public override void SetLength(long value) => _origin.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => _origin.Write(buffer, offset, count);
        }

        [Test]
        public void GetStreamAsync_FaultedSharedStream_SecondReadStillCompletes()
        {
            using var packageStream = CreateValidPackageStream("a.txt", "b.txt");
            using var faulting = new FaultInRangeStream(packageStream, faultFrom: 0, faultTo: 0);

            var fs = new PackageVirtualFileSystem();
            var opened = false;
            try {
                // Open parses header, block table and LZMA file list through the
                // same shared stream — keep faulting disabled for that phase.
                fs.Open(faulting, leaveOpen: true);
                opened = true;

                // Arm the fault over a.txt's stored block so the first data read
                // throws inside InternalOpen's locked copy.
                var block = fs.GetBlockInfo("a.txt");
                faulting.SetFaultRange(block.Offset, block.Offset + block.CompressedSize);

                var failed = fs.GetStreamAsync("a.txt");
                try {
                    Assert.That(WaitUntilFaulted(faulting, BoundedWaitMs), Is.True,
                        "first read must fault inside the locked copy");
                }
                finally {
                    failed.Dispose();
                }

                // NOTE: the faulted request itself never reports IsDone (its catch
                // logs without finishing) — a pre-existing gap outside R13's scope.
                // The regression is the SECOND read: with the monitor leaked, it
                // hangs forever; with Unlock in finally, it completes bounded.
                var second = fs.GetStreamAsync("b.txt");
                var finished = WaitUntilDone(second, BoundedWaitMs);
                second.Dispose();
                Assert.That(finished, Is.True,
                    "a fault under the shared-stream monitor must not block later async reads");
            }
            finally {
                if (opened) {
                    fs.Close();
                }
            }
        }

        private static bool WaitUntilFaulted(FaultInRangeStream stream, int timeoutMs)
        {
            var deadline = Environment.TickCount + timeoutMs;
            while (Environment.TickCount < deadline) {
                if (stream.HasFaulted) {
                    return true;
                }
                Thread.Sleep(10);
            }
            return stream.HasFaulted;
        }

        [Test]
        public void GetStreamAsync_TwoConcurrentReads_BothCompleteAndDataIntact()
        {
            using var packageStream = CreateValidPackageStream("a.txt", "b.txt");
            var fs = new PackageVirtualFileSystem();
            var opened = false;
            try {
                fs.Open(packageStream, leaveOpen: true);
                opened = true;

                var first = fs.GetStreamAsync("a.txt");
                var second = fs.GetStreamAsync("b.txt");
                try {
                    Assert.That(WaitUntilDone(first, BoundedWaitMs), Is.True, "first read must finish");
                    Assert.That(WaitUntilDone(second, BoundedWaitMs), Is.True, "second read must finish");

                    var a = first.Stream;
                    var b = second.Stream;
                    Assert.That(a, Is.Not.Null, "first read produced a stream");
                    Assert.That(b, Is.Not.Null, "second read produced a stream");
                    Assert.That(ReadAllText(a), Is.EqualTo("a.txt"),
                        "first read returned its own block's data");
                    Assert.That(ReadAllText(b), Is.EqualTo("b.txt"),
                        "second read returned its own block's data");
                }
                finally {
                    first.Dispose();
                    second.Dispose();
                }
            }
            finally {
                if (opened) {
                    fs.Close();
                }
            }
        }

        private static bool WaitUntilDone(IVirtualFileStreamRequest request, int timeoutMs)
        {
            var deadline = Environment.TickCount + timeoutMs;
            while (Environment.TickCount < deadline) {
                if (request.IsDone) {
                    return true;
                }
                Thread.Sleep(10);
            }
            return request.IsDone;
        }

        private static string ReadAllText(IVirtualFileStream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var bytes = new byte[stream.Length];
            var read = 0;
            while (read < bytes.Length) {
                var n = stream.Read(bytes, read, (int)(bytes.Length - read));
                if (n <= 0) {
                    break;
                }
                read += n;
            }
            return Encoding.UTF8.GetString(bytes, 0, read);
        }
    }
}