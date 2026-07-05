using System;
using System.Threading;
using vFrame.Core;

namespace vFrame.VFS
{
    internal class PackageVirtualFileStreamRequest : VirtualFileStreamRequest
    {
        private bool _finished;

        public PackageVirtualFileStreamRequest(PackageVirtualFileSystemStream vpkStream, PackageBlockInfo blockInfo) {
            var context = new PackageStreamContext {
                Stream = vpkStream,
                BlockInfo = blockInfo
            };
            ThreadPool.QueueUserWorkItem(OpenPackageStreamAsync, context);
        }

        private void OpenPackageStreamAsync(object state) {
            try {
                var context = (PackageStreamContext)state;
                var vpkStream = context.Stream;
                var stream = new PackageVirtualFileStream(vpkStream, context.BlockInfo);
                if (!stream.Open()) {
                    throw new PackageStreamOpenFailedException();
                }
                Stream = stream;

                lock (_lockObject) {
                    _finished = true;

                    if (!_disposed) {
                        return;
                    }

                    Stream.Dispose();
                    Stream = null;
                }
            }
            catch (Exception e) {
                Logger.Error(FileSystemConst.LogTag, $"Error occurred while reading package: {e}");
            }
        }

        public override bool MoveNext() {
            lock (_lockObject) {
                return !_finished;
            }
        }

        private class PackageStreamContext
        {
            public PackageBlockInfo BlockInfo;
            public PackageVirtualFileSystemStream Stream;
        }
    }
}