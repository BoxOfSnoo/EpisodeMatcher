namespace EpisodeMatcherCore;

/// <summary>Represents a parsed reference SRT episode file.</summary>
public record EpisodeReference(
    string FilePath,
    string EpisodeName,         // Full filename without extension
    List<SrtEntry> Entries
);

/// <summary>One subtitle cue from an SRT file.</summary>
public record SrtEntry(
    int Index,
    TimeSpan Start,
    TimeSpan End,
    string Text                 // Plain text, HTML stripped
);

/// <summary>A video file to be matched and renamed.</summary>
public record VideoFile(
    string FilePath,
    string OriginalName
);

/// <summary>One candidate match with its score.</summary>
public record MatchCandidate(
    EpisodeReference Episode,
    double Score
);

/// <summary>Result of matching a video file to an episode.</summary>
public record MatchResult(
    VideoFile Video,
    EpisodeReference? BestMatch,
    double Score,               // 0.0 – 1.0
    string ExtractedText,
    bool Ambiguous,             // true if two candidates are very close
    List<MatchCandidate> TopCandidates  // Top 3 candidates with scores
);
