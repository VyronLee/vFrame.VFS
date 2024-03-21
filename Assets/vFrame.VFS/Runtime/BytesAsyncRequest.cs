using System;
using vFrame.Core.Loggers;
using vFrame.Core.MultiThreading;

namespace vFrame.VFS
{
    public class BytesAsyncRequest : ThreadedTask<byte[], string>, IBytesAsyncRequest
    {
        internal IFileSystemManager FileSystemManager { get; set; }

        protected override byte[] OnHandleTask(string arg) {
            if (null == FileSystemManager) {
                throw new ArgumentNullException(nameof(FileSystemManager), "FileSystemManager cannot be null");
            }
            return FileSystemManager.ReadAllBytes(arg);
        }

        protected override void ErrorHandler(Exception e) {
            Logger.Error(FileSystemConst.LogTag, "Exception occurred while reading: {0}, msg: {1}", Arg, e);
        }
    }
}