using System.IO;
using System.Text;

namespace vFrame.VFS
{
    public abstract class VirtualFileStream : Stream, IVirtualFileStream
    {
        private const int BufferSize = 1024 * 8;
        private BinaryReader _binaryReader;
        private StreamReader _streamReader;

        protected StreamReader StreamReader =>
            _streamReader ?? (_streamReader = new StreamReader(this, Encoding.UTF8, true, BufferSize, true));

        protected BinaryReader BinaryReader =>
            _binaryReader ?? (_binaryReader = new BinaryReader(this, Encoding.UTF8, true));

        public string ReadAllText() {
            Seek(0, SeekOrigin.Begin);
            return StreamReader.ReadToEnd();
        }

        public byte[] ReadAllBytes() {
            Seek(0, SeekOrigin.Begin);
            return BinaryReader.ReadBytes((int)Length);
        }
    }
}