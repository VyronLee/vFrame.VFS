using System.IO;

namespace vFrame.VFS
{
    public interface IPackageVirtualFileSystem : IVirtualFileSystem
    {
        void Open(Stream stream, bool leaveOpen = false);
        VFSPath PackageFilePath { get; }
        PackageBlockInfo GetBlockInfo(string filePath);
        void AddFile(string filePath);
        void AddFile(string filePath, long encryptType, long encryptKey, long compressType);
        void AddStream(string filePath, Stream stream);
        void AddStream(string filePath, Stream stream, long encryptType, long encryptKey, long compressType);
        void DeleteFile(string filePath);
        void Flush(bool clean = false);
        bool ReadOnly { get; }
        event OnGetPackageBlockEventHandler OnGetBlock;
    }

    public delegate void OnGetPackageBlockEventHandler(string vfsPath, string filePath);
}