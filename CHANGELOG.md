# Changelog

All notable changes to EpisodeMatcher will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Multiple match candidates display in GUI (top 3 with scores)
- Extracted subtitle text snippet on hover in GUI
- Top candidates and text snippet display in CLI (verbose mode)
- CLI shows candidates for both matched and unmatched files
- Improved MKVToolNix detection (only show install message if not found)
- Copilot instructions and deployment guides

### Fixed
- GUI launch failure due to List<T> binding issue (now uses ObservableCollection)
- Processing logic now correctly uses checked items (was inverted)
- Optional dependencies no longer show misleading install messages when found

### Changed
- GUI checkbox semantics: checked items are processed (was inverted)
- Better error messaging for missing tools

## [Initial Release]

### Added
- Cross-platform GUI (Windows, Linux, macOS) using Avalonia
- Command-line interface for batch processing
- Subtitle extraction from MKV, MP4, and other video formats
- Support for text subtitles (subrip, ass, webvtt, mov_text)
- Support for bitmap subtitles (DVD/Blu-ray) with OCR
- Bigram cosine similarity + Jaro-Winkler hybrid scoring
- Dry-run mode to preview renames before applying
- Multi-threaded processing for GUI
- Configurable matching thresholds and windows
- Tool status indicators in GUI
- Comprehensive logging in GUI

### Technical
- Built with .NET 8 and Avalonia 11.3.12
- Self-contained binary deployment
- FFmpeg integration for subtitle extraction
- Tesseract OCR for bitmap subtitles
- MKVToolNix integration for MKV subtitle extraction
- F23.StringSimilarity for text matching algorithms

---

## Release Strategy

We follow semantic versioning:
- **MAJOR.MINOR.PATCH**
- MAJOR: Breaking changes to CLI/API
- MINOR: New features, backward compatible
- PATCH: Bug fixes and small improvements

## Notes

For a detailed comparison of versions, see the [GitHub releases page](https://github.com/yourusername/EpisodeMatcher/releases).
