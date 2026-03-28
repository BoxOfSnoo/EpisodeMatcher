# EpisodeMatcher

Renames ripped TV episode files (MKV, MP4, etc.) by matching embedded subtitles
against a folder of correctly-named reference SRT files.

Two interfaces are included:
- **EpisodeMatcherGui** — Avalonia cross-platform GUI (Windows / Linux / macOS)
- **EpisodeMatcherCli** — Command-line tool for scripting / batch use

---

## How it works

1. For each video file, FFmpeg extracts the subtitle track for the first N seconds
   (default: 30 s).
2. The extracted text is normalised and scored against the text in each reference
   SRT using bigram cosine similarity.
3. The video is renamed to match the highest-scoring reference SRT, provided the
   score meets the minimum threshold.

---

## Prerequisites

| Tool | Required | Purpose |
|------|----------|---------|
| [ffmpeg + ffprobe](https://ffmpeg.org/download.html) | **Yes** | Extracts subtitle streams |
| [Tesseract OCR](https://github.com/tesseract-ocr/tesseract) | DVD/Blu-ray bitmap subs only | Reads subtitle images |

### Windows — Winget

```
winget install Gyan.FFmpeg
winget install UB-Mannheim.TesseractOCR
```

### Windows — Chocolatey

```
choco install ffmpeg
choco install tesseract
```

### Linux (Debian / Ubuntu)

```bash
sudo apt install ffmpeg tesseract-ocr
```

### macOS (Homebrew)

```bash
brew install ffmpeg tesseract
```

---

## GUI Usage (EpisodeMatcherGui)

1. Launch `EpisodeMatcherGui.exe` (Windows) or `EpisodeMatcherGui` (Linux/macOS).
2. Check the **Tools** bar at the top — FFmpeg and Tesseract indicators will be
   green (found) or red (not found on PATH).
3. Set the **Reference SRT Folder** — the folder containing your correctly-named
   `.srt` or `.zip` files (one per episode).
4. **Drag and drop** MKV/MP4 files (or entire folders) onto the drop zone,
   or click **Add Files…**.
5. Adjust settings if needed (threads, window, min. score).
6. Keep **Dry Run** checked to preview renames without changing anything.
7. Click **Dry Run** — review the matched names in the list.
8. Uncheck **Dry Run**, then click **Rename Files** to apply.

Each row shows:
- Checkbox — uncheck to exclude a file from processing
- Original filename
- Matched episode name (blue)
- Status text with colour coding (green = matched, orange = ambiguous, red = no match)
- Similarity score
- Status icon (✅ ⚠️ ❌)

---

## CLI Usage (EpisodeMatcherCli)

```
EpisodeMatcherCli <videos> <srt-folder> [options]
```

| Argument / Option | Default | Description |
|-------------------|---------|-------------|
| `<videos>` | — | Folder containing the video files to rename |
| `<srt-folder>` | — | Folder with correctly-named `.srt` or `.zip` files |
| `--window <seconds>` | `30` | How many seconds of subtitles to sample |
| `--min-score <0-1>` | `0.20` | Minimum similarity score to accept a match |
| `--dry-run` | off | Print planned renames without changing anything |
| `--force` | off | Rename even when ambiguous or target exists |
| `--verbose` | off | Print extracted subtitle text per file |

### Examples

```
# Dry run first
EpisodeMatcherCli "D:\Rips\Show S01" "D:\SRTs\Show S01" --dry-run

# Apply renames
EpisodeMatcherCli "D:\Rips\Show S01" "D:\SRTs\Show S01"

# Longer window + lower threshold for difficult discs
EpisodeMatcherCli "D:\Rips\Show S01" "D:\SRTs\Show S01" --window 60 --min-score 0.10 --dry-run
```

---

## Preparing reference SRT files

The video is renamed to the SRT filename (minus the extension), preserving the
original video extension.

```
S01E01 - Pilot.srt                → renames video to  S01E01 - Pilot.mkv
S01E02 - The One with the Dog.srt → renames video to  S01E02 - The One with the Dog.mkv
```

ZIPs containing a single SRT are also supported — the episode name is taken from
the SRT filename inside the ZIP.

A reliable source for correctly-named SRTs: [OpenSubtitles](https://www.opensubtitles.org/)

---

## Subtitle type support

| Type | Codec | Works? |
|------|-------|--------|
| Soft text subs (MKV, MP4) | `subrip`, `ass`, `webvtt`, `mov_text` | Yes — direct |
| DVD bitmap subs | `dvd_subtitle` / `dvdsub` | Yes — with Tesseract |
| Blu-ray PGS subs | `hdmv_pgs_subtitle` | Yes — with Tesseract |
| No subtitle stream | — | Skipped with warning |

---

## Troubleshooting

**Score always 0 / no matches:**
Subtitles are likely bitmap-only and Tesseract is not installed or not on PATH.
Use `--verbose` (CLI) or check the log panel (GUI) to see what text was extracted.

**Wrong episode matched:**
Try increasing `--window` to 60–90 s. Also verify that the reference SRTs come
from the same release as your rip.

**FFmpeg not found on Windows:**
Add `C:\ffmpeg\bin` to your system PATH, or use `winget`/`choco` to install it.
