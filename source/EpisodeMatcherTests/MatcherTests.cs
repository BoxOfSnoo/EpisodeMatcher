using EpisodeMatcherCore;

namespace EpisodeMatcherTests;

public class MatcherTests
{
    private static readonly VideoFile TestVideo = new("test.mkv", "test.mkv");

    private static EpisodeReference MakeRef(string name, string text)
        => new(name + ".srt", name, SrtParser.Parse(CreateTempSrt(text)));

    private static EpisodeReference MakeRefDirect(string name, List<SrtEntry> entries)
        => new(name + ".srt", name, entries);

    private static string CreateTempSrt(string dialogueText)
    {
        var lines = dialogueText.Split('\n');
        var srtContent = "";
        for (int i = 0; i < lines.Length; i++)
        {
            var start = TimeSpan.FromSeconds(i * 3);
            var end = start + TimeSpan.FromSeconds(2);
            srtContent += $"{i + 1}\n{start:hh\\:mm\\:ss\\,fff} --> {end:hh\\:mm\\:ss\\,fff}\n{lines[i].Trim()}\n\n";
        }

        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.srt");
        File.WriteAllText(path, srtContent);
        return path;
    }

    // -------------------------------------------------------------------
    // Basic matching behavior
    // -------------------------------------------------------------------

    [Fact]
    public void Match_IdenticalText_ReturnsHighScore()
    {
        var matcher = new Matcher();
        var text = "the quick brown fox jumps over the lazy dog";
        var refs = new List<EpisodeReference>
        {
            MakeRef("Episode1", text)
        };

        var result = matcher.Match(TestVideo, SrtParser.NormalizeText(text), refs);

        Assert.NotNull(result.BestMatch);
        Assert.True(result.Score > 0.90, $"Expected > 0.90 but got {result.Score:F3}");
        Assert.False(result.Ambiguous);
    }

    [Fact]
    public void Match_CompletelyDifferentText_ReturnsLowScore()
    {
        var matcher = new Matcher();
        var refs = new List<EpisodeReference>
        {
            MakeRef("Episode1", "the weather is beautiful today in spring")
        };

        var result = matcher.Match(TestVideo,
            SrtParser.NormalizeText("quantum physics explains particle entanglement"),
            refs);

        Assert.True(result.Score < 0.50, $"Expected < 0.50 but got {result.Score:F3}");
    }

    [Fact]
    public void Match_BelowMinScore_ReturnsNoBestMatch()
    {
        var matcher = new Matcher { MinAcceptScore = 0.80 };
        var refs = new List<EpisodeReference>
        {
            MakeRef("Episode1", "completely unrelated dialogue about cooking recipes and ingredients")
        };

        var result = matcher.Match(TestVideo,
            SrtParser.NormalizeText("space exploration and rocket engineering technology"),
            refs);

        Assert.Null(result.BestMatch);
    }

    [Fact]
    public void Match_SelectsCorrectEpisode_FromMultiple()
    {
        var matcher = new Matcher();
        var extractedText = SrtParser.NormalizeText(
            "i can't believe we're going to the moon said the astronaut");

        var refs = new List<EpisodeReference>
        {
            MakeRef("CookingShow", "today we will make a delicious chocolate cake with cream"),
            MakeRef("SpaceAdventure", "i can't believe we're going to the moon said the astronaut"),
            MakeRef("Drama", "she looked at him and whispered i love you so much"),
        };

        var result = matcher.Match(TestVideo, extractedText, refs);

        Assert.NotNull(result.BestMatch);
        Assert.Equal("SpaceAdventure", result.BestMatch.EpisodeName);
        Assert.False(result.Ambiguous);
    }

    // -------------------------------------------------------------------
    // Ambiguity detection
    // -------------------------------------------------------------------

    [Fact]
    public void Match_SimilarCandidates_FlagsAmbiguous()
    {
        var matcher = new Matcher { AmbiguityThreshold = 0.05 };
        var text = SrtParser.NormalizeText("the hero saves the day in the castle");

        // Two very similar reference texts
        var refs = new List<EpisodeReference>
        {
            MakeRef("EpisodeA", "the hero saves the day in the castle by the river"),
            MakeRef("EpisodeB", "the hero saves the day in the castle near the mountain"),
        };

        var result = matcher.Match(TestVideo, text, refs);

        Assert.True(result.Ambiguous, "Expected ambiguous flag when candidates are close");
    }

    [Fact]
    public void Match_ClearWinner_NotAmbiguous()
    {
        var matcher = new Matcher { AmbiguityThreshold = 0.03 };
        var text = SrtParser.NormalizeText(
            "detective jones examined the evidence at the crime scene carefully");

        var refs = new List<EpisodeReference>
        {
            MakeRef("DetectiveEp", "detective jones examined the evidence at the crime scene carefully"),
            MakeRef("ComedyEp", "the clown juggled three balls and fell off the stage hilariously"),
        };

        var result = matcher.Match(TestVideo, text, refs);

        Assert.NotNull(result.BestMatch);
        Assert.Equal("DetectiveEp", result.BestMatch.EpisodeName);
        Assert.False(result.Ambiguous);
    }

    [Fact]
    public void Match_AmbiguousButBelowMinScore_NotFlagged()
    {
        var matcher = new Matcher { MinAcceptScore = 0.99, AmbiguityThreshold = 0.50 };

        var refs = new List<EpisodeReference>
        {
            MakeRef("A", "some unique text alpha"),
            MakeRef("B", "some unique text beta"),
        };

        var result = matcher.Match(TestVideo, SrtParser.NormalizeText("something else entirely"), refs);

        // Even though scores may be close, if best is below MinAcceptScore -> not ambiguous
        Assert.False(result.Ambiguous);
        Assert.Null(result.BestMatch);
    }

    // -------------------------------------------------------------------
    // Edge cases
    // -------------------------------------------------------------------

    [Fact]
    public void Match_EmptyExtractedText_ReturnsNoMatch()
    {
        var matcher = new Matcher();
        var refs = new List<EpisodeReference>
        {
            MakeRef("Episode1", "some dialogue text")
        };

        var result = matcher.Match(TestVideo, "", refs);

        Assert.Null(result.BestMatch);
        Assert.Equal(0.0, result.Score);
    }

    [Fact]
    public void Match_WhitespaceExtractedText_ReturnsNoMatch()
    {
        var matcher = new Matcher();
        var refs = new List<EpisodeReference>
        {
            MakeRef("Episode1", "some dialogue text")
        };

        var result = matcher.Match(TestVideo, "   \n\t  ", refs);

        Assert.Null(result.BestMatch);
    }

    [Fact]
    public void Match_EmptyReferences_ReturnsNoMatch()
    {
        var matcher = new Matcher();
        var result = matcher.Match(TestVideo, "hello world", new List<EpisodeReference>());

        Assert.Null(result.BestMatch);
        Assert.Equal(0.0, result.Score);
    }

    [Fact]
    public void Match_SingleReference_NeverAmbiguous()
    {
        var matcher = new Matcher();
        var text = SrtParser.NormalizeText("the story begins in a far away land");
        var refs = new List<EpisodeReference>
        {
            MakeRef("OnlyEpisode", "the story begins in a far away land of wonder")
        };

        var result = matcher.Match(TestVideo, text, refs);

        Assert.False(result.Ambiguous, "Single reference should never be ambiguous");
    }

    // -------------------------------------------------------------------
    // TopCandidates
    // -------------------------------------------------------------------

    [Fact]
    public void Match_ReturnsTopCandidates_OrderedByScore()
    {
        var matcher = new Matcher();
        var text = SrtParser.NormalizeText("the robot walked through the city streets at night");

        var refs = new List<EpisodeReference>
        {
            MakeRef("EpA", "birds flew over the ocean waves at sunrise"),
            MakeRef("EpB", "the robot walked through the city streets at night slowly"),
            MakeRef("EpC", "a cat sat on a warm windowsill in winter"),
        };

        var result = matcher.Match(TestVideo, text, refs);

        Assert.Equal(3, result.TopCandidates.Count);
        Assert.True(result.TopCandidates[0].Score >= result.TopCandidates[1].Score);
        Assert.True(result.TopCandidates[1].Score >= result.TopCandidates[2].Score);
    }

    [Fact]
    public void Match_TopCandidates_CappedAtThree()
    {
        var matcher = new Matcher();
        var text = SrtParser.NormalizeText("testing candidate count");

        var refs = Enumerable.Range(1, 5)
            .Select(i => MakeRef($"Ep{i}", $"testing candidate count variant {i}"))
            .ToList();

        var result = matcher.Match(TestVideo, text, refs);

        Assert.Equal(3, result.TopCandidates.Count);
    }

    // -------------------------------------------------------------------
    // Threshold configuration
    // -------------------------------------------------------------------

    [Fact]
    public void Match_CustomMinScore_RespectedCorrectly()
    {
        var strict = new Matcher { MinAcceptScore = 0.95 };
        var lenient = new Matcher { MinAcceptScore = 0.10 };

        var text = SrtParser.NormalizeText("partially matching text for the test");
        var refs = new List<EpisodeReference>
        {
            MakeRef("Episode1", "partially matching text with some differences added")
        };

        var strictResult = strict.Match(TestVideo, text, refs);
        var lenientResult = lenient.Match(TestVideo, text, refs);

        // Same score, different acceptance
        Assert.Equal(strictResult.Score, lenientResult.Score, precision: 5);

        // Lenient should accept, strict may reject
        if (lenientResult.Score >= 0.10 && lenientResult.Score < 0.95)
        {
            Assert.NotNull(lenientResult.BestMatch);
            Assert.Null(strictResult.BestMatch);
        }
    }
}
