[English](README.md) | [简体中文](README.zh-CN.md)

# vFrame 虚拟文件系统

适用于 Unity 与 .NET 场景的虚拟文件系统，统一管理目录文件系统与 `.vpk` 包文件系统。

## 特性

- 提供统一的 `IFileSystemManager` / `IVirtualFileSystem` 抽象
- 使用 `FileSystemManager.AddFileSystem(VFSPath)` 自动识别目录与 `.vpk`
- 支持同步读取：`GetStream()`、`ReadAllText()`、`ReadAllBytes()`
- 支持异步读取：`GetStreamAsync()`、`ReadAllTextAsync()`、`ReadAllBytesAsync()`
- 提供 `PackageVirtualFileSystem` 直接创建、写入、读取 `.vpk` 文件
- 提供 `PackageVirtualFileOperator.CreatePackage()` / `ExtractPackage()` 批量打包与解包
- 提供 `VFSPath` 统一路径规范化、拼接、相对路径计算能力
- Unity 扩展内置 Android `StreamingAssets` 访问支持
- 运行时拆分为 `vFrame.VFS` 与 `vFrame.VFS.UnityExtension` 两个程序集

## 环境要求

- Unity `2019.4.40f1` 或兼容的 `2019.4+`
- 依赖 `vFrame.Core`
- Unity 扩展额外依赖 `vFrame.Core.Unity`

## 安装

该仓库包含两个 UPM 包：

- `com.vyronlee.vframe.vfs` `1.0.1`
- `com.vyronlee.vframe.vfs.unity-extension` `1.0.1`

在项目的 `Packages/manifest.json` 中添加依赖：

```json
{
  "dependencies": {
    "com.vyronlee.vframe.core": "https://github.com/VyronLee/vFrame.Core.git#upm-core",
    "com.vyronlee.vframe.vfs": "https://github.com/VyronLee/vFrame.VFS.git#upm-vfs",
    "com.vyronlee.vframe.vfs.unity-extension": "https://github.com/VyronLee/vFrame.VFS.git#upm-vfs-unity"
  }
}
```

也可以通过 Unity Package Manager 执行 `Window > Package Manager > Add package from git URL...` 后分别添加对应 Git URL。

## 快速开始

### 挂载目录并读取文本

```csharp
using vFrame.VFS;

var manager = new FileSystemManager();
manager.Create();

var root = VFSPath.Create(UnityEngine.Application.persistentDataPath).AsDirectory();
manager.AddFileSystem(root);

var text = manager.ReadAllText("config/game.json");

manager.Destroy();
```

### 挂载 `.vpk` 文件并读取内容

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

## 使用说明

### 场景 1：统一管理多个文件系统

`FileSystemManager` 会按添加顺序遍历文件系统；命中的第一个文件即为读取来源。

```csharp
using vFrame.VFS;

var manager = new FileSystemManager();
manager.Create();

manager.AddFileSystem(VFSPath.Create(UnityEngine.Application.persistentDataPath).AsDirectory());
manager.AddFileSystem(VFSPath.Create(UnityEngine.Application.streamingAssetsPath + "/base.vpk"));

var bytes = manager.ReadAllBytes("bundles/ui/panel.prefab");

manager.Destroy();
```

### 场景 2：使用 `VFSPath` 处理路径

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

### 场景 3：创建 `.vpk` 包文件

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

### 场景 4：批量打包与解包目录

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

### 场景 5：异步读取文件

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

## 架构概览

### 目录结构

- `Assets/vFrame.VFS/Runtime/Constants`
- `Assets/vFrame.VFS/Runtime/Exceptions`
- `Assets/vFrame.VFS/Runtime/Package`
- `Assets/vFrame.VFS/Runtime/Pools`
- `Assets/vFrame.VFS/Runtime/Standard`
- `Assets/vFrame.VFS.UnityExtension/Runtime/3rd`
- `Assets/vFrame.VFS.UnityExtension/Runtime/StreamingAssets`

### 运行时模块

- `vFrame.VFS`：核心运行时，包含 `FileSystemManager`、`VFSPath`、`PackageVirtualFileSystem`、`PackageVirtualFileOperator`
- `vFrame.VFS.UnityExtension`：Unity 扩展运行时，包含 `vFrame.VFS.UnityExtension.FileSystemManager`、`SAStandardVirtualFileSystem`、`SAPackageVirtualFileSystem`

### 文件系统模型

- 标准文件系统：由 `StandardVirtualFileSystem` 处理普通目录
- 包文件系统：由 `PackageVirtualFileSystem` 处理 `.vpk` 文件
- Android StreamingAssets：由 `SAStandardVirtualFileSystem` / `SAPackageVirtualFileSystem` 处理 `StreamingAssets`

## 注意事项

- `PackageVirtualFileSystem` 只支持只读方式访问已存在的 `.vpk` 数据块；读取时仅支持 `FileMode.Open` 与 `FileAccess.Read`
- `FileSystemManager.ReadAllText()` 在找不到文件时返回空字符串，`ReadAllBytes()` 在找不到文件时返回 `null`
- `PackageVirtualFileOperator.CreatePackage()` 默认使用 `BlockFlags.BlockEncryptXor`、`PackageFileSystemConst.Id`、`BlockFlags.BlockCompressLZMA`
- 在 Android 上访问 `StreamingAssets` 时，请使用 `vFrame.VFS.UnityExtension.FileSystemManager`，不要使用 `vFrame.VFS.FileSystemManager`
- `SAStandardVirtualFileSystem.GetFiles()` 当前会抛出 `NotSupportedException`，不适合用来枚举 Android `StreamingAssets`

## License

本项目基于 [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0) 许可协议发布。
