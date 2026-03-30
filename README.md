# EpisodeMatcher

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![GitHub](https://img.shields.io/badge/GitHub-EpisodeMatcher-blue)](https://github.com/yourusername/EpisodeMatcher)

Automatically rename ripped TV episode files by matching their embedded subtitles against a folder of correctly-named reference SRT files.

**Available as both a cross-platform GUI and command-line tool.**

---

## Features

✅ **Dual Interface**
- Desktop GUI (Avalonia, Windows/Linux/macOS)
- Command-line tool (for scripting and batch use)

✅ **Smart Matching**
- Bigram cosine similarity + Jaro-Winkler hybrid scoring
- Handles ambiguous matches with confidence indicators
- Shows top candidate episodes with scores
- Text snippet preview on hover (GUI)

✅ **Flexible Subtitle Support**
- Text subtitles: `subrip`, `ass`, `webvtt`, `mov_text` (direct extraction)
- Bitmap subtitles: DVD & Blu-ray with Tesseract OCR
- ZIP file support (single SRT per archive)

✅ **Safe Operations**
- Dry-run mode to preview all renames before applying
- Configurable similarity thresholds
- Checks for ambiguous matches before renaming
- Original files preserved on error

✅ **Easy Distribution**
- Self-contained executables (no .NET installation needed)
- CLI: 64.92 MB | GUI: 73.78 MB
- Windows x64, Linux x64, macOS support

---

## Quick Start

### GUI (Recommended for beginners)

1. **Install prerequisites:**
   ```bash
   # Windows (choose one)
   winget install Gyan.FFmpeg UB-Mannheim.TesseractOCR
   choco install ffmpeg tesseract

   # Linux
   sudo apt install ffmpeg tesseract-ocr libvlc-dev

   # macOS
   brew install ffmpeg tesseract
   ```

2. **Download and run `EpisodeMatcherGui.exe`**

3. **In the app:**
   - ✅ Check tool indicators (green = found, red = not found)
   - 📁 Set reference SRT folder
   - 📺 Drag & drop video files or click "Add Files…"
   - ⚙️ Adjust settings if needed
   - ▶️ Click "Dry Run" to preview
   - ✏️ Uncheck "Dry Run" and click "Rename Files" to apply

### CLI (For scripting)

```bash
# Dry run first (preview renames)
EpisodeMatcher "D:\Videos\Show S01" "D:\SRTs\Show S01" --dry-run

# Apply renames
EpisodeMatcher "D:\Videos\Show S01" "D:\SRTs\Show S01"

# Verbose output + longer window for difficult discs
EpisodeMatcher "D:\Videos\Show S01" "D:\SRTs\Show S01" --window 60 --min-score 0.10 --verbose
```

---

## How It Works

1. **Extract Subtitles**  
   FFmpeg extracts the subtitle track from the first N seconds (default: 30s) of each video.

2. **Normalize & Score**  
   The extracted text is normalized and compared against all reference SRTs using:
   - **Primary:** Bigram cosine similarity (85% weight)
   - **Tiebreaker:** Jaro-Winkler distance (15% weight)

3. **Match & Rename**  
   The video is renamed to match the highest-scoring reference SRT if the score meets the minimum threshold.

### Scoring

| Score | Result |
|-------|--------|
| ≥ 0.20 | ✅ Confident match — rename proceeds |
| < 0.20 | ❌ Skipped — doesn't clearly match any episode |
| Top 2 within ±0.05 | ⚠️ Ambiguous — use `--force` flag to rename anyway |

---

## Prerequisites

### Required

| Tool | Purpose | Install |
|------|---------|---------|
| **FFmpeg + FFprobe** | Subtitle extraction | [ffmpeg.org](https://ffmpeg.org/download.html) |

### Optional

| Tool | Purpose | When Needed | Install |
|------|---------|-------------|---------|
| **Tesseract OCR** | Bitmap subtitle OCR | DVD/Blu-ray subtitles only | [GitHub](https://github.com/tesseract-ocr/tesseract) |
| **MKVToolNix** | MKV subtitle extraction | For complex MKV files | [bunkus.org](https://www.bunkus.org/videotools/mkvtoolnix/) |

### Installation

**Windows (Winget)**
```bash
winget install Gyan.FFmpeg
winget install UB-Mannheim.TesseractOCR
```

**Windows (Chocolatey)**
```bash
choco install ffmpeg tesseract
```

**Linux (Debian/Ubuntu)**
```bash
sudo apt install ffmpeg tesseract-ocr mkvtoolnix libvlc-dev
```

**macOS (Homebrew)**
```bash
brew install ffmpeg tesseract mkvtoolnix
```

---

## Usage

### GUI

**Video File Panel**
- ✓ Checkbox: Include/exclude file from processing
- Original filename
- Matched episode name (blue)
- Status (green/orange/red with icon)
- Similarity score
- Hover for extracted subtitle snippet

**Settings**
- **Reference SRT Folder:** Path to folder with correctly-named SRT files
- **Threads:** Parallel processing threads (1-16, default: 2)
- **Window (seconds):** How much subtitle text to extract (default: 30s)
- **Min. Score (0-1):** Minimum confidence to accept match (default: 0.20)
- **Dry Run:** Preview mode — doesn't change files

**Workflow**
1. Add video files (drag-and-drop or "Add Files…")
2. Uncheck files you want to skip
3. Click "Dry Run" to preview matches
4. Review the log panel
5. Uncheck "Dry Run" and click "Rename Files" to apply changes

### CLI

```bash
EpisodeMatcher <videos> <srt-folder> [options]
```

**Arguments**
| Argument | Required | Description |
|----------|----------|-------------|
| `<videos>` | Yes | Folder with video files to rename |
| `<srt-folder>` | Yes | Folder with reference SRT files |

**Options**
| Option | Default | Description |
|--------|---------|-------------|
| `--window <seconds>` | 30 | Seconds of subtitle to extract |
| `--min-score <0-1>` | 0.20 | Minimum similarity score |
| `--dry-run` | off | Preview without changing files |
| `--force` | off | Rename even if ambiguous |
| `--verbose` | off | Print extracted subtitle text |

**Examples**

```bash
# Dry run (always recommended first)
EpisodeMatcher "D:\Videos\Season1" "D:\SRTs\Season1" --dry-run

# Apply renames
EpisodeMatcher "D:\Videos\Season1" "D:\SRTs\Season1"

# Lower threshold for difficult discs
EpisodeMatcher "D:\Videos\Season1" "D:\SRTs\Season1" \
  --window 60 --min-score 0.15 --dry-run

# Force rename ambiguous matches
EpisodeMatcher "D:\Videos\Season1" "D:\SRTs\Season1" --force

# See extracted text
EpisodeMatcher "D:\Videos\Season1" "D:\SRTs\Season1" --verbose --dry-run
```

---

## Preparing Reference SRT Files

Reference SRT files must be **correctly named** with your desired episode names:

```
S01E01 - Pilot.srt
S01E02 - The One with the Dog.srt
S01E03 - The One Where Monica Gets a Roommate.srt
```

Videos will be renamed to match, preserving their original file extension:

```
unknown-episode-1.mkv → S01E01 - Pilot.mkv
unknown-episode-2.mkv → S01E02 - The One with the Dog.mkv
unknown-episode-3.mkv → S01E03 - The One Where Monica Gets a Roommate.mkv
```

**ZIP Support:** Single SRT files can be stored in ZIP archives. The episode name is extracted from the SRT filename inside.

**SRT Source:** [OpenSubtitles.org](https://www.opensubtitles.org/) has a large database of correctly-named subtitles.

---

## Subtitle Type Support

| Type | Codec | Status | Notes |
|------|-------|--------|-------|
| **Text (Soft)** | subrip, ass, webvtt, mov_text | ✅ Works | Direct extraction via FFmpeg |
| **DVD Bitmap** | dvd_subtitle, dvdsub | ✅ Works | Requires Tesseract OCR |
| **Blu-ray PGS** | hdmv_pgs_subtitle | ✅ Works | Requires Tesseract OCR |
| **No Subtitles** | — | ⏭️ Skipped | File is skipped with warning |

### Extracting Subtitles from Complex MKV Files

If you have MKV files with embedded subtitles but standard extraction fails:

```bash
# Use mkvextract to list subtitle tracks
mkvextract "video.mkv" tracks

# Extract specific track (e.g., track 2)
mkvextract "video.mkv" tracks 2:"extracted.srt"
```

---

## Troubleshooting

### "Score always 0 / no matches found"

**Symptom:** Every file shows "No match (best score=0%)"

**Causes & Solutions:**
- Subtitles are bitmap-only and Tesseract is not installed
  - Run with `--verbose` to see what text (if any) was extracted
  - Install Tesseract: `winget install UB-Mannheim.TesseractOCR`
- Reference SRTs are from a different release (timing differs significantly)
  - Verify SRTs match your video release
- Videos have no subtitle streams
  - Check with FFprobe: `ffprobe -select_streams s "video.mkv"`

### "Wrong episode matched"

**Symptom:** Files are matched to wrong episodes

**Solutions:**
- Increase sample window: `--window 60` or `--window 90`
- Verify reference SRTs are from the same release as your rips
- Timing differences in the first 30 seconds can matter
- Check subtitle codec: `ffprobe -select_streams s "video.mkv"`

### "FFmpeg not found on Windows"

**Symptom:** "ffmpeg not found. Please install it and ensure it is on your PATH"

**Solutions:**
1. **Add to PATH:**
   - Win+X → System → Advanced system settings → Environment Variables
   - Add `C:\ffmpeg\bin` to Path
   - Restart the app

2. **Install with package manager:**
   ```bash
   winget install Gyan.FFmpeg
   ```

3. **Place in same folder as EpisodeMatcher.exe:**
   - Copy `ffmpeg.exe`, `ffprobe.exe` to the app folder

### "GUI won't launch"

**Symptom:** GUI closes silently with no error

**Solutions:**
- Ensure .NET 8 runtime is installed
- Check for `crash.log` in the app folder
- Try the CLI instead to test your setup

### GUI shows "Tesseract: not found"

**Symptom:** Red indicator for Tesseract

**Solution:** Only needed for DVD/Blu-ray bitmap subtitles
- If your videos have text subtitles, this is fine (optional)
- If you have bitmap subs, install: `winget install UB-Mannheim.TesseractOCR`

---

## Building from Source

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or VS Code
- FFmpeg, Tesseract, MKVToolNix (for runtime testing)

### Build

```bash
cd source

# Debug build
dotnet build

# Release build
dotnet build -c Release
```

### Run

```bash
# GUI (Debug)
dotnet run --project EpisodeMatcherGui

# CLI (Debug)
dotnet run --project EpisodeMatcherCli -- --help

# GUI (Release)
dotnet build -c Release
.\EpisodeMatcherGui\bin\Release\net8.0\EpisodeMatcherGui.exe
```

### Publish Self-Contained Binaries

```bash
# CLI - Windows x64
dotnet publish EpisodeMatcherCli -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true -o publish/

# GUI - Windows x64
dotnet publish EpisodeMatcherGui -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true -o publish/

# CLI - Linux x64
dotnet publish EpisodeMatcherCli -c Release -r linux-x64 \
  --self-contained true -p:PublishSingleFile=true -o publish/

# GUI - Linux x64 (requires libvlc-dev installed: sudo apt install libvlc-dev)
dotnet publish EpisodeMatcherGui -c Release -r linux-x64 \
  --self-contained true -p:PublishSingleFile=true -o publish/
```

Results: 64-73 MB executables ready to distribute.

---

## Architecture

### Core Library (EpisodeMatcherCore)

- **Models.cs** — Domain types (VideoFile, EpisodeReference, MatchResult)
- **Matcher.cs** — Similarity scoring engine
- **SubtitleExtractor.cs** — FFmpeg, Tesseract, MKVToolNix integration
- **SrtParser.cs** — SRT parsing and text normalization
- **Renamer.cs** — File renaming with dry-run support

### CLI (EpisodeMatcherCli)

- **Program.cs** — System.CommandLine integration, argument parsing

### GUI (EpisodeMatcherGui)

- **MVVM Architecture** — ViewModels, value converters, XAML bindings
- **Avalonia Framework** — Cross-platform UI
- **Multi-threading** — Non-blocking UI during processing
- **Drag & Drop** — File addition via drag-and-drop

See [REPOSITORY_STRUCTURE.md](REPOSITORY_STRUCTURE.md) for detailed architecture.

---

## Platform Support

| Platform | GUI | CLI | Notes |
|----------|-----|-----|-------|
| **Windows x64** | ✅ | ✅ | Primary platform, fully tested |
| **Windows ARM64** | ⚠️ | ⚠️ | Untested but should work |
| **Linux x64** | ✅ | ✅ | Requires X11 or Wayland; GUI requires `libvlc-dev` |
| **macOS** | ✅ | ✅ | Requires Xcode command-line tools |

---

## Dependencies

### .NET Libraries

All MIT licensed:
- **Avalonia 11.3.12** — Cross-platform UI framework
- **SkiaSharp** — 2D graphics rendering
- **HarfBuzzSharp** — Text shaping and layout
- **F23.StringSimilarity** — String similarity algorithms
- **MicroCom.Runtime** — COM interop

### Native Libraries

- **ANGLE** (av_libglesv2.dll) — OpenGL ES 2.0 renderer (BSD 3-Clause)
- **Skia Graphics** — 2D rendering engine (BSD 3-Clause)
- **HarfBuzz** — OpenType text engine (Old MIT)

### External Tools (User-Installed)

- **FFmpeg** — LGPL 2.1+
- **Tesseract OCR** — Apache 2.0
- **MKVToolNix** — GPL 2.0

See [ACKNOWLEDGEMENTS.md](ACKNOWLEDGEMENTS.md) for complete license details.

---

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

**Areas for contribution:**
- Bug fixes and improvements
- Additional similarity algorithms
- Platform-specific optimizations
- Documentation and tutorials
- Test coverage

---

## License

EpisodeMatcher is licensed under the **MIT License**. See [LICENSE](LICENSE) for details.

All bundled dependencies are compatible with commercial use and distribution.

---

## FAQ

**Q: Will this work with my video format?**  
A: Probably! If FFmpeg can read it, EpisodeMatcher can extract subtitles. Tested with MKV, MP4, AVI, M4V, MOV.

**Q: Do I need Tesseract?**  
A: Only if your videos have bitmap subtitles (DVD/Blu-ray). Text subtitles work without it.

**Q: Can I use this with anime?**  
A: Yes! The similarity algorithm is language-agnostic. Just provide correctly-named reference SRTs.

**Q: Is it safe?**  
A: Yes. The dry-run mode lets you preview all changes before applying them. Original files are preserved on error.

**Q: How long does it take?**  
A: Depends on video duration and subtitle complexity. 30-second extraction typically takes 5-15 seconds per file.

**Q: Can I batch process many seasons?**  
A: Yes! Use the CLI with multiple folder runs, or add all videos to the GUI (multi-threaded).

**Q: What if multiple episodes match equally well?**  
A: EpisodeMatcher flags this as "ambiguous" and skips the rename by default. Use `--force` flag to rename anyway (choose highest score).

---

## Support

- 📖 See [README files](source/) for detailed usage
- 🐛 [Report issues](https://github.com/yourusername/EpisodeMatcher/issues)
- 💬 [Discussions](https://github.com/yourusername/EpisodeMatcher/discussions)
- 📝 [Changelog](CHANGELOG.md)

---

## Acknowledgements

Special thanks to:
- [Avalonia UI](https://github.com/AvaloniaUI/Avalonia) — Cross-platform framework
- [SkiaSharp](https://github.com/mono/SkiaSharp) — Graphics rendering
- [FFmpeg](https://ffmpeg.org) — Subtitle extraction
- [OpenSubtitles](https://www.opensubtitles.org/) — SRT sources

See [ACKNOWLEDGEMENTS.md](ACKNOWLEDGEMENTS.md) for all dependencies and licenses.

---

**Made with ❤️ for TV enthusiasts everywhere.**
