[English](README.md) | [简体中文](README.zh-CN.md)

# vFrame VFS

A virtual file system for Unity and .NET projects that unifies directory-based storage and `.vpk` package-based storage behind one API.

## Features

- Exposes unified `IFileSystemManager` and `IVirtualFileSystem` abstractions
- Uses `FileSystemManager.AddFileSystem(VFSPath)` to auto-detect directories and `.vpk` packages
- Supports synchronous reads with `GetStream()`, `ReadAllText()`, and `ReadAllBytes()`
- Supports asynchronous reads with `GetStreamAsync()`, `ReadAllTextAsync()`, and `ReadAllBytesAsync()`
- Includes `PackageVirtualFileSystem` for creating, writing, and reading `.vpk` files
- Includes `PackageVirtualFileOperator.CreatePackage()` and `ExtractPackage()` for batch workflows
- Includes `VFSPath` for normalization, combination, and relative-path handling
- Ships a Unity extension for Android `StreamingAssets` access
- Splits runtime code into `vFrame.VFS` and `vFrame.VFS.UnityExtension` assemblies

## Requirements

- Unity `2019.4.40f1` or a compatible `2019.4+` release
- `vFrame.Core` dependency
- `vFrame.Core.Unity` dependency for the Unity extension package

## Installation

This repository contains two UPM packages:

- `com.vyronlee.vframe.vfs` `1.0.1`
- `com.vyronlee.vframe.vfs.unity-extension` `1.0.1`

Add the dependencies to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.vyronlee.vframe.core": "https://github.com/VyronLee/vFrame.Core.git#upm-core",
    "com.vyronlee.vframe.vfs": "https://github.com/VyronLee/vFrame.VFS.git#upm-vfs",
    "com.vyronlee.vframe.vfs.unity-extension": "https://github.com/VyronLee/vFrame.VFS.git#upm-vfs-unity"
  }
}
```

You can also install them through Unity Package Manager with `Window > Package Manager > Add package from git URL...`.

## Quick Start

### Mount a directory and read text

```csharp
using vFrame.VFS;

var manager = new FileSystemManager();
manager.Create();

var root = VFSPath.Create(UnityEngine.Application.persistentDataPath).AsDirectory();
manager.AddFileSystem(root);

var text = manager.ReadAllText("config/game.json");

manager.Destroy();
```

### Mount a `.vpk` package and read content

```csharp
using vFrame.VFS;

var manager = new FileSystemManager();
manager.Create();

manager.AddFileSystem(VFSPath.Create(UnityEngine.Application.streamingAssetsPath + "/data.vpk"));

using (var stream = manager.GetStream("config/game.json")) {
    var json = stream.ReadAllText();
}

manager.Destroy();
```

## Usage

### Scenario 1: Work with multiple file systems

`FileSystemManager` checks mounted file systems in insertion order. The first match wins.

```csharp
using vFrame.VFS;

var manager = new FileSystemManager();
manager.Create();

manager.AddFileSystem(VFSPath.Create(UnityEngine.Application.persistentDataPath).AsDirectory());
manager.AddFileSystem(VFSPath.Create(UnityEngine.Application.streamingAssetsPath + "/base.vpk"));

var bytes = manager.ReadAllBytes("bundles/ui/panel.prefab");

manager.Destroy();
```

### Scenario 2: Build paths with `VFSPath`

```csharp
using vFrame.VFS;

var root = VFSPath.Create("D:/GameData").AsDirectory();
var file = VFSPath.Create("config/settings.json");
var full = root.Combine(file);

var fileName = file.GetFileName();
var extension = file.GetExtension();
var directory = full.GetDirectory();
var relative = full.GetRelative(root);
```

### Scenario 3: Create a `.vpk` package

```csharp
using System.IO;
using vFrame.VFS;

var package = PackageVirtualFileSystem.CreatePackage("D:/Build/data.vpk");

using (var input = File.OpenRead("D:/Build/config/game.json")) {
    package.AddStream(
        "config/game.json",
        input,
        BlockFlags.BlockEncryptXor,
        PackageFileSystemConst.Id,
        BlockFlags.BlockCompressLZMA);
}

package.Flush(true);
package.Close();
```

### Scenario 4: Pack and extract a directory

```csharp
using vFrame.VFS;

PackageVirtualFileOperator.CreatePackage(
    "D:/Build/RawData",
    "D:/Build/content.vpk",
    force: true);

PackageVirtualFileOperator.ExtractPackage(
    "D:/Build/content.vpk",
    "D:/Build/Extracted",
    force: true);
```

### Scenario 5: Read files asynchronously

```csharp
using vFrame.VFS;

var manager = new FileSystemManager();
manager.Create();
manager.AddFileSystem(VFSPath.Create("D:/GameData").AsDirectory());

var request = manager.ReadAllTextAsync("config/game.json");
while (!request.IsDone) {
}

var text = request.Result;
manager.Destroy();
```

## Architecture Overview

### Directory layout

- `Assets/vFrame.VFS/Runtime/Constants`
- `Assets/vFrame.VFS/Runtime/Exceptions`
- `Assets/vFrame.VFS/Runtime/Package`
- `Assets/vFrame.VFS/Runtime/Pools`
- `Assets/vFrame.VFS/Runtime/Standard`
- `Assets/vFrame.VFS.UnityExtension/Runtime/3rd`
- `Assets/vFrame.VFS.UnityExtension/Runtime/StreamingAssets`

### Runtime modules

- `vFrame.VFS`: core runtime with `FileSystemManager`, `VFSPath`, `PackageVirtualFileSystem`, and `PackageVirtualFileOperator`
- `vFrame.VFS.UnityExtension`: Unity-specific runtime with `vFrame.VFS.UnityExtension.FileSystemManager`, `SAStandardVirtualFileSystem`, and `SAPackageVirtualFileSystem`

### File system model

- Standard file system: handled by `StandardVirtualFileSystem` for directories
- Package file system: handled by `PackageVirtualFileSystem` for `.vpk` files
- Android `StreamingAssets`: handled by `SAStandardVirtualFileSystem` and `SAPackageVirtualFileSystem`

## Notes

- `PackageVirtualFileSystem` reads existing package data in read-only mode and only supports `FileMode.Open` with `FileAccess.Read`
- `FileSystemManager.ReadAllText()` returns an empty string when no file is found, and `ReadAllBytes()` returns `null`
- `PackageVirtualFileOperator.CreatePackage()` defaults to `BlockFlags.BlockEncryptXor`, `PackageFileSystemConst.Id`, and `BlockFlags.BlockCompressLZMA`
- On Android, use `vFrame.VFS.UnityExtension.FileSystemManager` for `StreamingAssets`; do not use `vFrame.VFS.FileSystemManager`
- `SAStandardVirtualFileSystem.GetFiles()` currently throws `NotSupportedException`, so Android `StreamingAssets` enumeration is not supported

## License

This project is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
