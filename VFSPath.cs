using System;
using System.IO;
using vFrame.Core.Extensions;
using vFrame.Core.Utils;

namespace vFrame.VFS
{
    public struct VFSPath : IComparable
    {
        private string _value;

        private VFSPath(string value) {
            if (null == value) {
                throw new ArgumentNullException(nameof(value));
            }
            _value = string.Empty;
            _value = Normalize(value);
        }

        public static VFSPath Create(string value) {
            if (null == value) {
                throw new ArgumentNullException(nameof(value));
            }
            return new VFSPath(value);
        }

        private static string Normalize(string value) {
            return value.Replace("\\", "/");
        }

        private VFSPath EnsureDirectoryPath() {
            if (!_value.EndsWith("/"))
                _value += "/";
            return this;
        }

        public VFSPath AsDirectory() {
            return EnsureDirectoryPath();
        }

        public VFSPath SkipBase(int level = 1) {
            var value = _value;
            var index = -1;
            while (level-- > 0) {
                index = value.IndexOf('/');
                if (index < 0) {
                    break;
                }
            }

            if (index >= 0) {
                return value.Substring(index + 1);
            }
            return value;
        }

        public VFSPath SkipParent(int level = 1) {
            var value = _value;
            var index = -1;
            while (level-- > 0) {
                index = value.LastIndexOf('/');
                if (index < 0) {
                    break;
                }
            }

            if (index >= 0) {
                return value.Substring(0, index);
            }
            return value;
        }

        public string GetValue() {
            return _value;
        }

        public string GetHash() {
            return MessageDigestUtils.MD5(_value.ToUtf8ByteArray());
        }

        public string GetFileName() {
            return Path.GetFileName(_value);
        }

        public VFSPath GetDirectory() {
            return new VFSPath(GetDirectoryName());
        }

        public string GetDirectoryName() {
            return Path.GetDirectoryName(_value);
        }

        public string GetExtension() {
            return Path.GetExtension(_value);
        }

        public VFSPath GetRelative(VFSPath target) {
            if (_value.Contains(target._value)) {
                return _value.Substring(target._value.Length);
            }
            throw new PathNotRelativeException(this, target);
        }

        public VFSPath Combine(VFSPath target) {
            return AsDirectory() + target;
        }

        public bool IsAbsolute() {
            // unix style: "/user/data"
            // window style: "c:/user/data"
            return _value.Length > 0 && _value[0] == '/'
                   || _value.Length >= 3 && _value[1] == ':' && _value[2] == '/';
        }

        public override string ToString() {
            return _value;
        }

        public int CompareTo(object obj) {
            switch (obj) {
                case VFSPath path:
                    return _value.CompareTo(path._value);
                case string str:
                    return _value.CompareTo(str);
                default:
                    throw new NotSupportedException();
            }
        }

        public override bool Equals(object obj) {
            switch (obj) {
                case VFSPath path:
                    return path._value == _value;
                case string str:
                    return str == _value;
                default:
                    return false;
            }
        }

        public override int GetHashCode() {
            return _value.GetHashCode();
        }

        public static bool operator ==(VFSPath p1, VFSPath p2) {
            return p1._value == p2._value;
        }

        public static bool operator !=(VFSPath p1, VFSPath p2) {
            return !(p1 == p2);
        }

        public static implicit operator string(VFSPath vfsPath) {
            return vfsPath.GetValue();
        }

        public static implicit operator VFSPath(string value) {
            return Create(value ?? string.Empty);
        }
    }
}