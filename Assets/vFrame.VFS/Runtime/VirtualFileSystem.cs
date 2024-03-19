using System.Collections.Generic;
using System.IO;

namespace vFrame.VFS
{
    public abstract class VirtualFileSystem : IVirtualFileSystem
    {
        public abstract void Open(VFSPath fsPath);
        public abstract void Close();

        public abstract bool Exist(VFSPath filePath);

        public abstract IVirtualFileStream GetStream(VFSPath filePath, FileMode mode = FileMode.Open,
            FileAccess access = FileAccess.Read, FileShare share = FileShare.Read);

        public abstract IVirtualFileStreamRequest GetStreamAsync(VFSPath filePath);

        public virtual IList<VFSPath> GetFiles() {
            return GetFiles(new List<VFSPath>());
        }

        public abstract IList<VFSPath> GetFiles(IList<VFSPath> refs);
        public abstract event OnGetStreamEventHandler OnGetStream;

        public void Dispose() {
            Close();
        }
    }
}