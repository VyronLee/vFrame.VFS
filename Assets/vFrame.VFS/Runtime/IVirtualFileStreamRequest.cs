using System;

namespace vFrame.VFS
{
    public interface IVirtualFileStreamRequest : IAsync, IDisposable
    {
        IVirtualFileStream Stream { get; }
    }
}