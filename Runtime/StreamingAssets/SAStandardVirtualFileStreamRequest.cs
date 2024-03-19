using System.IO;
using UnityEngine.Networking;
using vFrame.Core.Loggers;
using vFrame.Core.Unity.Utils;

namespace vFrame.VFS.UnityExtension
{
    internal class SAStandardVirtualFileStreamRequest : VirtualFileStreamRequest
    {
        private bool _failed;
        private bool _finished;
        private UnityWebRequest _request;

        public SAStandardVirtualFileStreamRequest(string path) {
            var absolutePath = path.RelativeStreamingAssetsPathToAbsolutePath();
            _request = UnityWebRequest.Get(absolutePath);
        }

        public override bool MoveNext() {
            if (_finished || _failed) {
                return false;
            }

            if (_request.isDone) {
                Stream = new SAStandardVirtualFileStream(
                    new MemoryStream(_request.downloadHandler.data));

                _request.Dispose();
                _request = null;
                _finished = true;
                return false;
            }

#if UNITY_2020_1_OR_NEWER
            if (_request.result == UnityWebRequest.Result.ConnectionError || _request.result == UnityWebRequest.Result.ProtocolError) {
                Logger.Error(FileSystemConst.LogTag, "Error occurred while reading file: {0}", _request.error);
                _failed = true;
                return false;
            }
#else
            if (_request.isNetworkError || _request.isHttpError) {
                Logger.Error(FileSystemConst.LogTag, "Error occurred while reading file: {0}", _request.error);
                _failed = true;
                return false;
            }
#endif

            return true;
        }
    }
}