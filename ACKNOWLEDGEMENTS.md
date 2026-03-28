# Acknowledgements

EpisodeMatcher builds upon the excellent work of many open-source projects. This document lists the dependencies and their licenses.

## Direct Dependencies

### Avalonia Framework
- **Version:** 11.3.12
- **License:** MIT
- **Repository:** https://github.com/AvaloniaUI/Avalonia
- **Purpose:** Cross-platform UI framework for the GUI application
- **Copyright:** © AvaloniaUI Contributors

### SkiaSharp
- **Version:** 2.88.x (via Avalonia)
- **License:** MIT
- **Repository:** https://github.com/mono/SkiaSharp
- **Purpose:** 2D graphics rendering engine (native wrapper)
- **Copyright:** © Xamarin, Inc. and Microsoft Corporation

### HarfBuzzSharp
- **Version:** 7.3.x (via Avalonia)
- **License:** MIT
- **Repository:** https://github.com/MicroCom/HarfBuzzSharp
- **Purpose:** OpenType text shaping engine (native wrapper)
- **Copyright:** © HarfBuzzSharp Contributors

### F23.StringSimilarity
- **Version:** 7.0.1
- **License:** MIT
- **Repository:** https://github.com/feature23/StringSimilarity.NET
- **Purpose:** String similarity algorithms (cosine, Jaro-Winkler)
- **Copyright:** © Feature23

### MicroCom.Runtime
- **Version:** 0.11.0
- **License:** MIT
- **Repository:** https://github.com/MicroCom/MicroCom.Runtime
- **Purpose:** COM interop runtime
- **Copyright:** © MicroCom Contributors

### LibVLCSharp
- **Version:** 3.9.6
- **License:** LGPL 3.0
- **Repository:** https://github.com/videolan/vlcsharp
- **Purpose:** .NET bindings for VLC media player
- **Copyright:** © VideoLAN and libvlcsharp contributors

### LibVLCSharp.Avalonia
- **Version:** 3.9.6
- **License:** LGPL 3.0
- **Repository:** https://github.com/videolan/vlcsharp
- **Purpose:** Avalonia control for VLC video playback
- **Copyright:** © VideoLAN and libvlcsharp contributors

## Native Media Libraries

### VideoLAN.LibVLC.Windows
- **Version:** 3.0.23
- **License:** LGPL 2.1 or later
- **Repository:** https://www.videolan.org/
- **Component:** `libvlc.dll`, `libvlccore.dll` (Windows x64/x86/ARM64)
- **Purpose:** Core VLC media player library for video playback
- **Copyright:** © VideoLAN

## Native Graphics Libraries

### ANGLE - Almost Native Graphics Layer Engine
- **License:** BSD 3-Clause
- **Repository:** https://github.com/google/angle
- **Component:** `av_libglesv2.dll` (Windows x64/x86/ARM64)
- **Purpose:** OpenGL ES 2.0 renderer for hardware-accelerated graphics
- **Copyright:** © Google Inc.

### Skia Graphics Library
- **License:** BSD 3-Clause
- **Repository:** https://skia.org/
- **Component:** `libSkiaSharp.dll` (Windows), `libSkiaSharp.so` (Linux), `libSkiaSharp.dylib` (macOS)
- **Purpose:** 2D graphics rendering backend
- **Copyright:** © Google Inc.

### HarfBuzz
- **License:** Old MIT License (BSD-like)
- **Repository:** https://github.com/harfbuzz/harfbuzz
- **Component:** `libHarfBuzzSharp.dll` (Windows), `libHarfBuzzSharp.so` (Linux), `libHarfBuzzSharp.dylib` (macOS)
- **Purpose:** Complex text layout and shaping
- **Copyright:** © HarfBuzz contributors

## System & Utility Libraries

### System.IO.Pipelines
- **License:** MIT
- **Repository:** https://github.com/dotnet/runtime
- **Purpose:** High-performance I/O operations
- **Copyright:** © Microsoft Corporation

### Tmds.DBus.Protocol
- **License:** MIT
- **Repository:** https://github.com/tmds/Tmds.DBus
- **Purpose:** D-Bus protocol support (Linux integration)
- **Copyright:** © Tom Deseyn

### Avalonia.Remote.Protocol
- **License:** MIT
- **Repository:** https://github.com/AvaloniaUI/Avalonia
- **Purpose:** Remote debugging protocol
- **Copyright:** © AvaloniaUI Contributors

## External Tools (Runtime Dependencies)

The following tools must be installed on the system to use EpisodeMatcher:

### FFmpeg
- **License:** LGPL 2.1 or later
- **Website:** https://ffmpeg.org/
- **Purpose:** Subtitle extraction from video files
- **Note:** User must install separately

### FFprobe
- **License:** LGPL 2.1 or later (part of FFmpeg)
- **Website:** https://ffmpeg.org/ffprobe.html
- **Purpose:** Video stream probing
- **Note:** User must install separately

### Tesseract OCR
- **License:** Apache 2.0
- **Repository:** https://github.com/UB-Mannheim/tesseract
- **Purpose:** Optical character recognition for bitmap subtitles
- **Note:** Optional; user must install separately if needed

### MKVToolNix
- **License:** GPL 2.0
- **Website:** https://www.bunkus.org/videotools/mkvtoolnix/
- **Purpose:** MKV subtitle extraction
- **Note:** Optional; user must install separately

## License Compatibility

All bundled libraries in the compiled binaries include:

- ✅ **MIT License:** Most core libraries (Avalonia, SkiaSharp, HarfBuzzSharp, F23.StringSimilarity, etc.)
- ✅ **BSD 3-Clause:** ANGLE and Skia graphics libraries
- ⚠️ **LGPL 2.1/3.0:** VLC media libraries (libvlc, LibVLCSharp)
- ⚠️ **Runtime Tools:** FFmpeg (LGPL), Tesseract (Apache 2.0), MKVToolNix (GPL)

**Important:** EpisodeMatcher includes LibVLC (LGPL 2.1+) for video playback. The application is distributed under the MIT license. Users have the right to:
- Use the application freely
- Modify the source code (excluding LibVLC binaries)
- Request modified versions under LGPL 2.1 terms for the LibVLC portions

For full compliance details, see https://www.videolan.org/legal.html

**Note on External Tools:** FFmpeg, Tesseract, and MKVToolNix are not included in the binary distribution and must be installed separately by users. Users are responsible for complying with those projects' licenses when installing and using those tools.

## Attribution Requirements

When distributing EpisodeMatcher binaries, please include:

1. A copy of `LICENSE` (this project's MIT license)
2. This `ACKNOWLEDGEMENTS.md` file
3. License files and notices from:
   - Avalonia: https://github.com/AvaloniaUI/Avalonia/blob/master/LICENSE
   - SkiaSharp: https://github.com/mono/SkiaSharp/blob/main/LICENSE.txt
   - ANGLE: https://github.com/google/angle/blob/main/LICENSE
   - Skia: https://skia.googlesource.com/skia/+/master/LICENSE
   - LibVLC: https://www.videolan.org/legal.html
   - LibVLCSharp: https://github.com/videolan/vlcsharp/blob/master/LICENSE

---

**Last Updated:** 2026-03-28 (Added LibVLCSharp and VideoLAN.LibVLC for video playback)

For the most up-to-date information on dependency licenses, check the NuGet package pages and source repositories.
