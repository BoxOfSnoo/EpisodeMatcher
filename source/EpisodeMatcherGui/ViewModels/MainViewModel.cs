using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EpisodeMatcherCore;

namespace EpisodeMatcherGui.ViewModels;

public class ToolStatus
{
    public string Name { get; set; } = "";
    public bool Found { get; set; }
    public string Path { get; set; } = "";
    public string Icon  => Found ? "✅" : "❌";
    public string Color => Found ? "#27ae60" : "#e74c3c";
    public string Detail => Found ? Path : "Not found on PATH";
}

public class MainViewModel : INotifyPropertyChanged
{
    // -----------------------------------------------------------------------
    // Observable state
    // -----------------------------------------------------------------------

    public ObservableCollection<EpisodeItemViewModel> Episodes { get; } = new();

    private string _srtFolder = "";
    public string SrtFolder
    {
        get => _srtFolder;
        set { _srtFolder = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRun)); }
    }

    private int _threadCount = 2;
    public int ThreadCount
    {
        get => _threadCount;
        set { _threadCount = Math.Max(1, Math.Min(16, value)); OnPropertyChanged(); }
    }

    private double _windowSeconds = 30.0;
    public double WindowSeconds
    {
        get => _windowSeconds;
        set { _windowSeconds = value; OnPropertyChanged(); }
    }

    private double _minScore = 0.20;
    public double MinScore
    {
        get => _minScore;
        set { _minScore = value; OnPropertyChanged(); }
    }

    private bool _dryRun = true;
    public bool DryRun
    {
        get => _dryRun;
        set { _dryRun = value; OnPropertyChanged(); OnPropertyChanged(nameof(RunButtonText)); }
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            _isRunning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRun));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(RunButtonText));
        }
    }

    private string _log = "";
    public string Log
    {
        get => _log;
        set { _log = value; OnPropertyChanged(); }
    }

    private int _progress;
    public int Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressText)); }
    }

    private int _progressMax = 1;
    public int ProgressMax
    {
        get => _progressMax;
        set { _progressMax = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressText)); }
    }

    public string ProgressText => ProgressMax > 0 ? $"{Progress} / {ProgressMax}" : "";

    public bool CanRun  => !IsRunning && Episodes.Count > 0 && Directory.Exists(SrtFolder);
    public bool CanEdit => !IsRunning;
    public string RunButtonText => DryRun ? "▶  Dry Run" : "▶  Rename Files";

    public ObservableCollection<ToolStatus> Tools { get; } = new();

    private string _selectedVideoPath = "";
    public string SelectedVideoPath
    {
        get => _selectedVideoPath;
        set { _selectedVideoPath = value; OnPropertyChanged(); }
    }

    private CancellationTokenSource? _cts;

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------

    public MainViewModel()
    {
        RefreshToolStatus();
    }

    // -----------------------------------------------------------------------
    // Tool detection
    // -----------------------------------------------------------------------

    public void RefreshToolStatus()
    {
        Tools.Clear();
        Tools.Add(MakeToolStatus("ffmpeg"));
        Tools.Add(MakeToolStatus("ffprobe"));
        Tools.Add(MakeToolStatus("tesseract"));
        Tools.Add(MakeToolStatus("mkvextract"));

        // Initialise the extractor paths (silent — UI shows status via indicators above)
        try { SubtitleExtractor.LocateTools(suppressOutput: true); } catch { }
    }

    private static ToolStatus MakeToolStatus(string name)
    {
        var path = FindOnPath(name);
        return new ToolStatus { Name = name, Found = path is not null, Path = path ?? "" };
    }

    private static string? FindOnPath(string name)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        var exts = OperatingSystem.IsWindows()
            ? new[] { ".exe", ".cmd" }
            : new[] { "" };

        foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        foreach (var ext in exts)
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
                $@"C:\ProgramData\chocolatey\bin\{name}.exe"
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        return null;
    }

    // -----------------------------------------------------------------------
    // Episode list management
    // -----------------------------------------------------------------------

    public void AddFiles(IEnumerable<string> paths)
    {
        var existing = Episodes.Select(e => e.FilePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var videoExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".mkv", ".mp4", ".avi", ".m4v", ".mov" };

        foreach (var path in paths)
        {
            if (!videoExts.Contains(Path.GetExtension(path))) continue;
            if (existing.Contains(path)) continue;

            Episodes.Add(new EpisodeItemViewModel
            {
                FilePath = path,
                FileName = Path.GetFileName(path),
                Status   = EpisodeStatus.Pending,
                StatusText = "Pending"
            });

            existing.Add(path);
        }

        // Auto-populate SRT folder if not already set
        if (Episodes.Count > 0 && string.IsNullOrEmpty(SrtFolder))
        {
            var firstEpisodeDir = Path.GetDirectoryName(Episodes.First().FilePath);
            if (!string.IsNullOrEmpty(firstEpisodeDir))
            {
                var srtFolder = FindSrtFolder(firstEpisodeDir);
                if (!string.IsNullOrEmpty(srtFolder))
                    SrtFolder = srtFolder;
            }
        }

        OnPropertyChanged(nameof(CanRun));
    }

    private static string? FindSrtFolder(string baseDir)
    {
        var srtNames = new[] { "subtitles", "srt", "srts" };
        var di = new DirectoryInfo(baseDir);
        
        foreach (var subDir in di.GetDirectories())
        {
            if (srtNames.Any(name => subDir.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return subDir.FullName;
        }

        return null;
    }

    public void RemoveSelected()
    {
        var toRemove = Episodes.Where(e => !e.IsSelected).ToList();
        // Actually remove the ones that have IsSelected == false
        // We repurpose "IsSelected" as a checkbox — remove those unchecked
        var unchecked_ = Episodes.Where(e => !e.IsSelected).ToList();
        foreach (var ep in unchecked_)
            Episodes.Remove(ep);

        OnPropertyChanged(nameof(CanRun));
    }

    public void ClearAll()
    {
        Episodes.Clear();
        OnPropertyChanged(nameof(CanRun));
    }

    // -----------------------------------------------------------------------
    // Processing
    // -----------------------------------------------------------------------

    public async Task RunAsync()
    {
        if (!CanRun) return;

        _cts = new CancellationTokenSource();
        IsRunning = true;
        Log = "";

        var toProcess = Episodes
            .Where(e => e.IsSelected && e.Status != EpisodeStatus.Renamed)
            .ToList();

        ProgressMax = toProcess.Count;
        Progress    = 0;

        AppendLog($"Loading reference SRT files from: {SrtFolder}");

        List<EpisodeReference> references;
        try
        {
            var refFiles = Directory.GetFiles(SrtFolder, "*.*")
                .Where(f => f.EndsWith(".srt", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                .ToList();

            references = refFiles
                .Select(f => new EpisodeReference(
                    f,
                    SrtParser.EpisodeNameFrom(f),
                    SrtParser.Parse(f, WindowSeconds * 2)))
                .Where(r => r.Entries.Count > 0)
                .ToList();

            AppendLog($"Loaded {references.Count} reference file(s).");
        }
        catch (Exception ex)
        {
            AppendLog($"Error loading SRT folder: {ex.Message}");
            IsRunning = false;
            return;
        }

        if (references.Count == 0)
        {
            AppendLog("No usable reference SRT/ZIP files found.");
            IsRunning = false;
            return;
        }

        var semaphore = new SemaphoreSlim(ThreadCount);
        var matcher   = new Matcher { MinAcceptScore = MinScore };
        var tasks     = new List<Task>();

        foreach (var ep in toProcess)
        {
            if (_cts.Token.IsCancellationRequested) break;

            var captured = ep;
            var task = Task.Run(async () =>
            {
                await semaphore.WaitAsync(_cts.Token);
                try
                {
                    await ProcessEpisodeAsync(captured, references, matcher);
                }
                finally
                {
                    semaphore.Release();
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        Progress++;
                    });
                }
            }, _cts.Token);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        int renamed   = toProcess.Count(e => e.Status == EpisodeStatus.Renamed);
        int matched   = toProcess.Count(e => e.Status == EpisodeStatus.Matched);
        int ambiguous = toProcess.Count(e => e.Status == EpisodeStatus.Ambiguous);
        int noMatch   = toProcess.Count(e => e.Status == EpisodeStatus.NoMatch);

        AppendLog("");
        AppendLog($"Done.  Renamed: {renamed}  Matched (dry-run): {matched}  Ambiguous: {ambiguous}  No match: {noMatch}");
        if (DryRun) AppendLog("Dry-run mode — uncheck 'Dry Run' and click again to apply renames.");

        IsRunning = false;
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }

    private async Task ProcessEpisodeAsync(
        EpisodeItemViewModel ep,
        List<EpisodeReference> references,
        Matcher matcher)
    {
        SetStatus(ep, EpisodeStatus.Processing, "Extracting subtitles…");

        string text;
        try
        {
            text = await Task.Run(() => SubtitleExtractor.Extract(ep.FilePath, WindowSeconds));
        }
        catch (Exception ex)
        {
            SetStatus(ep, EpisodeStatus.Error, $"Extraction error: {ex.Message}");
            AppendLog($"  [error] {ep.FileName}: {ex.Message}");
            return;
        }

        var result = matcher.Match(
            new VideoFile(ep.FilePath, ep.FileName),
            text,
            references);

        if (result.BestMatch is null)
        {
            SetStatus(ep, EpisodeStatus.NoMatch, $"No match (best={result.Score:P0})");
            SetExtractedText(ep, text);
            AppendLog($"  [no match] {ep.FileName}  best score={result.Score:P0}");
            return;
        }

        var status = result.Ambiguous ? EpisodeStatus.Ambiguous : EpisodeStatus.Matched;
        var statusText = result.Ambiguous
            ? $"Ambiguous → {result.BestMatch.EpisodeName} ({result.Score:P0})"
            : $"→ {result.BestMatch.EpisodeName} ({result.Score:P0})";

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            ep.MatchedName = result.BestMatch.EpisodeName;
            ep.Score       = result.Score;
            ep.ExtractedTextSnippet = GetTextSnippet(text);
            ep.TopCandidates = result.TopCandidates;
            ep.CandidatesText = GetCandidatesText(result);
            ep.HoveredCandidateText = "";
        });

        if (!DryRun && !result.Ambiguous)
        {
            try
            {
                var dir     = Path.GetDirectoryName(ep.FilePath)!;
                var ext     = Path.GetExtension(ep.FilePath);
                var newName = SanitizeFileName(result.BestMatch.EpisodeName) + ext;
                var newPath = Path.Combine(dir, newName);

                if (!string.Equals(ep.FilePath, newPath, StringComparison.OrdinalIgnoreCase))
                    File.Move(ep.FilePath, newPath);

                SetStatus(ep, EpisodeStatus.Renamed, $"Renamed → {newName}");
                AppendLog($"  [renamed] {ep.FileName}  →  {newName}");

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ep.FilePath = newPath;
                    ep.FileName = newName;
                });
                return;
            }
            catch (Exception ex)
            {
                SetStatus(ep, EpisodeStatus.Error, $"Rename error: {ex.Message}");
                AppendLog($"  [error] {ep.FileName}: {ex.Message}");
                return;
            }
        }

        SetStatus(ep, status, statusText);
        AppendLog($"  [{(result.Ambiguous ? "ambiguous" : "match")}] {ep.FileName}  →  {result.BestMatch.EpisodeName}  ({result.Score:P0})");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private void SetStatus(EpisodeItemViewModel ep, EpisodeStatus status, string text)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            ep.Status     = status;
            ep.StatusText = text;
        });
    }

    private void AppendLog(string line)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Log += line + Environment.NewLine;
        });
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }

    private static string GetTextSnippet(string text, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var snippet = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return snippet.Length > maxLength ? snippet[..maxLength] + "…" : snippet;
    }

    private static string GetCandidatesText(MatchResult result)
    {
        if (result.TopCandidates.Count == 0) return "";
        var lines = result.TopCandidates
            .Take(3)
            .Select((c, i) => $"{i + 1}. {c.Episode.EpisodeName} ({c.Score:P0})")
            .ToList();
        return string.Join("\n", lines);
    }

    private void SetExtractedText(EpisodeItemViewModel ep, string text)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            ep.ExtractedTextSnippet = GetTextSnippet(text);
        });
    }

    // -----------------------------------------------------------------------
    // INotifyPropertyChanged
    // -----------------------------------------------------------------------

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
