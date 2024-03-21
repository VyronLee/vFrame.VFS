//------------------------------------------------------------
//        File:  PackageStruct.cs
//       Brief:  Package structures.
//
//      Author:  VyronLee, lwz_jz@hotmail.com
//
//    Modified:  2020-03-11 16:42
//   Copyright:  Copyright (c) 2020, VyronLee
//============================================================

namespace vFrame.VFS
{
    public struct PackageHeader
    {
        public long Id;
        public long Version;
        public long TotalSize;
        public long BlockTableOffset;
        public long BlockTableSize;
        public long BlockOffset;
        public long BlockSize;
        public long Reserved1;
        public long Reserved2;
        public long Reserved3;

        // 80 bytes
        public static int GetStructSize() {
            return sizeof(long) * 10;
        }
    }

    public struct PackageBlockInfo
    {
        public long Flags;
        public long Offset;
        public long OriginalSize;
        public long CompressedSize;
        public long EncryptKey;

        // 40 bytes
        public static int GetStructSize() {
            return sizeof(long) * 5;
        }

        // ==================================
        // Not save
        public long OpFlags;
        public byte[] RawData;
    }
}