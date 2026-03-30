# EpisodeMatcher GUI Deployment Guide

## The Problem: Manual File Copying

Copying only the `.exe` and a few DLL files (even with `libHarfBuzzSharp`, `av_libglesv2`, `libSkiaSharp`) will **not work**. The GUI requires:

1. **Managed .NET assemblies** (43 DLL files)
2. **Platform-specific native binaries** (3 DLLs in `runtimes/` folder)
3. **Configuration files** (`.deps.json`, `.runtimeconfig.json`)

## Recommended Approach: Self-Contained Single-File Publish

**This is the easiest way** — it bundles everything into a single `.exe`:

```bash
dotnet publish EpisodeMatcherGui -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```

Result: `publish/EpisodeMatcherGui.exe` (~100 MB) — ready to copy and run anywhere.

**For Linux:**
```bash
dotnet publish EpisodeMatcherGui -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```

> **Note:** On Linux, the GUI requires `libvlc` to be installed on the system for video preview:
> ```bash
> sudo apt install libvlc-dev   # Debian/Ubuntu
> sudo dnf install vlc-libs     # Fedora
> ```

Result: `publish/EpisodeMatcherGui` — executable binary.

## If You Must Copy Files Manually

Copy the **entire output folder structure** from the build output, **not** individual files:

### Folder Layout
```
EpisodeMatcherGui/
├── EpisodeMatcherGui.exe
├── EpisodeMatcherGui.dll
├── EpisodeMatcherGui.runtimeconfig.json
├── EpisodeMatcherGui.deps.json
├── EpisodeMatcherCore.dll
├── Avalonia.*.dll                    (23 Avalonia DLLs)
├── HarfBuzzSharp.dll
├── SkiaSharp.dll
├── F23.StringSimilarity.dll
├── System.IO.Pipelines.dll
├── Tmds.DBus.Protocol.dll
├── MicroCom.Runtime.dll
├── runtimes/
│   └── win-x64/
│       └── native/
│           ├── av_libglesv2.dll      ⚠️ Required (OpenGL renderer)
│           ├── libHarfBuzzSharp.dll  ⚠️ Required (text shaping)
│           └── libSkiaSharp.dll      ⚠️ Required (graphics engine)
```

### Complete File List (43 Assemblies Required)

**Avalonia Framework (23 DLLs):**
- Avalonia.Base.dll
- Avalonia.Controls.dll
- Avalonia.Controls.ColorPicker.dll
- Avalonia.DesignerSupport.dll
- Avalonia.Desktop.dll
- Avalonia.Dialogs.dll
- Avalonia.dll
- Avalonia.Fonts.Inter.dll
- Avalonia.FreeDesktop.dll
- Avalonia.Markup.dll
- Avalonia.Markup.Xaml.dll
- Avalonia.Metal.dll
- Avalonia.MicroCom.dll
- Avalonia.Native.dll
- Avalonia.OpenGL.dll
- Avalonia.Remote.Protocol.dll
- Avalonia.Skia.dll
- Avalonia.Themes.Fluent.dll
- Avalonia.Themes.Simple.dll
- Avalonia.Vulkan.dll
- Avalonia.Win32.dll
- Avalonia.Win32.Automation.dll
- Avalonia.X11.dll

**Graphics & Typography:**
- SkiaSharp.dll
- HarfBuzzSharp.dll

**Application:**
- EpisodeMatcherGui.dll
- EpisodeMatcherCore.dll
- F23.StringSimilarity.dll

**System & Utilities:**
- MicroCom.Runtime.dll
- System.IO.Pipelines.dll
- Tmds.DBus.Protocol.dll

**Configuration:**
- EpisodeMatcherGui.runtimeconfig.json
- EpisodeMatcherGui.deps.json

**Native Binaries (runtimes/win-x64/native/):**
- av_libglesv2.dll (ANGLE OpenGL ES 2.0 renderer)
- libHarfBuzzSharp.dll (Text layout & shaping)
- libSkiaSharp.dll (2D rendering backend)

## Why It Fails Without `runtimes/` Folder

The `runtimes/` directory is **not optional**. Here's why:

- **av_libglesv2.dll**: Avalonia uses this for GPU-accelerated rendering on Windows
- **libHarfBuzzSharp.dll**: Handles complex text layout (font metrics, ligatures, bidirectional text)
- **libSkiaSharp.dll**: The core 2D graphics engine that renders UI elements

Without these native libraries, the application crashes during Avalonia initialization when it tries to:
1. Initialize the rendering backend
2. Load fonts
3. Create the window

## Deployment Checklist

- [ ] Copy `EpisodeMatcherGui.exe`
- [ ] Copy all 43 `.dll` files (Avalonia, graphics, utilities)
- [ ] Copy `runtimes/win-x64/native/` folder with 3 native DLLs
- [ ] Copy `.runtimeconfig.json` 
- [ ] Copy `.deps.json`
- [ ] Don't copy `.pdb` files (only needed for debugging)
- [ ] Don't copy individual files from `runtimes/` — copy the entire folder structure

## Platform-Specific Distributions

If distributing to multiple platforms, include the appropriate `runtimes/` subdirectory:

- **Windows x64**: `runtimes/win-x64/native/` (3 DLLs)
- **Windows ARM64**: `runtimes/win-arm64/native/` (3 DLLs)
- **Linux x64**: `runtimes/linux-x64/native/` (2 .so files: libHarfBuzzSharp.so, libSkiaSharp.so)
- **macOS**: `runtimes/osx/native/` (3 .dylib files)

## What the Configuration Files Do

**EpisodeMatcherGui.runtimeconfig.json:**
- Specifies .NET version and rollforward behavior
- Required for runtime initialization

**EpisodeMatcherGui.deps.json:**
- Dependency manifest — tells the runtime which DLLs to load
- Specifies platform-specific native binaries
- Critical for finding `runtimes/` folder contents

Both files are generated during build and must be present next to the `.exe`.

## Recommended Distribution Method

**For production use, always use self-contained publish:**

```bash
# Creates a single 100 MB .exe with everything included
dotnet publish EpisodeMatcherGui -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o publish/

# Size: ~100 MB (self-contained with all dependencies)
# Result: One file to distribute — users run it directly
# No .NET installation needed on target machine
```

This approach:
- Eliminates dependency on system .NET installation
- No manual folder structure to maintain
- Single file to distribute and update
