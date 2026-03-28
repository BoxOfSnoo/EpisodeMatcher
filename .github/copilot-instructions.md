# Copilot Instructions for EpisodeMatcher

## Overview

**EpisodeMatcher** is a cross-platform tool (CLI + GUI) that renames ripped TV episode files by matching their embedded subtitles against a folder of correctly-named reference SRT files. The matching uses bigram cosine similarity with Jaro-Winkler tiebreaker scoring.

## Build & Run Commands

### Prerequisites

**Runtime dependencies** (required):
- FFmpeg + FFprobe: Subtitle extraction from video files
- Tesseract OCR: Text recognition from bitmap subtitles (DVD/Blu-ray only)

**Development dependencies**:
- .NET 8 SDK
- Visual Studio 2022 or VS Code

### Building

```bash
# Debug build
dotnet build

# Release build (optimized)
dotnet build -c Release
```

### Running the CLI

```bash
# Basic usage (after building)
dotnet run --project EpisodeMatcherCli -- <videos-folder> <srt-folder> [options]

# Example: dry run
dotnet run --project EpisodeMatcherCli -- "D:\Videos\Show S01" "D:\SRTs\Show S01" --dry-run

# With all options
dotnet run --project EpisodeMatcherCli -- "D:\Videos\Show S01" "D:\SRTs\Show S01" --window 60 --min-score 0.15 --verbose
```

### Running the GUI

```bash
# Debug
dotnet run --project EpisodeMatcherGui

# Release
dotnet run --project EpisodeMatcherGui -c Release
```

### Publishing (Self-Contained Single-File)

```bash
# Windows x64
dotnet publish EpisodeMatcherCli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/

# Linux x64
dotnet publish EpisodeMatcherCli -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```

### Distributing the GUI

**Recommended: Use self-contained publish** (creates single `.exe` with all dependencies):

```bash
dotnet publish EpisodeMatcherGui -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```

**If manually copying files**, ensure you copy the entire folder structure from `bin\Release\net8.0\`:
- All `.dll` files in the root
- The `runtimes\` folder with platform-specific native binaries
- The `.deps.json` and `.runtimeconfig.json` files
- Do NOT copy `.pdb` files (optional, only for debugging)

**Critical Windows x64 native dependencies** (in `runtimes/win-x64/native/`):
- `av_libglesv2.dll` — OpenGL ES 2.0 renderer (Avalonia graphics)
- `libHarfBuzzSharp.dll` — Text shaping (typography)
- `libSkiaSharp.dll` — 2D graphics (rendering engine)

**Never copy individual DLL files** — always copy the entire `runtimes/` directory structure so the loader can find the correct platform-specific versions.

## Architecture

### Project Structure

- **EpisodeMatcherCore**: Shared library containing core business logic
  - `Models.cs`: Domain models (VideoFile, EpisodeReference, MatchResult, SrtEntry)
  - `SubtitleExtractor.cs`: FFmpeg integration to extract subtitles from videos
  - `SrtParser.cs`: Parsing and normalization of SRT subtitle files
  - `Matcher.cs`: Similarity scoring engine using F23.StringSimilarity
  - `Renamer.cs`: File system operations for dry-run and actual renaming

- **EpisodeMatcherCli**: Command-line interface
  - `Program.cs`: System.CommandLine argument parsing and main entry point
  - Minimal logic (delegates to Core)

- **EpisodeMatcherGui**: Avalonia desktop UI
  - MVVM architecture with ViewModels and converters
  - Multi-threaded processing to avoid UI blocking
  - Drag-and-drop support for file addition

### Data Flow

1. **VideoFile** → **SubtitleExtractor** → Raw subtitle text
2. Raw text + **EpisodeReferences** → **Matcher** → **MatchResult** (with score)
3. Accepted results → **Renamer** → File system (dry-run or real)

### Key Dependencies

- **F23.StringSimilarity** (v7.0.1): Cosine similarity (bigram) and Jaro-Winkler algorithms
- **System.CommandLine** (v2.0.0-beta4): CLI argument parsing
- **Avalonia** (v11.3.12): Cross-platform GUI framework

## Key Conventions

### Scoring Algorithm

The `Matcher` class combines two similarity metrics:
- **Cosine similarity (85% weight)**: Bigram-based, primary signal
- **Jaro-Winkler (15% weight)**: Character-level, tiebreaker

Combined score = `cosine * 0.85 + jaro_winkler * 0.15`

- **Accepted**: score ≥ `MinAcceptScore` (default: 0.20)
- **Ambiguous**: Two top scores within `AmbiguityThreshold` (default: 0.05)

### Text Normalization

`SrtParser.ToComparisonText()` normalizes subtitle text for comparison:
- HTML tags removed
- Whitespace normalized
- Case-insensitive matching

### Video Extension Handling

The renamer preserves the original video file extension when renaming:
- `MyVideo.mkv` matching `S01E01 - Pilot.srt` → `S01E01 - Pilot.mkv`
- Works with `.mkv`, `.mp4`, and other container formats

### Supported Subtitle Codecs

| Type | Codec | Requires Tesseract? |
|------|-------|-------------------|
| Text subtitles (MKV/MP4 soft subs) | subrip, ass, webvtt, mov_text | No |
| DVD bitmap subs | dvd_subtitle / dvdsub | Yes |
| Blu-ray PGS subs | hdmv_pgs_subtitle | Yes |

### Error Handling Pattern

- CLI exits with code 1 on validation errors (missing folders, tools)
- GUI logs errors to the log panel; non-blocking for other files
- Both preserve original files on errors; rename only on success

### Threading

- GUI uses multi-threading (Thread count configurable) to avoid UI freezing
- CLI is single-threaded by design for simplicity
- Both use the same Core logic to ensure consistency

## Testing

No formal test projects currently exist. Validation is done manually:
- CLI: Dry-run mode to preview renames before applying
- GUI: Visual inspection in the UI; checkbox to exclude problematic files

When adding tests:
- Use xUnit with EpisodeMatcherCore as the primary test target
- Focus on `Matcher` scoring edge cases and `SrtParser` text normalization
- Mock `SubtitleExtractor` (external FFmpeg dependency)

## Common Tasks

### Adding a new CLI option

1. Define the option in `Program.cs` using `System.CommandLine`
2. Add parameter to the `Run()` method signature
3. Pass to Core class constructors if needed
4. Update README examples

### Changing scoring thresholds

1. Modify `Matcher.MinAcceptScore` or `AmbiguityThreshold` defaults
2. Test against a variety of video releases (scoring is release-sensitive)
3. Document reasoning in commit message

### Supporting a new subtitle codec

1. Check `SubtitleExtractor.cs` for FFmpeg codec mapping
2. If bitmap-based, ensure Tesseract flow is tested
3. Add codec to the "Supported Subtitle Codecs" table in README

### GUI Layout Changes

- Use Avalonia's XAML in `.axaml` files
- Keep ViewModels logic-free (bind directly to command handlers when possible)
- Test drag-and-drop on both Windows and Linux
