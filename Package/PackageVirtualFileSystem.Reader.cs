using System.IO;
using System.Text;

namespace vFrame.VFS
{
    public partial class PackageVirtualFileSystem
    {
        private bool ReadHeader() {
            if (_vpkStream.Length < PackageHeader.GetMarshalSize())
                return false;

            _vpkStream.Seek(0, SeekOrigin.Begin);

            using (var reader = new BinaryReader(_vpkStream, Encoding.UTF8, true)) {
                _header.Id = reader.ReadInt64();
                _header.Version = reader.ReadInt64();
                _header.TotalSize = reader.ReadInt64();
                _header.BlockTableOffset = reader.ReadInt64();
                _header.BlockTableSize = reader.ReadInt64();
                _header.BlockOffset = reader.ReadInt64();
                _header.BlockSize = reader.ReadInt64();
                _header.Reserved1 = reader.ReadInt64();
                _header.Reserved2 = reader.ReadInt64();
                _header.Reserved3 = reader.ReadInt64();
            }

            return ValidateHeader(_header);
        }

        private bool ReadFileList() {
            _filePathList.Clear();

            var blockInfo = _blockInfos[_blockInfos.Count - 1]; // Last block is filename list
            using (var stream = new PackageVirtualFileStream(GetVPKStream(), blockInfo)) {
                if (!stream.Open()) {
                    throw new PackageFileSystemFileListDataErrorException();
                }

                using (var memoryStream = new MemoryStream(stream.ReadAllBytes())) {
                    using (var reader = new BinaryReader(memoryStream)) {
                        while (memoryStream.Position < memoryStream.Length) {
                            var len = reader.ReadInt32();
                            var bytes = reader.ReadBytes(len);
                            if (bytes.Length != len) {
                                throw new PackageStreamDataErrorException(bytes.Length, len);
                            }

                            var name = Encoding.UTF8.GetString(bytes);
                            _filePathList.Add(name);
                        }
                    }
                }
            }
            return true;
        }

        private bool ReadBlockTable() {
            if (_vpkStream.Length < _header.BlockTableOffset + _header.BlockTableSize)
                return false;

            _blockInfos.Clear();

            _vpkStream.Seek(_header.BlockTableOffset, SeekOrigin.Begin);
            while (_vpkStream.Position < _header.BlockTableOffset + _header.BlockTableSize)
                using (var reader = new BinaryReader(_vpkStream, Encoding.UTF8, true)) {
                    var block = new PackageBlockInfo {
                        Flags = reader.ReadInt64(),
                        Offset = reader.ReadInt64(),
                        OriginalSize = reader.ReadInt64(),
                        CompressedSize = reader.ReadInt64(),
                        EncryptKey = reader.ReadInt64()
                    };
                    _blockInfos.Add(block);
                }

            if (_vpkStream.Position != _header.BlockTableOffset + _header.BlockTableSize)
                return false;

            return true;
        }

        private static bool ValidateHeader(PackageHeader header) {
            return header.Id == PackageFileSystemConst.Id
                   && header.Version == PackageFileSystemConst.Version
                   && header.TotalSize > PackageHeader.GetMarshalSize()
                   && header.BlockTableOffset >= header.BlockOffset + header.BlockSize
                   && header.BlockTableSize % PackageBlockInfo.GetMarshalSize() == 0
                   && header.BlockOffset >= PackageHeader.GetMarshalSize()
                   && header.BlockSize >= 0
                ;
        }
    }
}