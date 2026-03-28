using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace EpisodeMatcherCore;

/// <summary>
/// Extracts subtitle text from a video file using FFmpeg (and optionally mkvextract + Tesseract).
///
/// For text subtitles (subrip/ass/webvtt): extracted directly via ffmpeg → SRT → parse.
/// For bitmap subtitles (dvd_subtitle/pgssub): three strategies are tried in order:
///   1. mkvextract  → .sub/.idx pair → Tesseract OCR
///   2. ffmpeg subtitles filter (burn onto lavfi black canvas) → PNG → Tesseract OCR
///   3. ffmpeg -c:s copy into .mkv, then re-read subtitle packets as images
/// </summary>
public static class SubtitleExtractor
{
    private static string? _ffmpegPath;
    private static string? _ffprobePath;
    private static string? _tesseractPath;
    private static string? _mkvextractPath;

    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(120);

    public static void LocateTools(bool suppressOutput = false)
    {
        _ffmpegPath    = FindTool("ffmpeg");
        _ffprobePath   = FindTool("ffprobe");
        _tesseractPath = FindTool("tesseract");
        _mkvextractPath = FindTool("mkvextract");

        if (!suppressOutput)
        {
            if (_ffmpegPath is null)
                throw new InvalidOperationException(
                    "ffmpeg not found. Please install it and ensure it is on your PATH.");
            if (_ffprobePath is null)
                throw new InvalidOperationException(
                    "ffprobe not found. Please install it and ensure it is on your PATH.");

            Console.WriteLine($"  ffmpeg    : {_ffmpegPath}");
            Console.WriteLine($"  ffprobe   : {_ffprobePath}");
            Console.WriteLine(_tesseractPath is not null
                ? $"  tesseract : {_tesseractPath}"
                : "  tesseract : not found (bitmap subtitles will be skipped)");
            Console.WriteLine(_mkvextractPath is not null
                ? $"  mkvextract: {_mkvextractPath}"
                : "  mkvextract: not found (will use ffmpeg fallback for DVD subs)");
        }
    }

    public static string Extract(string videoPath, double windowSeconds = 30.0)
    {
        if (_ffmpegPath is null || _ffprobePath is null)
            throw new InvalidOperationException(
                "ffmpeg/ffprobe not found. Install FFmpeg and click Refresh.");

        var streams = ProbeSubtitleStreams(videoPath);

        if (streams.Count == 0)
        {
            Console.Error.WriteLine($"  [info] No subtitle streams in {Path.GetFileName(videoPath)}");
            return string.Empty;
        }

        foreach (var s in streams)
            Console.WriteLine($"  [stream] index={s.FileIndex} subtitle#{s.SubIndex} codec={s.CodecName} text={s.IsText}");

        var textStream = streams.FirstOrDefault(s => s.IsText);
        if (textStream is not null)
            return ExtractTextSubtitle(videoPath, textStream.SubIndex, windowSeconds);

        // Bitmap path — need Tesseract
        if (_tesseractPath is null)
        {
            Console.Error.WriteLine(
                $"  [warn] Only bitmap subtitles found. Install Tesseract and add it to PATH.");
            return string.Empty;
        }

        // Try each bitmap stream until we get text
        foreach (var stream in streams)
        {
            var text = ExtractBitmapSubtitleOcr(videoPath, stream.FileIndex, stream.SubIndex, windowSeconds);
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return string.Empty;
    }

    // -----------------------------------------------------------------------
    // Probing
    // -----------------------------------------------------------------------

    private record StreamInfo(int FileIndex, int SubIndex, bool IsText, string CodecName);

    private static List<StreamInfo> ProbeSubtitleStreams(string videoPath)
    {
        var output = RunProcess(_ffprobePath!,
            $"-v error -select_streams s " +
            $"-show_entries stream=index,codec_name " +
            $"-of csv=p=0 \"{videoPath}\"");

        var streams = new List<StreamInfo>();
        int subIndex = 0;
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split(',');
            if (parts.Length < 2) continue;
            if (!int.TryParse(parts[0], out int fileIdx)) continue;
            var codec = parts[1].Trim().ToLowerInvariant();
            bool isText = codec is "subrip" or "srt" or "ass" or "ssa" or "webvtt" or "mov_text";
            streams.Add(new StreamInfo(fileIdx, subIndex++, isText, codec));
        }
        return streams;
    }

    // -----------------------------------------------------------------------
    // Text subtitle extraction
    // -----------------------------------------------------------------------

    private static string ExtractTextSubtitle(string videoPath, int subIndex, double windowSeconds)
    {
        var tmpSrt = Path.Combine(Path.GetTempPath(), $"em_{Guid.NewGuid():N}.srt");
        try
        {
            RunProcess(_ffmpegPath!,
                $"-y -i \"{videoPath}\" " +
                $"-t {windowSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)} " +
                $"-map 0:s:{subIndex} -c:s srt \"{tmpSrt}\"");

            if (!File.Exists(tmpSrt) || new FileInfo(tmpSrt).Length == 0)
                return string.Empty;

            var entries = SrtParser.Parse(tmpSrt, windowSeconds);
            var text = SrtParser.ToComparisonText(entries);
            Console.WriteLine($"  [text] {entries.Count} cues, {text.Split(' ').Length} words");
            return text;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  [warn] Text extraction failed: {ex.Message}");
            return string.Empty;
        }
        finally
        {
            if (File.Exists(tmpSrt)) File.Delete(tmpSrt);
        }
    }

    // -----------------------------------------------------------------------
    // Bitmap subtitle OCR — multiple strategies
    // -----------------------------------------------------------------------

    private static string ExtractBitmapSubtitleOcr(
        string videoPath, int fileIndex, int subIndex, double windowSeconds)
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), $"em_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            var dur = windowSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

            // ── Strategy 1: mkvextract → .sub/.idx → ffmpeg decode → OCR ──
            // mkvextract syntax: mkvextract <source-file> tracks <TID>:<output>
            if (_mkvextractPath is not null &&
                videoPath.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase))
            {
                var subBase = Path.Combine(tmpDir, "track");
                // Correct syntax: mkvextract <file> tracks <TID>:<output>
                var (_, mkvStderr) = RunProcessFull(_mkvextractPath,
                    $"\"{videoPath}\" tracks {fileIndex}:\"{subBase}.sub\"");

                var subFile = subBase + ".sub";
                var idxFile = subBase + ".idx";
                Console.WriteLine($"  [ocr] mkvextract: sub={File.Exists(subFile)} " +
                    $"({(File.Exists(subFile) ? new FileInfo(subFile).Length : 0)} bytes) " +
                    $"idx={File.Exists(idxFile)}");

                if (File.Exists(idxFile) && File.Exists(subFile) && new FileInfo(subFile).Length > 0)
                {
                    var text = OcrSubIdxPair(idxFile, tmpDir, windowSeconds);
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
                else
                {
                    var tail = mkvStderr.Length > 400 ? mkvStderr[^400..] : mkvStderr;
                    Console.WriteLine($"  [ocr] mkvextract stderr tail: {tail}");
                }
            }

            // ── Strategy 2: ffmpeg copy subtitle stream → .mkv → mkvextract ──
            if (_mkvextractPath is not null)
            {
                var subMkv = Path.Combine(tmpDir, "subsonly.mkv");
                RunProcess(_ffmpegPath!,
                    $"-y -i \"{videoPath}\" -t {dur} " +
                    $"-map 0:{fileIndex} -c:s copy \"{subMkv}\"");

                Console.WriteLine($"  [ocr] sub-only mkv: {(File.Exists(subMkv) ? new FileInfo(subMkv).Length : 0)} bytes");

                if (File.Exists(subMkv) && new FileInfo(subMkv).Length > 4096)
                {
                    var subBase2 = Path.Combine(tmpDir, "track2");
                    RunProcessFull(_mkvextractPath,
                        $"\"{subMkv}\" tracks 0:\"{subBase2}.sub\"");

                    var idxFile2 = subBase2 + ".idx";
                    var subFile2 = subBase2 + ".sub";
                    Console.WriteLine($"  [ocr] remux mkvextract: sub={File.Exists(subFile2)} " +
                        $"({(File.Exists(subFile2) ? new FileInfo(subFile2).Length : 0)} bytes) " +
                        $"idx={File.Exists(idxFile2)}");

                    if (File.Exists(idxFile2) && File.Exists(subFile2) && new FileInfo(subFile2).Length > 0)
                    {
                        var text = OcrSubIdxPair(idxFile2, tmpDir, windowSeconds);
                        if (!string.IsNullOrWhiteSpace(text)) return text;
                    }
                }
            }

            // ── Strategy 3: direct ffmpeg .idx decode (no mkvextract needed) ──
            // If we somehow got an .idx from strategy 1/2 but OCR returned empty, skip.
            // Try generating .idx via ffmpeg directly (some builds support this).
            {
                var idxDirect = Path.Combine(tmpDir, $"direct{subIndex}.idx");
                RunProcess(_ffmpegPath!,
                    $"-y -i \"{videoPath}\" -t {dur} " +
                    $"-map 0:s:{subIndex} -c:s copy \"{idxDirect}\"");

                Console.WriteLine($"  [ocr] direct idx: {(File.Exists(idxDirect) ? new FileInfo(idxDirect).Length : 0)} bytes");

                if (File.Exists(idxDirect) && new FileInfo(idxDirect).Length > 0)
                {
                    var text = OcrSubIdxPair(idxDirect, tmpDir, windowSeconds);
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }

            Console.Error.WriteLine(
                "  [warn] All bitmap OCR strategies failed.\n" +
                (string.IsNullOrEmpty(_mkvextractPath)
                    ? "  Install MKVToolNix: choco install mkvtoolnix  (or apt install mkvtoolnix)\n"
                    : "") +
                "  MKVToolNix path checked: " + (_mkvextractPath ?? "not found"));
            return string.Empty;
        }
        finally
        {
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, recursive: true);
        }
    }

    /// <summary>
    /// Given a .sub/.idx pair (pass the .idx path), renders VobSub frames onto a black
    /// canvas using ffmpeg -filter_complex overlay, then OCRs the subtitle region.
    ///
    /// VobSub streams cannot be written directly to image2 — they must be overlaid
    /// onto a video source first. We use lavfi nullsrc as the base.
    /// </summary>
    private static string OcrSubIdxPair(
        string idxFile, string tmpDir, double windowSeconds)
    {
        var dur = windowSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        var pngPattern = Path.Combine(tmpDir, "idx%04d.png");

        // Strategy A: lavfi color source (720x480 @ 2fps) + vobsub overlay → PNG sequence.
        // VobSub streams cannot be written to image2 directly — they must be overlaid
        // onto a video source. We use two inputs: lavfi color black + the .idx file.
        // ffmpeg auto-locates the paired .sub from the .idx path.
        var (_, stderrA) = RunProcessFull(_ffmpegPath!,
            $"-y " +
            $"-f lavfi -i color=black:size=720x480:rate=2 " +
            $"-f vobsub -i \"{idxFile}\" " +
            $"-t {dur} " +
            $"-filter_complex \"[0:v][1:s:0]overlay=0:0[out]\" " +
            $"-map \"[out]\" " +
            $"-vsync vfr " +
            $"\"{pngPattern}\"");

        var pngs = Directory.GetFiles(tmpDir, "idx*.png").OrderBy(f => f).ToArray();
        Console.WriteLine($"  [ocr] vobsub overlay: {pngs.Length} frames");

        if (pngs.Length == 0)
        {
            var tail = stderrA.Length > 600 ? stderrA[^600..] : stderrA;
            Console.WriteLine($"  [ocr] overlay stderr: {tail}");

            // Strategy B: try subtitles= filter (works if ffmpeg was built with libass
            // and the vobsub demuxer exposes a video-compatible stream on some builds).
            var pngPatternB = Path.Combine(tmpDir, "idxb%04d.png");
            var idxEscB = OperatingSystem.IsWindows()
                ? idxFile.Replace("\\", "\\\\\\\\").Replace(":", "\\\\:")
                : idxFile.Replace(":", "\\:");
            RunProcessFull(_ffmpegPath!,
                $"-y " +
                $"-f lavfi -i color=black:size=720x480:rate=2 " +
                $"-t {dur} " +
                $"-vf \"subtitles='{idxEscB}'\" " +
                $"\"{pngPatternB}\"");

            var pngsB = Directory.GetFiles(tmpDir, "idxb*.png").OrderBy(f => f).ToArray();
            Console.WriteLine($"  [ocr] subtitles filter: {pngsB.Length} frames");
            if (pngsB.Length > 0)
                return OcrFrames(pngsB, tmpDir);
        }

        return OcrFrames(pngs, tmpDir);
    }

    /// <summary>OCRs an array of PNG frames and returns normalised combined text.</summary>
    private static string OcrFrames(string[] pngs, string tmpDir)
    {
        var texts = new List<string>();
        foreach (var png in pngs.Take(120))
        {
            // Skip near-blank frames (file < 3 KB = no visible subtitle)
            if (new FileInfo(png).Length < 3072) continue;

            var ocrBase = Path.Combine(tmpDir, "ocr_" + Path.GetFileNameWithoutExtension(png));
            RunProcess(_tesseractPath!,
                $"\"{png}\" \"{ocrBase}\" -l eng --psm 7");

            var txtFile = ocrBase + ".txt";
            if (File.Exists(txtFile))
            {
                var t = File.ReadAllText(txtFile).Trim();
                if (!string.IsNullOrWhiteSpace(t)) texts.Add(t);
            }
        }
        Console.WriteLine($"  [ocr] text from {texts.Count}/{pngs.Length} frames");
        return SrtParser.NormalizeText(string.Join(" ", texts));
    }

    // -----------------------------------------------------------------------
    // Process helpers
    // -----------------------------------------------------------------------

    private static string RunProcess(string exe, string args)
        => RunProcessFull(exe, args).stdout;

    private static (string stdout, string stderr) RunProcessFull(string exe, string args)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            RedirectStandardInput  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding  = Encoding.UTF8,
        };

        using var p = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start: {exe}");

        // Close stdin immediately so tools never block waiting for input
        p.StandardInput.Close();

        // Read both streams asynchronously to avoid deadlock
        var stdoutTask = Task.Run(() => p.StandardOutput.ReadToEnd());
        var stderrTask = Task.Run(() => p.StandardError.ReadToEnd());

        bool exited = p.WaitForExit((int)ProcessTimeout.TotalMilliseconds);
        if (!exited)
        {
            try { p.Kill(entireProcessTree: true); } catch { }
            Console.Error.WriteLine(
                $"  [warn] Process timed out ({ProcessTimeout.TotalSeconds}s): {Path.GetFileName(exe)}");
        }

        var stdout = stdoutTask.Wait(TimeSpan.FromSeconds(5)) ? stdoutTask.Result : "";
        var stderr = stderrTask.Wait(TimeSpan.FromSeconds(5)) ? stderrTask.Result : "";
        return (stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Tool discovery
    // -----------------------------------------------------------------------

    private static string? FindTool(string name)
    {
        var pathDirs = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        var extensions = OperatingSystem.IsWindows()
            ? new[] { ".exe", ".cmd", ".bat" }
            : new[] { "" };

        foreach (var dir in pathDirs)
        foreach (var ext in extensions)
        {
            var full = Path.Combine(dir, name + ext);
            if (File.Exists(full)) return full;
        }

        if (OperatingSystem.IsWindows())
        {
            var candidates = new[]
            {
                $@"C:\Program Files\ffmpeg\bin\{name}.exe",
                $@"C:\ffmpeg\bin\{name}.exe",
                $@"C:\ProgramData\chocolatey\bin\{name}.exe",
                $@"C:\Program Files\Tesseract-OCR\{name}.exe",
                $@"C:\Program Files\MKVToolNix\{name}.exe",
                $@"C:\Program Files (x86)\MKVToolNix\{name}.exe",
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        return null;
    }

    /// <summary>Escapes a file path for use inside an ffmpeg -vf filter string.</summary>
    private static string EscapeFilterPath(string path)
    {
        // ffmpeg filter paths need backslashes doubled and colons escaped on Windows
        if (OperatingSystem.IsWindows())
            path = path.Replace("\\", "\\\\").Replace(":", "\\:");
        return path;
    }
}
