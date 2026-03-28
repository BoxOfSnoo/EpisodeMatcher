using F23.StringSimilarity;

namespace EpisodeMatcherCore;

/// <summary>
/// Scores video files against episode references using a combination of:
///   - Cosine similarity on normalised word tokens (primary)
///   - Jaro-Winkler as a tiebreaker
///
/// A match is accepted if the best score >= <see cref="MinAcceptScore"/>.
/// A match is flagged as ambiguous if the top two scores are within
/// <see cref="AmbiguityThreshold"/> of each other.
/// </summary>
public class Matcher
{
    public double MinAcceptScore   { get; init; } = 0.20;
    public double AmbiguityThreshold { get; init; } = 0.03;  // Tightened: only flag when top 2 are very close

    private static readonly Cosine    _cosine = new(2);   // bigram cosine
    private static readonly JaroWinkler _jw   = new();

    public MatchResult Match(VideoFile video, string extractedText, List<EpisodeReference> references)
    {
        if (string.IsNullOrWhiteSpace(extractedText) || references.Count == 0)
        {
            return new MatchResult(video, null, 0.0, extractedText, false, new());
        }

        var scored = references
            .Select(ep =>
            {
                var refText = SrtParser.ToComparisonText(ep.Entries);
                if (string.IsNullOrWhiteSpace(refText))
                    return (Episode: ep, Score: 0.0);

                // Cosine on normalised text (primary signal)
                double cosScore = _cosine.Similarity(extractedText, refText);

                // Jaro-Winkler tiebreaker (weight it lightly)
                double jwScore = _jw.Similarity(extractedText, refText);

                double combined = cosScore * 0.85 + jwScore * 0.15;
                return (Episode: ep, Score: combined);
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        var best   = scored[0];
        bool accepted  = best.Score >= MinAcceptScore;
        bool ambiguous = scored.Count > 1 &&
                         (best.Score - scored[1].Score) < AmbiguityThreshold &&
                         accepted;

        var topCandidates = scored
            .Take(3)
            .Select(s => new MatchCandidate(s.Episode, s.Score))
            .ToList();

        return new MatchResult(
            video,
            accepted ? best.Episode : null,
            best.Score,
            extractedText,
            ambiguous,
            topCandidates
        );
    }
}
