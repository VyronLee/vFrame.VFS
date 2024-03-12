using System;
using vFrame.Core.Loggers;
using vFrame.Core.MultiThreading;

namespace vFrame.VFS
{
    public class BytesAsyncRequest : ThreadedAsyncRequest<byte[], string>, IBytesAsyncRequest
    {
        internal IFileSystemManager _fileSystemManager { get; set; }

        protected override byte[] OnThreadedHandle(string arg) {
            if (null == _fileSystemManager) {
                throw new ArgumentNullException("FileSystemManager cannot be null");
            }
            return _fileSystemManager.ReadAllBytes(arg);
        }

        protected override void ErrorHandler(Exception e) {
            Logger.Error(FileSystemConst.LogTag, "Exception occurred while reading: {0}, msg: {1}", Arg, e);
        }
    }
}