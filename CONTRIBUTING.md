# Contributing to EpisodeMatcher

Thank you for your interest in contributing! This document outlines guidelines for contributing to EpisodeMatcher.

## Code of Conduct

Please be respectful and considerate of others when contributing to this project.

## How to Contribute

### Reporting Issues

- Check existing issues first to avoid duplicates
- Provide clear, descriptive titles
- Include steps to reproduce the issue
- Mention your OS, .NET version, and EpisodeMatcher version
- Attach logs or screenshots if relevant

### Submitting Code Changes

1. **Fork the repository** on GitHub
2. **Create a feature branch:**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes** following the code style (see below)
4. **Test your changes:**
   ```bash
   dotnet build
   dotnet test  # (if applicable)
   ```

5. **Commit with clear messages:**
   ```bash
   git commit -m "Fix/feature: Brief description of changes"
   ```

6. **Push to your fork** and create a Pull Request

## Code Style

- Use the existing code style in the project
- Follow C# naming conventions (PascalCase for public members)
- Use meaningful variable names
- Add comments only for non-obvious logic
- Keep methods focused and reasonably sized

## Project Structure

```
source/
├── EpisodeMatcherCore/          # Shared business logic
│   ├── Models.cs                # Domain models
│   ├── Matcher.cs               # Similarity scoring
│   ├── SubtitleExtractor.cs     # FFmpeg integration
│   ├── SrtParser.cs             # SRT file parsing
│   └── Renamer.cs               # File renaming logic
│
├── EpisodeMatcherCli/           # Command-line interface
│   ├── Program.cs               # CLI entry point and argument parsing
│   └── publish/                 # Release binaries
│
└── EpisodeMatcherGui/           # Avalonia GUI application
    ├── ViewModels/              # MVVM ViewModels
    ├── Converters/              # Value converters for XAML
    ├── MainWindow.axaml         # Main UI layout
    └── publish/                 # Release binaries
```

## Key Areas for Contribution

### Bug Fixes
- Video format support issues
- Subtitle extraction failures
- UI responsiveness

### Features
- Additional similarity algorithms
- Batch processing improvements
- Platform-specific optimizations

### Documentation
- Clarifying README sections
- Adding tutorials or guides
- Improving in-code comments

## Building & Testing

```bash
# Build the entire solution
dotnet build -c Release

# Run a specific project
dotnet run --project EpisodeMatcherCli -- --help

# Publish self-contained binaries
dotnet publish EpisodeMatcherGui -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Dependencies

- .NET 8 SDK
- FFmpeg + FFprobe (runtime)
- Tesseract OCR (optional, for bitmap subtitles)
- MKVToolNix (optional, for MKV subtitle extraction)

## Licensing

By contributing, you agree that your changes will be licensed under the MIT License (see LICENSE file).

## Questions?

Feel free to open a discussion or issue with your questions. We're here to help!

---

Happy coding! 🎬
