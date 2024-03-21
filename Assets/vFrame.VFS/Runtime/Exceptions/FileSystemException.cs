using System;

namespace vFrame.VFS
{
    public class FileSystemException : Exception
    {
        protected FileSystemException() { }

        protected FileSystemException(string message) : base(message) { }
    }

    public class PathNotRelativeException : FileSystemException
    {
        public PathNotRelativeException(VFSPath source, VFSPath target)
            : base($"source: {source}, target: {target}") { }
    }

    public class FileAlreadyExistException : FileSystemException
    {
        public FileAlreadyExistException(string fileName)
            : base($"{fileName} already exist.") { }

        public FileAlreadyExistException(VFSPath fileName)
            : base($"{fileName} already exist.") { }
    }

    public class FileSystemNotSupportedException : FileSystemException
    {
        public FileSystemNotSupportedException() { }

        public FileSystemNotSupportedException(string message) : base(message) { }
    }

    public class FileSystemBufferPoolInitFailedException : FileSystemException
    {
        public FileSystemBufferPoolInitFailedException(string message) : base(message) { }
    }

    public class FileSystemAlreadyOpenedException : FileSystemException { }

    public class FileSystemNotOpenedException : FileSystemException { }

    public class FileSystemAlreadyClosedException : FileSystemException { }

    public class PackageFileSystemHeaderDataErrorException : FileSystemException { }

    public class PackageFileSystemFileListDataErrorException : FileSystemException { }

    public class PackageFileSystemBlockTableDataErrorException : FileSystemException { }

    public class PackageFileSystemFileNotFoundException : FileSystemException
    {
        public PackageFileSystemFileNotFoundException() { }

        public PackageFileSystemFileNotFoundException(string message) : base(message) { }
    }

    public class PackageFileSystemApplicationException : FileSystemException
    {
        public PackageFileSystemApplicationException() { }

        public PackageFileSystemApplicationException(string message) : base(message) { }
    }

    public class PackageStreamOpenFailedException : FileSystemException { }

    public class PackageStreamNotOpenedException : FileSystemException { }

    public class PackageStreamClosedException : FileSystemException { }

    public class PackageBlockDisposedException : FileSystemException { }

    public class PackageBlockIndexOutOfRangeException : FileSystemException { }

    public class PackageBlockOffsetErrorException : FileSystemException
    {
        public PackageBlockOffsetErrorException(long offset, long need) : base($"At least: {need}, got: {offset}") { }
    }

    public class PackageBlockDataSizeTooLargeException : FileSystemException
    {
        public PackageBlockDataSizeTooLargeException(long size) : base($"Size too large: {size}") { }
    }

    public class PackageStreamDataErrorException : FileSystemException
    {
        public PackageStreamDataErrorException(long size, long need) : base($"At least: {need}, got: {size}") { }
    }

    public class PackageStreamDataLengthMismatchException : FileSystemException
    {
        public PackageStreamDataLengthMismatchException(long size, long expected)
            : base($"Got: {size}, expected: {expected}") { }
    }
}