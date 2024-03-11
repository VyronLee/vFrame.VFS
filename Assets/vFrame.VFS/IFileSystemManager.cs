using System.Collections.Generic;
using System.IO;
using vFrame.Core.Base;

namespace vFrame.VFS
{
    public interface IFileSystemManager : IBaseObject
    {
        /// <summary>
        /// Add file system by path.
        /// </summary>
        /// <param name="vfsPath"></param>
        /// <returns></returns>
        IVirtualFileSystem AddFileSystem(VFSPath vfsPath);

        /// <summary>
        /// Add file system.
        /// </summary>
        /// <param name="virtualFileSystem"></param>
        void AddFileSystem(IVirtualFileSystem virtualFileSystem);

        /// <summary>
        /// Remove file systems.
        /// </summary>
        /// <param name="virtualFileSystem"></param>
        void RemoveFileSystem(IVirtualFileSystem virtualFileSystem);

        /// <summary>
        /// Get stream with path from file systems.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        IVirtualFileStream GetStream(string path, FileMode mode = FileMode.Open);

        /// <summary>
        /// Get readonly file stream async of specified name.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        IReadonlyVirtualFileStreamRequest GetReadonlyStreamAsync(string fileName);

        /// <summary>
        /// Get file system enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator<IVirtualFileSystem> GetEnumerator();

        /// <summary>
        /// Read all text from path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        string ReadAllText(string path);

        /// <summary>
        /// Read all bytes from path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        byte[] ReadAllBytes(string path);

        /// <summary>
        /// Read all text from path async.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>ITextAsyncRequest</returns>
        ITextAsyncRequest ReadAllTextAsync(string path);

        /// <summary>
        /// Read all bytes from path async.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>IBytesAsyncRequest</returns>
        IBytesAsyncRequest ReadAllBytesAsync(string path);
    }
}