using System;
using vFrame.Core.Base;

namespace vFrame.VFS
{
    public interface IReadonlyVirtualFileStreamRequest : IAsync, IDisposable
    {
        IVirtualFileStream Stream { get; }
    }
}