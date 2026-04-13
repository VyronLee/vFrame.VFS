using System;
using vFrame.Core.Base;
using vFrame.Core.Loggers;

namespace vFrame.VFS
{
    public class BytesAsyncRequest : BaseObject<string>, IBytesAsyncRequest
    {
        private readonly object _lockObject = new object();
        private volatile bool _isDone;
        private byte[] _value;

        internal IFileSystemManager FileSystemManager { get; set; }

        /// <inheritdoc/>
        public byte[] Value
        {
            get
            {
                lock (_lockObject) { return _value; }
            }
        }

        /// <inheritdoc/>
        public bool IsDone => _isDone;

        /// <inheritdoc/>
        public float Progress => _isDone ? 1f : 0f;

        /// <inheritdoc/>
        public bool MoveNext() => !_isDone;

        /// <inheritdoc/>
        public void Reset() { }

        /// <inheritdoc/>
        public object Current => Value;

        /// <inheritdoc/>
        protected override void OnCreate(string arg) {
            System.Threading.Tasks.Task.Run(() => {
                try {
                    var result = OnHandleTask(arg);
                    lock (_lockObject) {
                        _value = result;
                    }
                }
                catch (Exception e) {
                    Logger.Error(FileSystemConst.LogTag,
                        "Exception occurred while reading: {0}, msg: {1}", arg, e);
                }
                _isDone = true;
            });
        }

        /// <inheritdoc/>
        protected override void OnDestroy() {
            _value = null;
        }

        private byte[] OnHandleTask(string arg) {
            if (null == FileSystemManager) {
                throw new ArgumentNullException(nameof(FileSystemManager), "FileSystemManager cannot be null");
            }
            return FileSystemManager.ReadAllBytes(arg);
        }
    }
}