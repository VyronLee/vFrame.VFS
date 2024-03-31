# vFrame 虚文件系统

![vFrame](https://img.shields.io/badge/vFrame-VFS-blue) [![Made with Unity](https://img.shields.io/badge/Made%20with-Unity-57b9d3.svg?style=flat&logo=unity)](https://unity3d.com) [![License](https://img.shields.io/badge/License-Apache%202.0-brightgreen.svg)](#License)

**虚文件系统**（VFS）是游戏中的一个抽象层，主要为了使得开发者能够以统一的方式操作游戏内的文件和目录。

## 目录

* [特点](#特点)
* [常规用途](#常规用途)
* [结构](#结构)
* [安装](#安装)
* [vFrame.Core](#vframecore)
    + [虚文件系统概述](#虚文件系统概述)
    + [文件流概述](#文件流概述)
    + [文件系统管理器](#文件系统管理器)
    + [使用方式](#使用方式)
        - [文件系统管理器的使用](#文件系统管理器的使用)
        - [包文件的创建](#包文件的创建)
* [vFrame.Core.UnityExtension](#vframecoreunityextension)
    + [使用方式](#使用方式)
* [License](#license)

## 特点

vFrame.VFS 基于虚文件系统的设计理念，整合并实现了以下功能：
1. 支持以指定的一个“文件夹”为单位构造文件系统
2. 以“文件包”的方式设计一套独有的“包文件系统”，能够在不释放包内文件的情况下直接操作，并且支持设置包内单个文件的压缩以及加密方式
3. 支持文件系统管理器，添加到管理器中的所有文件系统共享文件寻址方式
4. 支持 Android 平台下直接操作 StreamingAssets 内文件，无需使用异步方式（UnityWebRequest）读取

## 常规用途
1. 可用于整合游戏内所有 AssetBundle 为一个虚文件包（ VPK ），避免分发时零散地出现在包体内，增加破译难度
2. 可用于整合游戏内所有配置性文本或者数据文件，不必打进 AssetBundle 中即可直接操作，并进行压缩加密
3. 可用于制作游戏补丁，所有需要更新的文件整合进一个虚文件包中直接进行分发，下载后按照一定顺序添加到虚文件系统管理器即可生效
4. 可配合 `vFrame.Bundler` 进行 AssetBundle 的加载，无需事先释放文件

## 结构

该仓库拆分为两个独立的 Package，分为:
* `vFrame.VFS` 与 Unity 完全解耦，可在 Unity 之外的任意地方使用
* `vFrame.VFS.UnityExtension` 为 Unity 下的扩展，支持对 Android 平台下的特殊操作

## 安装

建议使用 Unity Package Manager 安装，添加以下路径：
* `vFrame.VFS` https://github.com/VyronLee/vFrame.VFS.git#upm-vfs
* `vFrame.VFS.UnityExtension` https://github.com/VyronLee/vFrame.VFS.git#upm-vfs-unity

如需指定版本，链接后面带上版本号即可

## vFrame.Core

### 虚文件系统概述

虚文件系统分为两大类：

* **标准文件系统** 以文件夹为单位构建的一个文件系统，该系统内可操作的文件均为此文件夹内的文件

* **包文件系统** 专门设计的独有的文件系统，可在不释放包内文件的前提下直接操作包内文件

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

上述接口为虚文件系统支持的操作，包括有：
* 打开文件系统（必须）
* 关闭文件系统
* 检查指定路径文件（相对路径）是否存在
* 获取文件系统内所有文件路径
* 获取文件流（同步）
* 获取文件流（异步）

### 文件流概述

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

文件流可操作的接口跟`FileStream`基本一致，包括有：
* 读取字节数据
* 写入字节数据
* 读取所有文本
* 读取所有字节
* 获取文件流长度
* 设置文件流长度
* 设置文件流当前位置
* 刷新缓冲区

### 文件系统管理器

文件系统管理器，顾名思义就是管理所有的文件系统。接口如下：

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

主要功能有：
* 添加文件系统
* 移除文件系统
* 获取文件流(同步)
* 获取文件流(异步)
* 读取文件所有文本数据(同步)
* 读取文件所有文本数据(异步)
* 读取文件所有字节数据(同步)
* 读取文件所有字节数据(异步)
* 遍历所有文件系统

### 使用方式

#### 文件系统管理器的使用

首先需要创建一个文件系统管理器，可使用如下方式：
```csharp
var fileSystemManager = new FileSystemManager();
fileSystemManager.Create();
```

往管理器中添加文件系统：
```csharp
// 添加标准文件系统
var dir = VFSPath.Create(YourDirPath).AsDirectory();
var stdFileSystem = fileSystemManager.AddFileSystem(dir);

// 添加包文件系统
var pkg = VFSPath.Create(YourVPKFilePath);
var pkgFileSystem = fileSystemManager.AddFileSystem(pkg);
```

获取文件流时传入文件的相对路径，管理器会**根据文件系统添加的顺序依次进行查询**，如果文件系统中包含有该路径的文件信息，则创建并返回对应的流
```csharp
var relativePath = VFSPath.Create(YourFilePath);

// 获取文件流
// 同步方式
var stream = fileSystemManager.GetStream(relativePath);

// 异步方式
var streamAsync = fileSystemManager.GetStreamAsync(relativePath);
yield return streamAsync;

// 也可以一次性直接读取所有文本或字节数据
// 同步方式
var text = fileSystemManager.ReadAllText(relativePath);
var bytes = fileSystemManager.ReadAllBytes(relativePath);

// 异步方式
var textAsync = fileSystemManager.ReadAllTextAsync(relativePath);
yield return textAsync;
var bytesAsync = fileSystemManager.ReadAllBytesAsync(relativePath);
yield return bytesAsync;
```

销毁文件系统管理器
```csharp
fileSystemManager.Destroy();
```

#### 包文件的创建

包文件（vpk）的创建有两种方式

1. 使用`PackageVirtualFileSystem.CreatePackage`

```csharp
var packageSystem = PackageVirtualFileSystem.CreatePackage(outputPath);
packageSystem.OnProgress += onProgress;
packageSystem.AddStream(relativePath1, fileStream1, encryptType1, encryptKey1, compressType1);
packageSystem.AddStream(relativePath2, fileStream2, encryptType2, encryptKey2, compressType2);
...
packageSystem.Flush(true);
packageSystem.Close(true);
```

2. 使用`PackageVirtualFileOperator`

如果需要批量操作，对一个文件夹内所有文件都使用相同的压缩以及加密方式创建包文件，可直接使用`PackageVirtualFileOperator`
```csharp
PackageVirtualFileOperator.CreatePackage(YourInputDirectoryPath, YourVPKOutputPath, true);
```

## vFrame.Core.UnityExtension

由于安卓平台下，App安装后是无法直接通过`File`来操作`StreamingAssets`下文件的，必须使用`UnityWebRequest`的方式来异步读取。为了解决这个问题，引入了该扩展包进行兼容处理。

### 使用方式

使用上跟`vFrame.VFS`没有任何区别，只需要把运行时创建的`vFrame.VFS.FileSystemManager`改为下面这个即可

```csharp
namespace vFrame.VFS.UnityExtension
{
    public class FileSystemManager : VFS.FileSystemManager { }
}
```

示例：
```csharp
_fileSystemManager = new vFrame.VFS.UnityExtension.FileSystemManager(); // 注意命名空间
_fileSystemManager.Create();
_fileSystemManager.AddFileSystem(PathUtils.Combine(Application.persistentDataPath, "patch-v1.1.vpk");
_fileSystemManager.AddFileSystem(PathUtils.Combine(Application.persistentDataPath, "patch-v1.2.vpk");
_fileSystemManager.AddFileSystem(PathUtils.Combine(Application.streamingAssetsPath, "bundle.vpk");
_fileSystemManager.AddFileSystem(PathUtils.Combine(Application.streamingAssetsPath, "data.vpk");
... // Other file systems
```

## License

[Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)