using System;
using vFrame.Core.MultiThreading;

namespace vFrame.VFS
{
    public interface IVirtualFileStreamRequest : IAsync, IDisposable
    {
        IVirtualFileStream Stream { get; }
    }
}