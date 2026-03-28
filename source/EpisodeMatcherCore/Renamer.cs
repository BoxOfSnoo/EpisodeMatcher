namespace EpisodeMatcherCore;

public static class Renamer
{
    /// <summary>
    /// Renames (or dry-runs) each matched video file.
    /// The new name is taken from the best-matching SRT filename (minus the .srt extension),
    /// preserving the original video extension.
    /// </summary>
    public static void Apply(IEnumerable<MatchResult> results, bool dryRun, bool force)
    {
        int renamed = 0, skipped = 0, conflicts = 0;

        foreach (var r in results)
        {
            if (r.BestMatch is null)
            {
                Console.WriteLine($"  [skip] {r.Video.OriginalName} — no confident match (score={r.Score:P0})");
                skipped++;
                continue;
            }

            if (r.Ambiguous && !force)
            {
                Console.WriteLine(
                    $"  [ambiguous] {r.Video.OriginalName} — top match '{r.BestMatch.EpisodeName}' " +
                    $"(score={r.Score:P0}) is too close to next candidate. Use --force to rename anyway.");
                skipped++;
                continue;
            }

            var dir      = Path.GetDirectoryName(r.Video.FilePath)!;
            var ext      = Path.GetExtension(r.Video.FilePath);
            var newName  = SanitizeFileName(r.BestMatch.EpisodeName) + ext;
            var newPath  = Path.Combine(dir, newName);

            // Already has the right name
            if (string.Equals(r.Video.FilePath, newPath, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  [ok]   {r.Video.OriginalName} already named correctly.");
                renamed++;
                continue;
            }

            // Collision check
            if (File.Exists(newPath) && !force)
            {
                Console.WriteLine(
                    $"  [conflict] '{newName}' already exists. Use --force to overwrite.");
                conflicts++;
                continue;
            }

            Console.WriteLine(
                $"  [{(dryRun ? "dry-run" : "rename")}] " +
                $"{r.Video.OriginalName}  →  {newName}  (score={r.Score:P0}{(r.Ambiguous ? ", ambiguous!" : "")})");

            if (!dryRun)
            {
                File.Move(r.Video.FilePath, newPath, overwrite: force);
                renamed++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Summary: {renamed} renamed, {skipped} skipped, {conflicts} conflicts.");
        if (dryRun) Console.WriteLine("(Dry-run — no files were changed. Remove --dry-run to apply.)");
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
