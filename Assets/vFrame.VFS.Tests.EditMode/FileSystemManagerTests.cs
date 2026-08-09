// ------------------------------------------------------------
//         File: FileSystemManagerTests.cs
//        Brief: Regression tests for FileSystemManager mount lifecycle (R1)
//
//       Author: VyronLee, lwz_jz@hotmail.com
//
//      Created: 2026-08-09 00:00:00
//    Copyright: Copyright (c) 2026, VyronLee
// ============================================================

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using vFrame.VFS;

namespace vFrame.VFS.Tests.EditMode
{
    [TestFixture]
    public class FileSystemManagerTests
    {
        /// <summary>
        /// Minimal fake implementing the real <see cref="IVirtualFileSystem"/> surface.
        /// Identity is by reference; reachability is asserted via enumeration.
        /// </summary>
        private class FakeFS : IVirtualFileSystem
        {
            public void Open(VFSPath fsPath) { }
            public void Close() { }
            public bool Exist(VFSPath filePath) => false;
            public IVirtualFileStream GetStream(VFSPath filePath, FileMode mode,
                FileAccess access, FileShare share) => null;
            public IVirtualFileStreamRequest GetStreamAsync(VFSPath filePath) => null;
            public IList<VFSPath> GetFiles() => new List<VFSPath>();
            public IList<VFSPath> GetFiles(IList<VFSPath> refs) => refs;
            public event OnGetStreamEventHandler OnGetStream { add { } remove { } }
            public void Dispose() { }
        }

        private static List<IVirtualFileSystem> Enumerate(FileSystemManager manager) {
            var list = new List<IVirtualFileSystem>();
            foreach (var fs in manager) {
                list.Add(fs);
            }
            return list;
        }

        /// <summary>
        /// Regression for R1: the mount counter used to be decremented on removal, which
        /// both orphaned higher-indexed mounts (the reader loop bound shrank past them)
        /// and silently dropped the next add (it reused a key still held by a survivor).
        /// The counter is now monotonic; removal leaves a gap skipped by readers.
        /// </summary>
        [Test]
        public void RemoveMiddleFileSystem_HigherIndexStillReachable() {
            var manager = new FileSystemManager();
            manager.Create();

            var fs0 = new FakeFS();
            var fs1 = new FakeFS();
            var fs2 = new FakeFS();
            manager.AddFileSystem(fs0);
            manager.AddFileSystem(fs1);
            manager.AddFileSystem(fs2);

            manager.RemoveFileSystem(fs1); // remove the middle one

            var remaining = Enumerate(manager);
            CollectionAssert.Contains(remaining, fs0, "fs0 should still be reachable");
            CollectionAssert.Contains(remaining, fs2, "fs2 (higher index) must remain reachable after removing fs1");

            // a subsequent add must not collide with the surviving fs2 and be silently dropped
            var fs3 = new FakeFS();
            manager.AddFileSystem(fs3);
            var afterReadd = Enumerate(manager);
            CollectionAssert.Contains(afterReadd, fs3, "fs3 was silently dropped on re-add (key collision)");
            CollectionAssert.Contains(afterReadd, fs2, "fs2 still reachable after re-add");

            manager.Destroy();
        }
    }
}
