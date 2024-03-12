using System;
using System.IO;

namespace vFrame.VFS
{
    public static class PackageVirtualFileOperator
    {
        public enum ProcessState
        {
            CalculatingBlockInfo,
            WritingHeader,
            WritingBlockInfo,
            WritingBlockData,
        }

        public static void CreatePackage(string directory,
            string outputPath,
            bool force = false,
            Action<ProcessState, float, float> onProgress = null
        ) {
            CreatePackage(directory,
                outputPath,
                BlockFlags.BlockEncryptXor,
                PackageFileSystemConst.Id,
                BlockFlags.BlockCompressLZMA,
                force,
                onProgress);
        }

        public static void CreatePackage(string directory,
            string outputPath,
            long encryptType,
            long encryptKey,
            long compressType,
            bool force = false,
            Action<ProcessState, float, float> onProgress = null
        ) {
            var dir = VFSPath.Create(directory).AsDirectory();
            var stdFileSystem = new StandardVirtualFileSystem();
            stdFileSystem.Open(dir);

            var files = stdFileSystem.List();

            if (force && File.Exists(outputPath)) {
                File.Delete(outputPath);
            }
            var packageSystem = PackageVirtualFileSystem.CreatePackage(outputPath);
            packageSystem.OnProgress += onProgress;

            try {
                var idx = 0f;
                foreach (var path in files) {
                    onProgress?.Invoke(ProcessState.CalculatingBlockInfo, ++idx, files.Count);
                    using (var fd = File.OpenRead(dir + path)) {
                        packageSystem.AddStream(path, fd, encryptType, encryptKey, compressType);
                    }
                }
                packageSystem.Flush(true);
            }
            finally {
                packageSystem.Close();
            }

            stdFileSystem.Close();
        }

        public static void ExtractPackage(string pkgPath,
            string destPath,
            bool force = false,
            Action<string, float, float> onProgress = null
        ) {
            var pkgFileSystem = new PackageVirtualFileSystem();
            pkgFileSystem.Open(pkgPath);

            var stdFileSystem = new StandardVirtualFileSystem();
            stdFileSystem.Open(destPath);

            var files = pkgFileSystem.List();
            var idx = 0;
            var total = files.Count;
            foreach (var path in files) {
                using (var input = (Stream) pkgFileSystem.GetStream(path)) {
                    var absolute = VFSPath.Create(destPath).Combine(path);
                    var dirName = absolute.GetDirectoryName();
                    Directory.CreateDirectory(dirName);

                    if (!force && stdFileSystem.Exist(path)) {
                        throw new FileAlreadyExistException(path);
                    }

                    using (var output = new FileStream(absolute, FileMode.Create)) {
                        input.CopyTo(output);
                    }
                }

                onProgress?.Invoke(path, idx++, total);
            }

            pkgFileSystem.Close();
            stdFileSystem.Close();
        }
    }
}