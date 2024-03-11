using System;
using System.Threading;
using vFrame.Core.Loggers;
using vFrame.Core.Profiles;

namespace vFrame.VFS
{
    internal class PackageReadonlyVirtualFileStreamRequest : ReadonlyVirtualFileStreamRequest
    {
        private class PackageStreamContext
        {
            public PackageVirtualFileSystemStream Stream;
            public PackageBlockInfo BlockInfo;
        }

        private bool _finished;

        public PackageReadonlyVirtualFileStreamRequest(PackageVirtualFileSystemStream vpkStream, PackageBlockInfo blockInfo) {
            var context = new PackageStreamContext {
                Stream = vpkStream,
                BlockInfo = blockInfo,
            };
            ThreadPool.QueueUserWorkItem(OpenPackageStreamAsync, context);
        }

        private void OpenPackageStreamAsync(object state) {
            try {
                PerfProfile.Start(out var id);
                PerfProfile.Pin("PackageReadonlyVirtualFileStreamRequest:OpenPackageStreamAsync", id);

                var context = (PackageStreamContext) state;
                var vpkStream = context.Stream;
                var stream = new PackageVirtualFileStream(vpkStream, context.BlockInfo);
                if (!stream.Open())
                    throw new PackageStreamOpenFailedException();
                Stream = stream;

                PerfProfile.Unpin(id);

                lock (_lockObject) {
                    _finished = true;

                    if (!_disposed)
                        return;

                    Stream.Dispose();
                    Stream = null;
                }
            }
            catch (Exception e) {
                Logger.Error(FileSystemConst.LogTag, "Error occurred while reading package: {0}", e);
            }
        }

        public override bool MoveNext() {
            lock (_lockObject) {
                return !_finished;
            }
        }
    }
}