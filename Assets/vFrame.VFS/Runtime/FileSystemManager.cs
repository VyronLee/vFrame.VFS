using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using vFrame.Core;

namespace vFrame.VFS
{
    public class FileSystemManager : BaseObject, IFileSystemManager
    {
        private readonly object _lockObject = new object();

        /// <summary>
        /// Monotonic key generator for <see cref="_fileSystems"/>. It is ONLY ever
        /// incremented — never decremented on removal — so that removing a middle file
        /// system leaves a gap (skipped by readers) without shrinking the key space or
        /// orphaning higher-indexed mounts. Decrementing it previously caused two bugs:
        /// readers' <c>for (i &lt; _nextId)</c> loop no longer reached higher keys, and the
        /// next <see cref="AddFileSystem"/> reused a key still held by a surviving mount
        /// (<see cref="ConcurrentDictionary{TKey,TValue}.TryAdd"/> silently failed).
        /// </summary>
        private int _nextId;
        private ConcurrentDictionary<int, IVirtualFileSystem> _fileSystems;

        public virtual IVirtualFileSystem AddFileSystem(VFSPath vfsPath) {
            IVirtualFileSystem virtualFileSystem;
            switch (vfsPath.GetExtension()) {
                case PackageFileSystemConst.Ext:
                    virtualFileSystem = new PackageVirtualFileSystem();
                    break;
                default:
                    virtualFileSystem = new StandardVirtualFileSystem();
                    break;
            }

            virtualFileSystem.Open(vfsPath);

            AddFileSystem(virtualFileSystem);
            return virtualFileSystem;
        }

        public void AddFileSystem(IVirtualFileSystem virtualFileSystem) {
            lock (_lockObject) {
                if (_fileSystems.TryAdd(_nextId, virtualFileSystem)) {
                    Interlocked.Increment(ref _nextId);
                }
            }
        }

        public void RemoveFileSystem(IVirtualFileSystem virtualFileSystem) {
            lock (_lockObject) {
                var index = -1;
                foreach (var kv in _fileSystems) {
                    if (kv.Value != virtualFileSystem) {
                        continue;
                    }

                    index = kv.Key;
                    break;
                }

                if (index < 0) {
                    return;
                }

                if (_fileSystems.TryRemove(index, out var fs)) {
                    // _nextId is monotonic — do NOT decrement. The removed key becomes a
                    // gap that readers skip via TryGetValue + continue.
                }
            }
        }

        public IVirtualFileStream GetStream(string path, FileMode mode = FileMode.Open) {
            var count = _nextId;
            for (var i = 0; i < count; i++) {
                if (!_fileSystems.TryGetValue(i, out var fileSystem)) {
                    continue;
                }

                if (fileSystem.Exist(path))
                    //Logger.Info(FileSystemConst.LogTag, "Get stream: \"{0}\" from file system: \"{1}\"",
                    //    path, fileSystem);
                {
                    return fileSystem.GetStream(path, mode);
                }
            }

            return null;
        }

        public IVirtualFileStreamRequest GetStreamAsync(string path) {
            var count = _nextId;
            for (var i = 0; i < count; i++) {
                if (!_fileSystems.TryGetValue(i, out var fileSystem)) {
                    continue;
                }

                if (fileSystem.Exist(path))
                    //Logger.Info(FileSystemConst.LogTag, "Get stream async: \"{0}\" from file system: \"{1}\"",
                    //    path, fileSystem);
                {
                    return fileSystem.GetStreamAsync(path);
                }
            }

            return null;
        }

        public IEnumerator<IVirtualFileSystem> GetEnumerator() {
            var count = _nextId;
            for (var i = 0; i < count; i++) {
                if (!_fileSystems.TryGetValue(i, out var fileSystem)) {
                    continue;
                }
                yield return fileSystem;
            }
        }

        public string ReadAllText(string path) {
            using (var stream = GetStream(path)) {
                return null == stream ? string.Empty : stream.ReadAllText();
            }
        }

        public byte[] ReadAllBytes(string path) {
            using (var stream = GetStream(path)) {
                return stream?.ReadAllBytes();
            }
        }

        public ITextAsyncRequest ReadAllTextAsync(string path) {
            var request = new TextAsyncRequest {
                _fileSystemManager = this
            };
            request.Create(path);
            return request;
        }

        public IBytesAsyncRequest ReadAllBytesAsync(string path) {
            var request = new BytesAsyncRequest {
                FileSystemManager = this
            };
            request.Create(path);
            return request;
        }

        protected override void OnCreate() {
            lock (_lockObject) {
                _fileSystems = new ConcurrentDictionary<int, IVirtualFileSystem>();
            }

            Interlocked.Exchange(ref _nextId, 0);
        }

        protected override void OnDestroy() {
            lock (_lockObject) {
                foreach (var kv in _fileSystems) {
                    kv.Value.Close();
                }
                _fileSystems.Clear();
            }

            Interlocked.Exchange(ref _nextId, 0);
        }
    }
}
