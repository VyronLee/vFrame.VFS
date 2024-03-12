using System.IO;

namespace vFrame.VFS
{
    internal class StandardVirtualFileStream : VirtualFileStream
    {
        private readonly Stream _fileStream;

        public StandardVirtualFileStream(Stream stream) {
            _fileStream = stream;
            _fileStream.Seek(0, SeekOrigin.Begin);
        }

        public override void Close() {
            _fileStream.Close();
            base.Close();
        }

        public override bool CanRead => _fileStream.CanRead;
        public override bool CanSeek => _fileStream.CanSeek;
        public override bool CanWrite => _fileStream.CanWrite;
        public override long Length => _fileStream.Length;

        public override long Position {
            get => _fileStream.Position;
            set => _fileStream.Position = value;
        }

        public override void Flush() {
            _fileStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return _fileStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) {
            return _fileStream.Seek(offset, origin);
        }

        public override void SetLength(long value) {
            _fileStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            _fileStream.Write(buffer, offset, count);
        }
    }
}