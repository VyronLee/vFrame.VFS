using System.IO;

namespace vFrame.VFS
{
    public interface IPackageVirtualFileSystem : IVirtualFileSystem
    {
        VFSPath PackageFilePath { get; }
        bool ReadOnly { get; }
        void Open(Stream stream, bool leaveOpen = false);
        PackageBlockInfo GetBlockInfo(string filePath);
        void AddFile(string filePath);
        void AddFile(string filePath, long encryptType, long encryptKey, long compressType);
        void AddStream(string filePath, Stream stream);
        void AddStream(string filePath, Stream stream, long encryptType, long encryptKey, long compressType);
        void DeleteFile(string filePath);
        void Flush(bool clean = false);
        event OnGetPackageBlockEventHandler OnGetBlock;
    }

    public delegate void OnGetPackageBlockEventHandler(string vfsPath, string filePath);
}