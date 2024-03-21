//------------------------------------------------------------
//        File:  PackageStream.cs
//       Brief:  Package stream.
//
//      Author:  VyronLee, lwz_jz@hotmail.com
//
//    Modified:  2020-03-11 16:39
//   Copyright:  Copyright (c) 2020, VyronLee
//============================================================

using System;
using System.Diagnostics;
using System.IO;
using vFrame.Core.Compression;
using vFrame.Core.Encryption;
using vFrame.Core.Profiles;

namespace vFrame.VFS
{
    internal class PackageVirtualFileStream : VirtualFileStream
    {
        private readonly PackageBlockInfo _blockInfo;

        private readonly PackageVirtualFileSystemStream _vpkStream;
        private bool _closed;
        private byte[] _dataBuffer;
        private MemoryStream _memoryStream;
        private bool _opened;

        internal PackageVirtualFileStream(PackageVirtualFileSystemStream vpkStream, PackageBlockInfo blockInfo) {
            _vpkStream = vpkStream;
            _blockInfo = blockInfo;
        }

        public override bool CanRead => _opened && !_closed;
        public override bool CanSeek => _opened && !_closed;
        public override bool CanWrite => false;
        public override long Length => _blockInfo.OriginalSize;

        public override long Position {
            get => _memoryStream.Position;
            set => _memoryStream.Position = value;
        }

        public bool Open() {
            Debug.Assert(null != _vpkStream);
            PerfProfile.Start(out var id);
            PerfProfile.Pin("PackageVirtualFileStream:InternalOpen", id);
            var ret = InternalOpen();
            PerfProfile.Unpin(id);
            return ret;
        }

        public override void Close() {
            if (_closed) {
                return;
            }
            _closed = true;

            _vpkStream?.Close();
            _vpkStream?.Dispose();

            if (null != _dataBuffer) {
                BufferPool.Shared.Return(_dataBuffer, true);
            }
            _dataBuffer = null;

            _memoryStream?.Close();
            _memoryStream?.Dispose();

            base.Close();
        }

        public byte[] GetBuffer() {
            ValidateStreamState();
            return _memoryStream.GetBuffer();
        }

        public byte[] ToArray() {
            ValidateStreamState();
            return _memoryStream.ToArray();
        }

        public override void Flush() {
            ValidateStreamState();
            _memoryStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            ValidateStreamState();
            return _memoryStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) {
            ValidateStreamState();
            return _memoryStream.Seek(offset, origin);
        }

        public override void SetLength(long value) {
            ValidateStreamState();
            _memoryStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            ValidateStreamState();
            _memoryStream.Write(buffer, offset, count);
        }

        //=======================================================//
        //                         Private                     ==//
        //=======================================================//

        private void ValidateStreamState() {
            if (!_opened) {
                throw new PackageStreamNotOpenedException();
            }
            if (_closed) {
                throw new PackageStreamClosedException();
            }
        }

        private bool InternalOpen() {
            ValidateBlockInfo(_vpkStream);

            var maxSize = Math.Max(_blockInfo.OriginalSize, _blockInfo.CompressedSize);
            if (maxSize > int.MaxValue) {
                throw new PackageBlockDataSizeTooLargeException(maxSize);
            }
            _dataBuffer = BufferPool.Shared.Rent((int)maxSize);

            // 1. copy data to temp buffer first
            var dataSize = (_blockInfo.Flags & BlockFlags.BlockCompressed) > 0
                ? _blockInfo.CompressedSize
                : _blockInfo.OriginalSize;

            var tempStream = new MemoryStream(_dataBuffer);
            tempStream.Seek(0, SeekOrigin.Begin);
            tempStream.SetLength(0);

            _vpkStream.Lock();
            BufferedCopyTo(_vpkStream, tempStream, _blockInfo.Offset, (int)dataSize);
            _vpkStream.Unlock();

            // 2. decompress
            if ((_blockInfo.Flags & BlockFlags.BlockCompressed) > 0) {
                PerfProfile.Start(out var id);
                PerfProfile.Pin($"PackageVirtualFileStream:Decompress size: {_blockInfo.OriginalSize:n0} bytes: ", id);
                var buffer = BufferPool.Shared.Rent((int)_blockInfo.OriginalSize);
                using (var decompressedStream = new MemoryStream(buffer)) {
                    decompressedStream.SetLength(0);
                    decompressedStream.Seek(0, SeekOrigin.Begin);

                    var compressType = (_blockInfo.Flags & BlockFlags.BlockCompressed) >> 8;
                    using (var compressor = CompressorPool.Instance().Rent((CompressorType)compressType)) {
                        tempStream.Seek(0, SeekOrigin.Begin);
                        compressor.Decompress(tempStream, decompressedStream);
                    }

                    tempStream.SetLength(0);
                    tempStream.Seek(0, SeekOrigin.Begin);

                    BufferedCopyTo(decompressedStream, tempStream, 0, (int)_blockInfo.OriginalSize);
                }

                BufferPool.Shared.Return(buffer);
                PerfProfile.Unpin(id);
            }

            // 3. decrypt
            if ((_blockInfo.Flags & BlockFlags.BlockEncrypted) > 0) {
                PerfProfile.Start(out var id);
                PerfProfile.Pin($"PackageVirtualFileStream:Decrypt size: {_blockInfo.OriginalSize:n0} bytes", id);
                var buffer = BufferPool.Shared.Rent((int)_blockInfo.OriginalSize);
                using (var decryptedStream = new MemoryStream(buffer)) {
                    decryptedStream.SetLength(0);
                    decryptedStream.Seek(0, SeekOrigin.Begin);

                    var cryptoKey = BitConverter.GetBytes(_blockInfo.EncryptKey);
                    var cryptoType = (_blockInfo.Flags & BlockFlags.BlockEncrypted) >> 12;
                    using (var encryptor = EncryptorPool.Instance().Rent((EncryptorType)cryptoType)) {
                        tempStream.Seek(0, SeekOrigin.Begin);
                        encryptor.Decrypt(tempStream, decryptedStream, cryptoKey, cryptoKey.Length);
                    }

                    tempStream.SetLength(0);
                    tempStream.Seek(0, SeekOrigin.Begin);

                    BufferedCopyTo(decryptedStream, tempStream, 0, (int)_blockInfo.OriginalSize);
                }

                BufferPool.Shared.Return(buffer);
                PerfProfile.Unpin(id);
            }

            _memoryStream = tempStream;
            _memoryStream.Seek(0, SeekOrigin.Begin);

            if (_memoryStream.Length != _blockInfo.OriginalSize) {
                throw new PackageStreamDataLengthMismatchException(_memoryStream.Length, _blockInfo.OriginalSize);
            }

            _opened = true;
            _closed = false;
            return true;
        }

        private void ValidateBlockInfo(Stream inputStream) {
            if ((_blockInfo.Flags & BlockFlags.BlockExists) <= 0) {
                throw new PackageBlockDisposedException();
            }

            var sizeOfHeader = PackageHeader.GetMarshalSize();
            if (_blockInfo.Offset < sizeOfHeader) {
                throw new PackageBlockOffsetErrorException(_blockInfo.Offset, sizeOfHeader);
            }

            var blockSize = (_blockInfo.Flags & BlockFlags.BlockCompressed) > 0
                ? _blockInfo.CompressedSize
                : _blockInfo.OriginalSize;

            if (inputStream.Length < _blockInfo.Offset + blockSize) {
                throw new PackageStreamDataErrorException(_blockInfo.Offset + blockSize, inputStream.Length);
            }
        }

        private static void BufferedCopyTo(Stream from, Stream to, long offset, int count) {
            if (count <= 0) {
                return;
            }
            const int BufferSize = 128 * 1024;
            var buffer = BufferPool.Shared.Rent(BufferSize);
            int read;
            from.Seek(offset, SeekOrigin.Begin);
            while (count > 0 && (read = from.Read(buffer, 0, Math.Min(count, BufferSize))) != 0) {
                to.Write(buffer, 0, read);
                count -= read;
            }

            BufferPool.Shared.Return(buffer);
        }
    }
}