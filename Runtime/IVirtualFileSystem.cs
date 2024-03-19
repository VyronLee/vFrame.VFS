using System;
using System.Collections.Generic;
using System.IO;

namespace vFrame.VFS
{
    public interface IVirtualFileSystem : IDisposable
    {
        /// <summary>
        ///     Open file system.
        /// </summary>
        /// <param name="fsPath">Working directory or package file path.</param>
        /// <returns></returns>
        void Open(VFSPath fsPath);

        /// <summary>
        ///     Close file system.
        /// </summary>
        /// <returns></returns>
        void Close();

        /// <summary>
        ///     Is file with relative path exist?
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        bool Exist(VFSPath filePath);

        /// <summary>
        ///     Get file stream of specified name.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="mode"></param>
        /// <param name="access"></param>
        /// <param name="share"></param>
        /// <returns></returns>
        IVirtualFileStream GetStream(VFSPath filePath, FileMode mode = FileMode.Open,
            FileAccess access = FileAccess.Read, FileShare share = FileShare.Read);

        /// <summary>
        ///     Get readonly file stream async of specified name.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        IVirtualFileStreamRequest GetStreamAsync(VFSPath filePath);

        /// <summary>
        ///     List all files in this file system.
        /// </summary>
        IList<VFSPath> GetFiles();

        /// <summary>
        ///     List all files in this file system.
        /// </summary>
        IList<VFSPath> GetFiles(IList<VFSPath> refs);

        /// <summary>
        ///     On get stream callback.
        /// </summary>
        event OnGetStreamEventHandler OnGetStream;
    }

    public delegate void OnGetStreamEventHandler(string vfsPath, string filePath, long originalSize,
        long compressedSize);
}