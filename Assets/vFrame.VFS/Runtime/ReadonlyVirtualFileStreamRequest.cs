namespace vFrame.VFS
{
    public abstract class ReadonlyVirtualFileStreamRequest : IReadonlyVirtualFileStreamRequest
    {
        protected bool _disposed;
        protected readonly object _lockObject = new object();

        public abstract bool MoveNext();

        public void Reset() {
            Stream = null;
        }

        public object Current => Stream;
        public bool IsDone => !MoveNext();
        public float Progress { get; }

        public IVirtualFileStream Stream { get; protected set; }

        public void Dispose() {
            lock (_lockObject) {
                Stream?.Dispose();
                Stream = null;

                _disposed = true;
            }
        }
    }
}