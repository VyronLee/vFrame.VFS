using System;
using vFrame.Core;

namespace vFrame.VFS
{
    public interface IVirtualFileStreamRequest : IAsync, IDisposable
    {
        IVirtualFileStream Stream { get; }
    }
}