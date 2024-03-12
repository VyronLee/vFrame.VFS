using System;
using System.IO;
using vFrame.Core.Loggers;

namespace vFrame.VFS
{
    internal class StandardReadonlyVirtualFileStreamRequest : ReadonlyVirtualFileStreamRequest
    {
        private bool _finished;

        public StandardReadonlyVirtualFileStreamRequest(Stream stream) {
            System.Threading.ThreadPool.QueueUserWorkItem(ReadFileStream, stream);
        }

        private void ReadFileStream(object state) {
            try {
                var stream  = (Stream) state;
                using (stream) {
                    var memoryStream = new MemoryStream((int)stream.Length);
                    stream.CopyTo(memoryStream, (int)stream.Length);
                    Stream = new StandardVirtualFileStream(memoryStream);
                }

                lock (_lockObject) {
                    _finished = true;

                    if (!_disposed)
                        return;
                    Stream.Dispose();
                    Stream = null;
                }
            }
            catch (Exception e) {
                Logger.Error(FileSystemConst.LogTag, "Error occurred while reading file: {0}", e);
            }
        }

        public override bool MoveNext() {
            lock (_lockObject) {
                return !_finished;
            }
        }
    }
}