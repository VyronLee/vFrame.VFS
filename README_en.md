# vFrame VFS

![vFrame](https://img.shields.io/badge/vFrame-VFS-blue) [![Made with Unity](https://img.shields.io/badge/Made%20with-Unity-57b9d3.svg?style=flat&logo=unity)](https://unity3d.com) [![License](https://img.shields.io/badge/License-Apache%202.0-brightgreen.svg)](#License)

The Virtual File System (VFS) serves as an intermediary in games, designed to provide developers with a consistent method for handling game files and folders.

## Contents

* [Features](#features)
* [Typical Uses](#typical-uses)
* [Architecture](#architecture)
* [Installation](#installation)
* [vFrame.VFS](#vframe-core)
    + [Virtual File System Overview](#virtual-file-system-overview)
    + [Virtual File Stream Overview](#virtual-file-stream-overview)
    + [File System Manager](#file-system-manager)
    + [How to Use](#how-to-use)
        - [Using the File System Manager](#using-the-file-system-manager)
        - [How to Create Package Files](#how-to-create-package-files)
* [vFrame.VFS.UnityExtension](#vframe-core-unity-extension)
    + [How to Use](#how-to-use-unity-extension)
* [License](#license)

## Features

vFrame.VFS leverages the principles of a virtual file system to offer a suite of integrated features:
1. Allows for the creation of a file system centered around a designated "folder."
2. Introduces a proprietary "package file system" that operates through "file packages," enabling direct file manipulation within the package without extraction, complete with options for individual file compression and encryption.
3. Includes a file system manager that enables a unified file addressing system for all incorporated file systems.
4. Facilitates direct file access within StreamingAssets on Android devices, bypassing the need for asynchronous reading methods like UnityWebRequest.

## Typical Uses
1. Useful for consolidating all AssetBundles in a game into a single virtual file package (VPK), which helps prevent scattered distribution and enhances security against unauthorized access.
2. Ideal for amalgamating in-game configuration texts or data files for straightforward access and manipulation, with added benefits of compression and encryption, without embedding them into AssetBundles.
3. Streamlines the game patching process by bundling all update-required files into one virtual package for easy distribution; these can be effectively activated by adding them to the virtual file system manager in a specific sequence.
4. Compatible with `vFrame.Bundler` to facilitate AssetBundle loading without the need to extract files first.

## Architecture

The repository is split into two distinct Packages:
* `vFrame.VFS`, which operates independently of Unity and can be utilized in any non-Unity environment.
* `vFrame.VFS.UnityExtension`, which is a Unity-specific extension, catering to unique operations on the Android platform.

## Installation

Before utilizing this Package, you should first set up [vFrame Core](https://github.com/VyronLee/vFrame.Core) by following the provided instructions.

For installation, it's recommended to use the Unity Package Manager. Just add the URLs below to the package manager:

- For `vFrame.VFS`: https://github.com/VyronLee/vFrame.VFS.git#upm-vfs
- For `vFrame.VFS.UnityExtension`: https://github.com/VyronLee/vFrame.VFS.git#upm-vfs-unity

To install a specific version, append the desired version number to the end of the URL.

## vFrame.VFS

### Virtual File System Overview

The virtual file system comes in two main types:

* **Standard File System** This system is structured around a specific folder, and it gives you the ability to work with any file contained within that folder.

* **Package File System** This is a custom-designed file system that enables you to directly manage files inside a package without having to extract them first.

```csharp
public interface IVirtualFileSystem : IDisposable
{
    /// <summary>
    ///     Open file system.
    /// </summary>
    /// <param name="fsPath">Working directory or package file path.</param>
    /// <returns></returns>
    void Open(VFSPath fsPath);

    /// <summary>
    ///     Close file system.
    /// </summary>
    /// <returns></returns>
    void Close();

    /// <summary>
    ///     Is file with relative path exist?
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    bool Exist(VFSPath filePath);

    /// <summary>
    ///     Get file stream of specified name.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="mode"></param>
    /// <param name="access"></param>
    /// <param name="share"></param>
    /// <returns></returns>
    IVirtualFileStream GetStream(VFSPath filePath, FileMode mode = FileMode.Open,
        FileAccess access = FileAccess.Read, FileShare share = FileShare.Read);

    /// <summary>
    ///     Get readonly file stream async of specified name.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    IVirtualFileStreamRequest GetStreamAsync(VFSPath filePath);

    /// <summary>
    ///     List all files in this file system.
    /// </summary>
    IList<VFSPath> GetFiles();

    /// <summary>
    ///     List all files in this file system.
    /// </summary>
    IList<VFSPath> GetFiles(IList<VFSPath> refs);

    /// <summary>
    ///     On get stream callback.
    /// </summary>
    event OnGetStreamEventHandler OnGetStream;
}
```

The mentioned interfaces represent the actions you can perform with the virtual file system, and they cover the following:
* Opening the file system (this is required)
* Closing the file system
* Verifying the existence of a file with a given path (relative path)
* Retrieving a list of all file paths within the file system
* Accessing a file stream in a synchronous manner
* Accessing a file stream in an asynchronous manner

### Virtual File Stream Overview

```csharp
public interface IVirtualFileStream : IDisposable
{
    long Position { get; set; }

    long Length { get; }

    void Flush();

    long Seek(long offset, SeekOrigin origin);

    void SetLength(long value);

    int Read(byte[] buffer, int offset, int count);

    void Write(byte[] buffer, int offset, int count);

    string ReadAllText();

    byte[] ReadAllBytes();
}
```

The operations available for file streams are largely similar to those of `FileStream`, encompassing:
* Reading bytes
* Writing bytes
* Reading entire text content
* Reading all bytes
* Obtaining the length of the file stream
* Adjusting the length of the file stream
* Setting the current pointer position within the file stream
* Refreshing the buffer

### File System Manager

As implied by its name, the file system manager is responsible for overseeing all file systems. The available interfaces are as follows:

```csharp
public interface IFileSystemManager : IBaseObject
{
    /// <summary>
    ///     Add file system by path.
    /// </summary>
    /// <param name="vfsPath"></param>
    /// <returns></returns>
    IVirtualFileSystem AddFileSystem(VFSPath vfsPath);

    /// <summary>
    ///     Add file system.
    /// </summary>
    /// <param name="virtualFileSystem"></param>
    void AddFileSystem(IVirtualFileSystem virtualFileSystem);

    /// <summary>
    ///     Remove file systems.
    /// </summary>
    /// <param name="virtualFileSystem"></param>
    void RemoveFileSystem(IVirtualFileSystem virtualFileSystem);

    /// <summary>
    ///     Get stream with path from file systems.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    IVirtualFileStream GetStream(string path, FileMode mode = FileMode.Open);

    /// <summary>
    ///     Get readonly file stream async of specified name.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    IVirtualFileStreamRequest GetStreamAsync(string fileName);

    /// <summary>
    ///     Get file system enumerator.
    /// </summary>
    /// <returns></returns>
    IEnumerator<IVirtualFileSystem> GetEnumerator();

    /// <summary>
    ///     Read all text from path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    string ReadAllText(string path);

    /// <summary>
    ///     Read all bytes from path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    byte[] ReadAllBytes(string path);

    /// <summary>
    ///     Read all text from path async.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>ITextAsyncRequest</returns>
    ITextAsyncRequest ReadAllTextAsync(string path);

    /// <summary>
    ///     Read all bytes from path async.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>IBytesAsyncRequest</returns>
    IBytesAsyncRequest ReadAllBytesAsync(string path);
}
```

The primary features are:
* Adding file systems
* Removing file systems
* Accessing file streams synchronously
* Accessing file streams asynchronously
* Reading the entire text content of a file synchronously
* Reading the entire text content of a file asynchronously
* Reading the entire byte content of a file synchronously
* Reading the entire byte content of a file asynchronously
* Traversing all file systems

### How to Use

#### Using the File System Manager

To begin with, you must create a file system manager, which can be achieved using the following method:

```csharp
var fileSystemManager = new FileSystemManager();
fileSystemManager.Create();
```

To add a file system to the manager:
```csharp
// Add standard file system
var dir = VFSPath.Create(YourDirPath).AsDirectory();
var stdFileSystem = fileSystemManager.AddFileSystem(dir);

// Add package file system
var pkg = VFSPath.Create(YourVPKFilePath);
var pkgFileSystem = fileSystemManager.AddFileSystem(pkg);
```

To retrieve a file stream, you provide the relative path of the file. The manager will **search through the file systems sequentially, based on the order they were added**. If a file system includes the file at the specified path, it will create and return the appropriate stream.

```csharp
var relativePath = VFSPath.Create(YourFilePath);

// To obtain a file stream
// For synchronous retrieval
var stream = fileSystemManager.GetStream(relativePath);

// For asynchronous retrieval
var streamAsync = fileSystemManager.GetStreamAsync(relativePath);
yield return streamAsync;

// Alternatively, you can read all text or bytes in one go
// For synchronous reading
var text = fileSystemManager.ReadAllText(relativePath);
var bytes = fileSystemManager.ReadAllBytes(relativePath);

// For asynchronous reading
var textAsync = fileSystemManager.ReadAllTextAsync(relativePath);
yield return textAsync;
var bytesAsync = fileSystemManager.ReadAllBytesAsync(relativePath);
yield return bytesAsync;
```

To dispose of the file system manager
```csharp
fileSystemManager.Destroy();
```

#### How to Create Package Files

You have two options for creating package files (vpk):

1. Utilize the `PackageVirtualFileSystem.CreatePackage` method.

```csharp
var packageSystem = PackageVirtualFileSystem.CreatePackage(outputPath);
packageSystem.OnProgress += onProgress;
packageSystem.AddStream(relativePath1, fileStream1, encryptType1, encryptKey1, compressType1);
packageSystem.AddStream(relativePath2, fileStream2, encryptType2, encryptKey2, compressType2);
...
packageSystem.Flush(true);
packageSystem.Close(true);
```

2. Make use of the `PackageVirtualFileOperator`.

For bulk processing, when you want to apply the same compression and encryption settings to every file in a directory while creating a package file, it's best to use `PackageVirtualFileOperator`.

```csharp
PackageVirtualFileOperator.CreatePackage(YourInputDirectoryPath, YourVPKOutputPath, true);
```

## vFrame.VFS.UnityExtension

Due to the fact that on Android, you can't directly access files in `StreamingAssets` with the `File` class after the app installation, you must use `UnityWebRequest` for asynchronous reading. To solve this, the extension package is provided for compatibility purposes.

### How to Use

The process is the same as with `vFrame.VFS`. The only change is to use the `FileSystemManager` from this extension instead of the one from the original package.

```csharp
namespace vFrame.VFS.UnityExtension
{
    public class FileSystemManager : VFS.FileSystemManager { }
}
```

For example:
```csharp
_fileSystemManager = new vFrame.VFS.UnityExtension.FileSystemManager(); // Pay attention to the namespace
_fileSystemManager.Create();
_fileSystemManager.AddFileSystem(PathUtils.Combine(Application.persistentDataPath, "patch-v1.1.vpk"));
_fileSystemManager.AddFileSystem(PathUtils.Combine(Application.persistentDataPath, "patch-v1.2.vpk"));
_fileSystemManager.AddFileSystem(PathUtils.Combine(Application.streamingAssetsPath, "bundle.vpk"));
_fileSystemManager.AddFileSystem(PathUtils.Combine(Application.streamingAssetsPath, "data.vpk"));
... // Additional file systems
```

## License

[Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)