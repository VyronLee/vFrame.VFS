// ------------------------------------------------------------
//         File: VFSPath.cs
//        Brief: VFSPath.cs
//
//       Author: VyronLee, lwz_jz@hotmail.com
//
//      Created: 2024-3-12 23:28
//    Copyright: Copyright (c) 2024, VyronLee
// ============================================================

using System.IO;
using vFrame.Core.Utils;

namespace vFrame.VFS.UnityExtension
{
    public static class VFSFile
    {
        public static bool Exist(string path) {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!PathUtils.IsFileInPersistentDataPath(path))
            {
                var relativePath = PathUtils.AbsolutePathToRelativeStreamingAssetsPath(path);
                return BetterStreamingAssets.FileExists(relativePath);
            }
            else
#endif
            {
                return File.Exists(path);
            }
        }

        public static byte[] ReadAllBytes(string path) {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!PathUtils.IsFileInPersistentDataPath(path))
            {
                var relativePath = PathUtils.AbsolutePathToRelativeStreamingAssetsPath(path);
                return BetterStreamingAssets.ReadAllBytes(relativePath);
            }
            else
#endif
            {
                return File.ReadAllBytes(path);
            }
        }

        public static string ReadAllText(string path) {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!PathUtils.IsFileInPersistentDataPath(path))
            {
                var relativePath = PathUtils.AbsolutePathToRelativeStreamingAssetsPath(path);
                //Logger.Error("relativePath: {0}", relativePath);
                return BetterStreamingAssets.ReadAllText(relativePath);
            }
            else
#endif
            {
                return File.ReadAllText(path);
            }
        }
    }
}