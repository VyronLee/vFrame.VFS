# vFrame.VFS

Virtual file system for Unity — unified API for directories and `.vpk` packages.

## Purpose

Provides `FileSystemManager` that abstracts directory-based storage (OS files) and `.vpk` package-based storage behind one API. Supports synchronous/asynchronous reads, VPK package creation, compression (LZMA/LZ4/ZSTD/ZLIB), and encryption (XOR/AES).

## Assemblies & Structure

| Assembly | Purpose | Dependencies |
|----------|---------|--------------|
| `vFrame.VFS` | Core runtime | `vFrame.Core` |
| `vFrame.VFS.UnityExtension` | Unity-specific (Android StreamingAssets) | `vFrame.VFS`, `vFrame.Core`, `vFrame.Core.Unity` |

**Code layout:**
- `Assets/vFrame.VFS/Runtime/` — core VFS, VPK format, `FileSystemManager`, `VFSPath`, `PackageVirtualFileSystem`, `PackageVirtualFileOperator`
- `Assets/vFrame.VFS.UnityExtension/Runtime/` — `FileSystemManager` subclass for Android, `SAStandardVirtualFileSystem`, `SAPackageVirtualFileSystem`, `BetterStreamingAssets` (3rd)

## VPK Format & Mounting

**VPK structure:** `[Header][BlockTable][FileList][BlockData...]`
- Header: 64 bytes (Id, Version, offsets, sizes)
- BlockTable: array of `PackageBlockInfo` (40 bytes each: Flags, Offset, OriginalSize, CompressedSize, EncryptKey)
- FileList: null-terminated UTF-8 paths
- BlockData: compressed/encrypted file contents

**Mount file systems:**
```csharp
var manager = new FileSystemManager();
manager.Create();

// Mount directory
manager.AddFileSystem(VFSPath.Create(path).AsDirectory());

// Mount .vpk package
manager.AddFileSystem(VFSPath.Create(path + ".vpk"));

// Read (first match wins, insertion order)
var text = manager.ReadAllText("config/game.json");
var bytes = manager.ReadAllBytes("assets/bundle.prefab");
using var stream = manager.GetStream("data/file.bin");

manager.Destroy();
```

## Compression & Encryption

Defined in `BlockFlags`:

| Compression | Flag | Encryption | Flag |
|-------------|------|------------|------|
| LZMA (default) | `BlockCompressLZMA` | XOR (default) | `BlockEncryptXor` |
| LZ4 | `BlockCompressLZ4` | AES | `BlockEncryptAes` |
| ZSTD | `BlockCompressZSTD` | — | — |
| ZLIB | `BlockCompressZLIB` | — | — |

## Key API

### FileSystemManager
- `Create()` / `Destroy()` — lifecycle (required before use)
- `AddFileSystem(VFSPath)` — mount directory or `.vpk` (auto-detects by extension)
- `GetStream()`, `ReadAllText()`, `ReadAllBytes()` — sync read
- `GetStreamAsync()`, `ReadAllTextAsync()`, `ReadAllBytesAsync()` — async read (poll `IsDone`)

### PackageVirtualFileSystem (VPK creation)
```csharp
var package = PackageVirtualFileSystem.CreatePackage("output.vpk");
using var input = File.OpenRead("source/file.bin");
package.AddStream(
    "internal/path/file.bin",
    input,
    BlockFlags.BlockEncryptXor,
    PackageFileSystemConst.Id,
    BlockFlags.BlockCompressLZMA
);
package.Flush(true);
package.Close();
```

### PackageVirtualFileOperator (batch operations)
```csharp
// Pack directory → .vpk
PackageVirtualFileOperator.CreatePackage(sourceDir, outputVpk, force: true);

// Extract .vpk → directory
PackageVirtualFileOperator.ExtractPackage(sourceVpk, destDir, force: true);
```

### VFSPath
- `VFSPath.Create("path")` — absolute or relative
- `.AsDirectory()` — marks as directory mount point
- `.Combine(other)` — path concatenation
- `.GetFileName()`, `.GetExtension()`, `.GetDirectory()`, `.GetRelative(root)` — utilities

## Build & Test

**Compilation:**
```powershell
# Core
dotnet build "D:/Workspace/vFrame/vFrame.VFS/vFrame.VFS.csproj" --no-restore

# UnityExtension
dotnet build "D:/Workspace/vFrame/vFrame.VFS/vFrame.VFS.UnityExtension.csproj" --no-restore
```

**No test assemblies** — VFS has no unit tests in this repo.

## Gotchas

- **Android StreamingAssets:** Use `vFrame.VFS.UnityExtension.FileSystemManager` instead of `vFrame.VFS.FileSystemManager`. The UnityExtension wraps `StreamingAssets` access via UnityWebRequest.
- **Read-only packages:** `PackageVirtualFileSystem` only supports `FileMode.Open` + `FileAccess.Read`.
- **Missing file returns:** `ReadAllText()` returns empty string, `ReadAllBytes()` returns `null`.
- **File enumeration on Android:** `SAStandardVirtualFileSystem.GetFiles()` throws `NotSupportedException` — not supported.
- **Editormode VPK packing:** Works since 2026-07-05 (de-pooled from ObjectPool). Do NOT re-add pooling — `BaseObject` terminal lifecycle forbids reuse.

**Cross-package conventions (workspace-root CLAUDE.md):**
- Coding standards, testing, compilation verification, namespace conflicts (ILogger), lifecycle semantics

## How it's Consumed

- `vFrame.Bundler.VFSAdapter` — loads AssetBundles from VFS packages
- `vFrame.Demo.FlappyBird` — uses VFS for resource loading (Android StreamingAssets via UnityExtension)
