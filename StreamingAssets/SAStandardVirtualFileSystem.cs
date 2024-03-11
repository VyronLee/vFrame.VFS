﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using vFrame.Core.Utils;

namespace vFrame.VFS.UnityExtension
{
    public class SAStandardVirtualFileSystem : VirtualFileSystem
    {
        private VFSPath _workingDir;

        public override void Open(VFSPath fsPath) {
            if (fsPath == Application.streamingAssetsPath) {
                // Root directory
            }
            else if (!BetterStreamingAssets.DirectoryExists(fsPath)) {
                throw new DirectoryNotFoundException();
            }

            if (!PathUtils.IsStreamingAssetsPath(fsPath)) {
                throw new ArgumentException("Input argument must be streaming-assets path.");
            }

            _workingDir = fsPath.AsDirectory();
            _workingDir = PathUtils.AbsolutePathToRelativeStreamingAssetsPath(_workingDir);
        }

        public override void Close() {

        }

        public override bool Exist(VFSPath filePath) {
            return BetterStreamingAssets.FileExists(_workingDir + filePath);
        }

        public override IVirtualFileStream GetStream(VFSPath filePath, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read,
            FileShare share = FileShare.Read) {
            var fullPath = _workingDir + filePath;
            var stream = BetterStreamingAssets.OpenRead(fullPath);
            return new SAStandardVirtualFileStream(stream);
        }

        public override IReadonlyVirtualFileStreamRequest GetReadonlyStreamAsync(VFSPath filePath) {
            var fullPath = _workingDir + filePath;
            return new SAStandardReadonlyVirtualFileStreamRequest(fullPath);
        }

        public override IList<VFSPath> List(IList<VFSPath> refs) {
            throw new NotSupportedException();
        }

        #pragma warning disable 67
        public override event OnGetStreamEventHandler OnGetStream;
        #pragma warning restore 67

        public override string ToString() {
            return VFSPath.Create(Application.streamingAssetsPath) + _workingDir;
        }
    }
}
