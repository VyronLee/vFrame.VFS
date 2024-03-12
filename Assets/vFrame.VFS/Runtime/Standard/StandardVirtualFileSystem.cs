using System;
using System.Collections.Generic;
using System.IO;

namespace vFrame.VFS
{
    internal class StandardVirtualFileSystem : VirtualFileSystem
    {
        private VFSPath _workingDir;

        public override void Open(VFSPath fsPath) {
            if (!Directory.Exists(fsPath)) {
                throw new DirectoryNotFoundException();
            }

            _workingDir = fsPath.AsDirectory();
        }

        public override void Close() {
        }

        public override bool Exist(VFSPath filePath) {
            return File.Exists(_workingDir + filePath);
        }

        public override IVirtualFileStream GetStream(VFSPath filePath,
            FileMode mode,
            FileAccess access,
            FileShare share
        ) {
            if (null == filePath) {
                throw new ArgumentNullException(nameof(filePath));
            }
            if (string.IsNullOrEmpty(filePath)) {
                throw new ArgumentException("file path cannot be empty.", nameof(filePath));
            }
            if (!Exist(filePath)) {
                throw new FileNotFoundException("File not found: " + filePath);
            }

            var absolutePath = _workingDir + filePath;
            var fileStream = new FileStream(absolutePath, mode, access, share);
            //Logger.Info(PackageFileSystemConst.LogTag, "Read stream: {0}", fileName);
            OnGetStream?.Invoke(_workingDir, filePath, fileStream.Length, fileStream.Length);
            return new StandardVirtualFileStream(fileStream);
        }

        public override IReadonlyVirtualFileStreamRequest GetReadonlyStreamAsync(VFSPath filePath) {
            if (null == filePath) {
                throw new ArgumentNullException(nameof(filePath));
            }
            if (string.IsNullOrEmpty(filePath)) {
                throw new ArgumentException("file path cannot be empty.", nameof(filePath));
            }

            if (!Exist(filePath)) {
                throw new FileNotFoundException("File not found: " + filePath);
            }

            var absolutePath = _workingDir + filePath;
            var fileStream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read);
            //Logger.Info(PackageFileSystemConst.LogTag, "Read stream async: {0}", fileName);
            OnGetStream?.Invoke(_workingDir, filePath, fileStream.Length, fileStream.Length);
            return new StandardReadonlyVirtualFileStreamRequest(fileStream);
        }

        public override IList<VFSPath> List(IList<VFSPath> refs) {
            var dirInfo = new DirectoryInfo(_workingDir);
            TravelDirectory(dirInfo, refs);
            return refs;
        }

        public override event OnGetStreamEventHandler OnGetStream;

        private void TravelDirectory(DirectoryInfo dir, IList<VFSPath> refs) {
            foreach (var fileInfo in dir.GetFiles()) {
                var full = VFSPath.Create(fileInfo.FullName);
                var relative = full.GetRelative(_workingDir);
                if (string.IsNullOrEmpty(relative)) {
                    continue;
                }
                refs.Add(relative);
            }

            foreach (var subDir in dir.GetDirectories()) {
                TravelDirectory(subDir, refs);
            }
        }

        public override string ToString() {
            return _workingDir;
        }
    }
}