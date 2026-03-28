using System.IO.Compression;
using System.Text.RegularExpressions;

namespace EpisodeMatcherCore;

public static class SrtParser
{
    // Matches SRT timestamps: 00:01:23,456 --> 00:01:25,789
    private static readonly Regex TimestampLine = new(
        @"(\d{2}):(\d{2}):(\d{2})[,.](\d{3})\s*-->\s*(\d{2}):(\d{2}):(\d{2})[,.](\d{3})",
        RegexOptions.Compiled);

    // Strip HTML/ASS tags: <i>, {\\an8}, etc.
    private static readonly Regex TagStripper = new(
        @"<[^>]+>|\{[^}]+\}",
        RegexOptions.Compiled);

    // -----------------------------------------------------------------------
    // Public entry points
    // -----------------------------------------------------------------------

    /// <summary>
    /// Loads SRT entries from a path that may be:
    ///   - a plain .srt file
    ///   - a .zip file containing one or more .srt files
    ///
    /// When a zip contains multiple SRTs, all are merged (allows for
    /// zips that bundle language variants — we just take them all).
    ///
    /// Returns an empty list on failure rather than throwing.
    /// </summary>
    public static List<SrtEntry> Parse(string path, double maxSeconds = double.MaxValue)
    {
        try
        {
            if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                return ParseZip(path, maxSeconds);

            if (!File.Exists(path))
                return new List<SrtEntry>();

            return ParseLines(File.ReadAllLines(path), maxSeconds);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  [warn] SRT parse error in {Path.GetFileName(path)}: {ex.Message}");
            return new List<SrtEntry>();
        }
    }

    /// <summary>
    /// Returns the "episode name" to use for a reference file.
    /// For a zip, uses the zip filename (without extension) unless the zip
    /// contains exactly one SRT, in which case the SRT's stem is used.
    /// </summary>
    public static string EpisodeNameFrom(string path)
    {
        if (!path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return Path.GetFileNameWithoutExtension(path);

        try
        {
            using var zip = ZipFile.OpenRead(path);
            var srts = zip.Entries
                .Where(e => e.Name.EndsWith(".srt", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (srts.Count == 1)
                return Path.GetFileNameWithoutExtension(srts[0].Name);
        }
        catch { /* fall through */ }

        return Path.GetFileNameWithoutExtension(path);
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static List<SrtEntry> ParseZip(string zipPath, double maxSeconds)
    {
        if (!File.Exists(zipPath))
            return new List<SrtEntry>();

        var all = new List<SrtEntry>();

        using var zip = ZipFile.OpenRead(zipPath);

        var srtEntries = zip.Entries
            .Where(e => e.Name.EndsWith(".srt", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (srtEntries.Count == 0)
        {
            Console.Error.WriteLine($"  [warn] No .srt files found inside {Path.GetFileName(zipPath)}");
            return all;
        }

        foreach (var entry in srtEntries)
        {
            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var lines = new List<string>();
            while (reader.ReadLine() is { } line)
                lines.Add(line);

            all.AddRange(ParseLines(lines.ToArray(), maxSeconds));
        }

        return all;
    }

    private static List<SrtEntry> ParseLines(string[] lines, double maxSeconds)
    {
        var entries = new List<SrtEntry>();
        int i = 0;
        string? lastText = null;

        while (i < lines.Length)
        {
            // Skip blank lines / byte-order marks
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i])) i++;
            if (i >= lines.Length) break;

            // Index line (might be missing — tolerate it)
            int index = 0;
            if (int.TryParse(lines[i].Trim(), out int parsedIndex))
            {
                index = parsedIndex;
                i++;
            }

            if (i >= lines.Length) break;

            // Timestamp line
            var tsMatch = TimestampLine.Match(lines[i]);
            if (!tsMatch.Success) { i++; continue; }

            var start = ParseTs(tsMatch, 1);
            var end   = ParseTs(tsMatch, 5);
            i++;

            // Text lines until blank line
            var textLines = new List<string>();
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
            {
                textLines.Add(lines[i].Trim());
                i++;
            }

            if (start.TotalSeconds > maxSeconds) break;

            var text = StripTags(string.Join(" ", textLines));
            if (!string.IsNullOrWhiteSpace(text) && text != lastText)
            {
                entries.Add(new SrtEntry(index, start, end, text));
                lastText = text;
            }
        }

        return entries;
    }

    // -----------------------------------------------------------------------
    // Text utilities (used by Matcher)
    // -----------------------------------------------------------------------

    /// <summary>Joins SRT entries into a single normalised string for comparison.</summary>
    public static string ToComparisonText(IEnumerable<SrtEntry> entries)
        => NormalizeText(string.Join(" ", entries.Select(e => e.Text)));

    public static string NormalizeText(string text)
    {
        text = text.ToLowerInvariant();
        text = TagStripper.Replace(text, "");
        text = Regex.Replace(text, @"[^\w\s']", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }

    private static string StripTags(string text) => TagStripper.Replace(text, "").Trim();

    private static TimeSpan ParseTs(Match m, int g) =>
        new TimeSpan(0,
            int.Parse(m.Groups[g    ].Value),
            int.Parse(m.Groups[g + 1].Value),
            int.Parse(m.Groups[g + 2].Value),
            int.Parse(m.Groups[g + 3].Value));
}
