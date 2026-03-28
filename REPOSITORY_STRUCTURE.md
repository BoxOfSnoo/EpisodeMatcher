# Repository Structure

EpisodeMatcher is organized as follows:

```
EpisodeMatcher/
├── source/                              # Source code (C# .NET 8)
│   ├── EpisodeMatcherCore/              # Shared business logic library
│   │   ├── Models.cs                    # Domain models (VideoFile, MatchResult, etc.)
│   │   ├── Matcher.cs                   # Text similarity scoring engine
│   │   ├── SubtitleExtractor.cs         # FFmpeg integration
│   │   ├── SrtParser.cs                 # SRT file parsing and normalization
│   │   ├── Renamer.cs                   # File renaming operations
│   │   └── EpisodeMatcherCore.csproj
│   │
│   ├── EpisodeMatcherCli/               # Command-line application
│   │   ├── Program.cs                   # CLI entry point (System.CommandLine)
│   │   ├── EpisodeMatcher.csproj
│   │   └── publish/                     # Release binaries
│   │       ├── cli/
│   │       │   └── EpisodeMatcher.exe   # CLI executable (64.92 MB, self-contained)
│   │       └── gui/
│   │           └── EpisodeMatcherGui.exe # GUI executable (73.78 MB, self-contained)
│   │
│   ├── EpisodeMatcherGui/               # Avalonia desktop GUI
│   │   ├── ViewModels/                  # MVVM view models
│   │   │   ├── MainViewModel.cs
│   │   │   └── EpisodeItemViewModel.cs
│   │   ├── Converters/                  # XAML value converters
│   │   │   └── StringToBrushConverter.cs
│   │   ├── MainWindow.axaml(.cs)        # Main UI
│   │   ├── App.axaml(.cs)               # App shell
│   │   ├── EpisodeMatcherGui.csproj
│   │   └── bin/Debug/net8.0/            # Debug output (all DLLs + runtimes/)
│   │
│   ├── EpisodeMatcher.sln               # Visual Studio solution file
│   └── .github/
│       ├── copilot-instructions.md      # Copilot CLI guidance
│       └── GUI_DEPLOYMENT.md            # GUI deployment guide
│
├── .github/
│   └── workflows/                       # GitHub Actions (optional)
│
├── LICENSE                              # MIT License (EpisodeMatcher)
├── ACKNOWLEDGEMENTS.md                  # Dependency licenses & attribution
├── CONTRIBUTING.md                      # Contribution guidelines
├── CHANGELOG.md                         # Version history
├── .gitignore                           # Git ignore rules
└── README.md                            # This file

```

## Key Directories

### `/source/EpisodeMatcherCore/`
Core matching and file processing logic. Pure business logic with no UI dependencies.

**Key Classes:**
- `Matcher` — Scores videos against episodes (cosine similarity + Jaro-Winkler)
- `SubtitleExtractor` — Extracts subtitles using FFmpeg, Tesseract, and MKVToolNix
- `SrtParser` — Parses SRT files and normalizes text
- `Models` — Domain types (VideoFile, EpisodeReference, MatchResult, etc.)

### `/source/EpisodeMatcherCli/`
Command-line interface for batch processing and scripting. Minimal UI, focused on input/output.

**Entry Point:** `Program.cs` uses System.CommandLine for robust CLI parsing.

### `/source/EpisodeMatcherGui/`
Desktop GUI application using Avalonia (cross-platform: Windows, Linux, macOS).

**Architecture:**
- MVVM pattern with `MainViewModel` and `EpisodeItemViewModel`
- Drag-and-drop file support
- Multi-threaded processing
- Color-coded status indicators
- Configurable matching parameters

### `/source/.github/`
Documentation for developers and CI/CD considerations.

- `copilot-instructions.md` — AI assistant context for future development
- `GUI_DEPLOYMENT.md` — Deployment and dependency management

## Build Output

### Debug Build
```
source/EpisodeMatcherGui/bin/Debug/net8.0/
├── EpisodeMatcherGui.exe                # Not self-contained
├── *.dll (43 files)                     # All managed assemblies
├── runtimes/win-x64/native/             # Platform-specific native DLLs
│   ├── av_libglesv2.dll                 # OpenGL ES renderer
│   ├── libHarfBuzzSharp.dll             # Text shaping
│   └── libSkiaSharp.dll                 # 2D graphics
└── *.json, *.config files
```

### Release Build (Self-Contained)
```
source/publish/
├── cli/
│   └── EpisodeMatcher.exe               # CLI executable (64.92 MB)
└── gui/
    └── EpisodeMatcherGui.exe            # GUI executable (73.78 MB)
```

Both executables include all dependencies and runtimes. No installation required — just run them directly.

## Dependency Graph

```
EpisodeMatcherGui
├── EpisodeMatcherCore
│   └── F23.StringSimilarity
├── Avalonia.*.dll (23 packages)
│   ├── SkiaSharp (2D graphics)
│   ├── HarfBuzzSharp (text shaping)
│   ├── MicroCom.Runtime
│   └── Avalonia.Remote.Protocol
├── System.IO.Pipelines
└── Tmds.DBus.Protocol (Linux support)

Native Libraries
├── libSkiaSharp.dll / .so / .dylib
├── libHarfBuzzSharp.dll / .so / .dylib
├── av_libglesv2.dll (Windows only)
└── *.so / *.dylib (Linux/macOS)
```

## Development Workflow

### Setup
```bash
cd source
dotnet restore
dotnet build
```

### Running
```bash
# GUI (Debug)
dotnet run --project EpisodeMatcherGui

# CLI (Debug)
dotnet run --project EpisodeMatcherCli -- --help

# GUI (Release)
dotnet publish EpisodeMatcherGui -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Testing
```bash
dotnet build -c Release
# (No test project yet; manual testing via CLI dry-run and GUI)
```

## Platform Support

| Platform | GUI | CLI | Notes |
|----------|-----|-----|-------|
| Windows x64 | ✅ | ✅ | Primary platform |
| Windows ARM64 | ⚠️ | ⚠️ | Untested |
| Linux x64 | ✅ | ✅ | Requires X11 or Wayland |
| macOS | ✅ | ✅ | Requires Xcode command-line tools |

## Release Checklist

- [ ] Update `CHANGELOG.md` with new features/fixes
- [ ] Bump version if semantic versioning applies
- [ ] Run `dotnet build -c Release`
- [ ] Publish both CLI and GUI: `dotnet publish ... -p:PublishSingleFile=true`
- [ ] Test both executables on target platform
- [ ] Create GitHub release with version tag
- [ ] Attach published binaries to release

## License

EpisodeMatcher is licensed under the MIT License. See `LICENSE` file.

All included dependencies are compatible with MIT licensing. See `ACKNOWLEDGEMENTS.md` for details.

---

For contribution guidelines, see `CONTRIBUTING.md`.
For deployment instructions, see `source/.github/GUI_DEPLOYMENT.md`.
