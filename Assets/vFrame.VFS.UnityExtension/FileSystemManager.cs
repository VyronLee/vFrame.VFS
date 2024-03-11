using vFrame.Core.Utils;

namespace vFrame.VFS.UnityExtension
{
    public class FileSystemManager : VFS.FileSystemManager
    {
        protected override void OnCreate() {
            BetterStreamingAssets.Initialize();
            PathUtils.Initialize();
            base.OnCreate();
        }

        public new IVirtualFileSystem AddFileSystem(VFSPath vfsPath) {
            if (!PathUtils.IsStreamingAssetsPath(vfsPath)) {
                return base.AddFileSystem(vfsPath);
            }

            IVirtualFileSystem virtualFileSystem;
            switch (vfsPath.GetExtension().ToLower()) {
                case PackageFileSystemConst.Ext:
                    virtualFileSystem = new SAPackageVirtualFileSystem();
                    break;
                default:
                    virtualFileSystem = new SAStandardVirtualFileSystem();
                    break;
            }

            virtualFileSystem.Open(vfsPath);

            AddFileSystem(virtualFileSystem);
            return virtualFileSystem;
        }
    }
}