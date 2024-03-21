using System;
using vFrame.Core.Unity.Utils;

namespace vFrame.VFS.UnityExtension
{
    public class SAPackageVirtualFileSystem : PackageVirtualFileSystem
    {
        public override void Open(VFSPath fsPath) {
            if (!PathUtils.IsStreamingAssetsPath(fsPath)) {
                throw new ArgumentException("Input argument must be streaming-assets path.");
            }

            var relativePath = PathUtils.AbsolutePathToRelativeStreamingAssetsPath(fsPath);
            var stream = BetterStreamingAssets.OpenRead(relativePath);

            Open(stream);

            ReadOnly = true;
            PackageFilePath = fsPath;
        }
    }
}