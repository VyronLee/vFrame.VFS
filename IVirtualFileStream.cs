using System;
using System.IO;

namespace vFrame.VFS
{
    public interface IVirtualFileStream : IDisposable
    {
        long Position { get; set; }

        void Flush();

        long Seek(long offset, SeekOrigin origin);

        void SetLength(long value);

        long Length { get; }

        int Read(byte[] buffer, int offset, int count);

        void Write(byte[] buffer, int offset, int count);

        string ReadAllText();

        byte[] ReadAllBytes();
    }
}