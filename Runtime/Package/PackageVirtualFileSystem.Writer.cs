using System;
using System.IO;
using System.Linq;
using System.Text;
using vFrame.Core.Compress.Services;
using vFrame.Core.Crypto;
using vFrame.Core.Extensions;
using vFrame.Core.Loggers;

namespace vFrame.VFS
{
    public partial class PackageVirtualFileSystem
    {
        public Action<PackageVirtualFileOperator.ProcessState, float, float> OnProgress;

        private void PrepareWriting(bool skipDeleted = false) {
            OpenFileStreamIfRequired();
            SaveBlockOperations();
            RecalculateFileListBlockInfo(skipDeleted);
            RecalculateHeader(skipDeleted);
            RecalculateBlockTable(skipDeleted);
        }

        private void FinishWriting() {
            ClearBlockOperations();
            CloseFileStreamIfRequired();
        }

        private void OpenFileStreamIfRequired() {
            if (null != _vpkStream) {
                return;
            }

            if (_openFromStream) {
                return;
            }
            _vpkStream = new FileStream(_vpkVfsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        private void CloseFileStreamIfRequired() {
            if (null == _vpkStream) {
                return;
            }

            if (_openFromStream) {
                return;
            }
            _vpkStream.Close();
            _vpkStream.Dispose();
            _vpkStream = null;
        }

        private void SaveBlockOperations() {
            for (var i = 0; i < _blockInfos.Count; i++) {
                var blockInfo = _blockInfos[i];
                if ((blockInfo.OpFlags & BlockOpFlags.Deleted) > 0) {
                    blockInfo.Flags |= BlockFlags.BlockDeleted;
                }
                _blockInfos[i] = blockInfo;
            }
        }

        private void ClearBlockOperations() {
            for (var i = 0; i < _blockInfos.Count; i++) {
                var blockInfo = _blockInfos[i];
                blockInfo.OpFlags = 0;
                blockInfo.RawData = null;
                _blockInfos[i] = blockInfo;
            }
            _dirty = false;
        }

        private void RecalculateHeader(bool skipDeleted) {
            var validBlocks = _blockInfos.Where(
                v => !skipDeleted || (v.Flags & BlockFlags.BlockDeleted) <= 0).ToList();
            var blockSize = validBlocks.Sum(v => v.CompressedSize);
            var blockTableSize = PackageBlockInfo.GetMarshalSize() * validBlocks.Count;
            var headerSize = PackageHeader.GetMarshalSize();
            var totalSize = headerSize + blockTableSize + blockSize;

            _header.Id = PackageFileSystemConst.Id;
            _header.Version = PackageFileSystemConst.Version;
            _header.TotalSize = totalSize;
            _header.BlockTableOffset = totalSize - blockTableSize;
            _header.BlockTableSize = blockTableSize;
            _header.BlockOffset = PackageHeader.GetMarshalSize();
            _header.BlockSize = blockSize;
            _header.Reserved1 = 0;
            _header.Reserved2 = 0;
            _header.Reserved3 = 0;
        }

        private void RecalculateFileListBlockInfo(bool skipDeleted) {
            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) {
                    for (var i = 0; i < _filePathList.Count; i++) {
                        if (skipDeleted && (_blockInfos[i].Flags & BlockFlags.BlockDeleted) > 0) {
                            continue;
                        }
                        var path = _filePathList[i];
                        var buffer = path.GetValue().ToUtf8ByteArray();
                        writer.Write(buffer.Length);
                        writer.Write(buffer);
                    }
                }
                stream.Seek(0, SeekOrigin.Begin);

                AddStream(PackageFileSystemConst.FileListFileName, stream, 0, 0, PackageFileSystemConst.FileListCompressType);
            }
        }

        private void RecalculateBlockTable(bool skipDeleted) {
            var offset = _header.BlockOffset;
            for (var i = 0; i < _blockInfos.Count; i++) {
                var blockInfo = _blockInfos[i];
                if (skipDeleted && (_blockInfos[i].Flags & BlockFlags.BlockDeleted) > 0) {
                    continue;
                }
                blockInfo.Offset = offset;
                blockInfo.Flags |= BlockFlags.BlockExists;
                offset += blockInfo.CompressedSize;
                _blockInfos[i] = blockInfo;
            }
        }

        private static PackageBlockInfo CalculateBlockInfo(Stream stream, long encryptType, long encryptKey, long compressType) {
            var blockInfo = new PackageBlockInfo {
                OriginalSize = stream.Length,
                CompressedSize = stream.Length
            };

            var buffer = new byte[stream.Length];
            if (stream.Read(buffer, 0, (int) stream.Length) <= 0) {
                blockInfo.OriginalSize = blockInfo.CompressedSize = 0;
            }

            if (encryptType != 0) {
                buffer = EncryptBytes(buffer, encryptType, encryptKey);
                blockInfo.Flags |= encryptType;
                blockInfo.EncryptKey = encryptKey;
            }

            if (compressType != 0) {
                var temp = CompressBytes(buffer, compressType);
                if (temp.Length < buffer.Length) {
                    blockInfo.Flags |= compressType;
                    blockInfo.CompressedSize = temp.Length;
                    buffer = temp;
                }
                else {
                    Logger.Info($"Size({temp.Length:n0} bytes) greater than origin({buffer.Length:n0} bytes)"
                                + " after compressed, discard this operation.");
                }
            }

            blockInfo.RawData = buffer;
            return blockInfo;
        }

        private static byte[] EncryptBytes(byte[] buffer, long encryptType, long encryptKey) {
            using (var encrypted = new MemoryStream()) {
                using (var input = new MemoryStream(buffer)) {
                    var keyBytes = BitConverter.GetBytes(encryptKey);
                    var cryptoService = CryptoService.CreateCryptoService((CryptoType) (encryptType >> 12));
                    cryptoService.Encrypt(input, encrypted, keyBytes, keyBytes.Length);
                    CryptoService.DestroyCryptoService(cryptoService);
                    return encrypted.ToArray();
                }
            }
        }

        private static byte[] CompressBytes(byte[] buffer, long compressType) {
            using (var compressed = new MemoryStream()) {
                using (var input = new MemoryStream(buffer)) {
                    var compressService = CompressService.CreateCompressService((CompressType) (compressType >> 8));
                    compressService.Compress(input, compressed);
                    CompressService.DestroyCompressService(compressService);
                    return compressed.ToArray();
                }
            }
        }

        private void WriteHeader() {
            _vpkStream.Seek(0, SeekOrigin.Begin);

            OnProgress?.Invoke(PackageVirtualFileOperator.ProcessState.WritingHeader, 0, 1);
            using (var writer = new BinaryWriter(_vpkStream, Encoding.UTF8, true)) {
                writer.Write(_header.Id);
                writer.Write(_header.Version);
                writer.Write(_header.TotalSize);
                writer.Write(_header.BlockTableOffset);
                writer.Write(_header.BlockTableSize);
                writer.Write(_header.BlockOffset);
                writer.Write(_header.BlockSize);
                writer.Write(_header.Reserved1);
                writer.Write(_header.Reserved2);
                writer.Write(_header.Reserved3);
            }
            OnProgress?.Invoke(PackageVirtualFileOperator.ProcessState.WritingHeader, 1, 1);
        }

        private void WriteBlockTable(bool skipDeleted) {
            _vpkStream.Seek(_header.BlockTableOffset, SeekOrigin.Begin);

            var idx = 0f;
            using (var writer = new BinaryWriter(_vpkStream, Encoding.UTF8, true)) {
                for (var i = 0; i < _blockInfos.Count; i++) {
                    var block = _blockInfos[i];
                    if (skipDeleted && (block.Flags & BlockFlags.BlockDeleted) > 0) {
                        OnProgress?.Invoke(PackageVirtualFileOperator.ProcessState.WritingBlockInfo, idx++, _blockInfos.Count);
                        continue;
                    }
                    writer.Write(block.Flags);
                    writer.Write(block.Offset);
                    writer.Write(block.OriginalSize);
                    writer.Write(block.CompressedSize);
                    writer.Write(block.EncryptKey);

                    OnProgress?.Invoke(PackageVirtualFileOperator.ProcessState.WritingBlockInfo, idx++, _blockInfos.Count);
                }
            }
        }

        private void WriteBlockData(bool skipDeleted) {
            _vpkStream.Seek(_header.BlockOffset, SeekOrigin.Begin);

            var idx = 0f;
            var newBlockWrite = false;
            using (var writer = new BinaryWriter(_vpkStream, Encoding.UTF8, true)) {
                for (var i = 0; i < _blockInfos.Count; i++) {
                    var block = _blockInfos[i];
                    if ((block.OpFlags & BlockOpFlags.New) > 0) {
                        if (null == block.RawData || block.RawData.Length != block.CompressedSize) {
                            throw new PackageStreamDataErrorException(block.RawData?.Length ?? 0, block.CompressedSize);
                        }

                        writer.Write(block.RawData);
                        newBlockWrite = true;
                    }
                    else {
                        if (newBlockWrite) {
                            throw new PackageFileSystemApplicationException("New block data does not continuous!");
                        }

                        if (skipDeleted && (block.Flags & BlockFlags.BlockDeleted) > 0) {
                            continue;
                        }

                        _vpkStream.Seek(block.CompressedSize, SeekOrigin.Current);
                    }

                    OnProgress?.Invoke(PackageVirtualFileOperator.ProcessState.WritingBlockData, idx++,
                        _blockInfos.Count);
                }
            }

            var tailPosition = _header.BlockOffset + _header.BlockSize;
            if (_vpkStream.Position != tailPosition) {
                throw new PackageBlockOffsetErrorException(_vpkStream.Position, tailPosition);
            }
            _vpkStream.SetLength(tailPosition);
        }
    }
}