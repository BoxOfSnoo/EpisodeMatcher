using System.CommandLine;
using EpisodeMatcherCore;

// ---------------------------------------------------------------------------
// CLI definition
// ---------------------------------------------------------------------------

var videosArg = new Argument<DirectoryInfo>(
    name: "videos",
    description: "Folder containing the MKV/MP4 files to rename.")
{
    Arity = ArgumentArity.ExactlyOne
};

var srtArg = new Argument<DirectoryInfo>(
    name: "srt-folder",
    description: "Folder containing the reference .srt files (one per episode, named correctly).")
{
    Arity = ArgumentArity.ExactlyOne
};

var windowOpt = new Option<double>(
    name: "--window",
    description: "How many seconds of subtitles to extract from the start of each video.",
    getDefaultValue: () => 30.0);

var dryRunOpt = new Option<bool>(
    name: "--dry-run",
    description: "Print planned renames without actually renaming any files.");

var forceOpt = new Option<bool>(
    name: "--force",
    description: "Rename even when the match is ambiguous, or overwrite existing files.");

var minScoreOpt = new Option<double>(
    name: "--min-score",
    description: "Minimum similarity score (0–1) required to accept a match.",
    getDefaultValue: () => 0.20);

var verboseOpt = new Option<bool>(
    name: "--verbose",
    description: "Print extracted subtitle text for each video.");

var rootCmd = new RootCommand("EpisodeMatcher — rename ripped TV episodes by matching embedded subtitles to reference SRT files.")
{
    videosArg,
    srtArg,
    windowOpt,
    dryRunOpt,
    forceOpt,
    minScoreOpt,
    verboseOpt
};

rootCmd.SetHandler(Run,
    videosArg, srtArg, windowOpt, dryRunOpt, forceOpt, minScoreOpt, verboseOpt);

return await rootCmd.InvokeAsync(args);

// ---------------------------------------------------------------------------
// Main logic
// ---------------------------------------------------------------------------

static void Run(
    DirectoryInfo videosDir,
    DirectoryInfo srtDir,
    double window,
    bool dryRun,
    bool force,
    double minScore,
    bool verbose)
{
    // --- Validate inputs ----------------------------------------------------
    if (!videosDir.Exists)
    {
        Console.Error.WriteLine($"Error: videos folder not found: {videosDir.FullName}");
        Environment.Exit(1);
    }
    if (!srtDir.Exists)
    {
        Console.Error.WriteLine($"Error: SRT folder not found: {srtDir.FullName}");
        Environment.Exit(1);
    }

    // --- Locate ffmpeg / ffprobe / tesseract --------------------------------
    Console.WriteLine("Locating tools...");
    try   { SubtitleExtractor.LocateTools(); }
    catch (Exception ex) { Console.Error.WriteLine(ex.Message); Environment.Exit(2); }

    // --- Load reference SRTs ------------------------------------------------
    Console.WriteLine($"\nLoading reference SRT files from: {srtDir.FullName}");

    // Accept both plain .srt files and .zip files containing SRTs
    var refFiles = srtDir
        .GetFiles("*.*", SearchOption.TopDirectoryOnly)
        .Where(f => f.Extension.Equals(".srt", StringComparison.OrdinalIgnoreCase) ||
                    f.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
        .ToList();

    if (refFiles.Count == 0)
    {
        Console.Error.WriteLine("No .srt or .zip files found in the SRT folder.");
        Environment.Exit(3);
    }

    var references = refFiles
        .Select(f =>
        {
            var entries    = SrtParser.Parse(f.FullName, window * 2);
            var episodeName = SrtParser.EpisodeNameFrom(f.FullName);
            return new EpisodeReference(f.FullName, episodeName, entries);
        })
        .Where(r => r.Entries.Count > 0)
        .ToList();

    Console.WriteLine($"  Loaded {references.Count} / {refFiles.Count} reference files.");

    if (references.Count == 0)
    {
        Console.Error.WriteLine("None of the reference SRT files contained subtitles in the analysis window.");
        Environment.Exit(3);
    }

    // --- Collect video files ------------------------------------------------
    var videoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".mkv", ".mp4", ".avi", ".m4v", ".mov" };

    var videoFiles = videosDir
        .GetFiles("*.*", SearchOption.TopDirectoryOnly)
        .Where(f => videoExtensions.Contains(f.Extension))
        .Select(f => new VideoFile(f.FullName, f.Name))
        .ToList();

    if (videoFiles.Count == 0)
    {
        Console.Error.WriteLine("No video files found in the videos folder.");
        Environment.Exit(4);
    }

    Console.WriteLine($"\nProcessing {videoFiles.Count} video file(s)...\n");

    // --- Extract, match, report -------------------------------------------
    var matcher = new Matcher { MinAcceptScore = minScore };
    var results = new List<MatchResult>();

    foreach (var video in videoFiles)
    {
        Console.WriteLine($"→ {video.OriginalName}");
        var text = SubtitleExtractor.Extract(video.FilePath, window);

        var result = matcher.Match(video, text, references);

        if (result.BestMatch is not null)
        {
            Console.WriteLine($"  Match: '{result.BestMatch.EpisodeName}'  (score={result.Score:P0})");
            
            if (result.Ambiguous && result.TopCandidates.Count > 1)
            {
                Console.WriteLine("  Top candidates:");
                foreach (var (candidate, idx) in result.TopCandidates.Take(3).Select((c, i) => (c, i)))
                    Console.WriteLine($"    {idx + 1}. {candidate.Episode.EpisodeName} ({candidate.Score:P0})");
            }
        }
        else
        {
            Console.WriteLine($"  No match found (best score={result.Score:P0})");
            if (result.TopCandidates.Count > 0)
            {
                Console.WriteLine("  Top candidates:");
                foreach (var (candidate, idx) in result.TopCandidates.Take(3).Select((c, i) => (c, i)))
                    Console.WriteLine($"    {idx + 1}. {candidate.Episode.EpisodeName} ({candidate.Score:P0})");
            }
        }

        if (verbose && !string.IsNullOrWhiteSpace(result.ExtractedText))
            Console.WriteLine($"  Extracted text: {Truncate(result.ExtractedText, 200)}");

        results.Add(result);
    }

    // --- Rename -------------------------------------------------------------
    Console.WriteLine("\n--- Rename plan ---");
    Renamer.Apply(results, dryRun, force);
}

static string Truncate(string s, int max) =>
    s.Length <= max ? s : s[..max] + "…";
