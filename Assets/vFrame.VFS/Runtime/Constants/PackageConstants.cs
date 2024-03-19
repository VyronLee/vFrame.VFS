using vFrame.Core.Loggers;

namespace vFrame.VFS
{
    public static class FileSystemConst
    {
        // 虚拟文件系统LogTag
        public static readonly LogTag LogTag = new LogTag("VirtualFileSystem");
    }

    public static class PackageFileSystemConst
    {
        // 包文件标识
        public const long Id = 0x766672616d65;

        // 包文件版本号
        public const long Version = 3;

        // 包文件后缀
        public const string Ext = ".vpk";

        // ReSharper disable once CommentTypo
        // 文件列表加密密钥
        public const long FileListEncryptKey = 0x48774746376d6762; // HwGF7mgb

        // 文件名列表虚拟文件名
        public const string FileListFileName = "//__VPK_FILE_LIST__//";

        // 文件名列表压缩方式
        public const long FileListCompressType = BlockFlags.BlockCompressLZMA;
    }

    public static class BlockFlags
    {
        /// 块是否存在
        public const long BlockExists = 0x00000001;

        /// 块是否标记删除
        public const long BlockDeleted = 0x00000002;

        /// 块是否进行了压缩
        public const long BlockCompressed = 0x00000F00;

        /// 压缩算法LZMA
        public const long BlockCompressLZMA = 0x00000100;

        /// 压缩算法LZ4
        public const long BlockCompressLZ4 = 0x00000200;

        /// 压缩算法ZSTD
        public const long BlockCompressZSTD = 0x00000300;

        /// 压缩算法ZLIB
        public const long BlockCompressZLIB = 0x00000400;

        /// 块是否进行了加密
        public const long BlockEncrypted = 0x0000F000;

        /// 加密算法XOR
        public const long BlockEncryptXor = 0x00001000;

        /// 加密算法AES
        public const long BlockEncryptAes = 0x00002000;

        /// 块的主版本号
        public const long BlockVerMajor = 0xFF000000;

        /// 块的从版本号
        public const long BlockVerMinor = 0x00FF0000;

        /// 块的初始版本号
        public const long BlockInitVer = 0x01000000;
    }

    public static class BlockOpFlags
    {
        public const long New = 0x00000001;
        public const long Deleted = 0x00000002;
    }
}