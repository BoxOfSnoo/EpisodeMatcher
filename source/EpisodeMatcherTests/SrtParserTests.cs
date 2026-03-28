using EpisodeMatcherCore;

namespace EpisodeMatcherTests;

public class SrtParserTests
{
    // -------------------------------------------------------------------
    // NormalizeText
    // -------------------------------------------------------------------

    [Fact]
    public void NormalizeText_LowercasesInput()
    {
        Assert.Equal("hello world", SrtParser.NormalizeText("Hello WORLD"));
    }

    [Fact]
    public void NormalizeText_StripsHtmlTags()
    {
        Assert.Equal("hello world", SrtParser.NormalizeText("<i>Hello</i> <b>World</b>"));
    }

    [Fact]
    public void NormalizeText_StripsAssTags()
    {
        Assert.Equal("subtitle text", SrtParser.NormalizeText("{\\an8}Subtitle text"));
    }

    [Fact]
    public void NormalizeText_RemovesPunctuation_PreservesApostrophes()
    {
        var result = SrtParser.NormalizeText("Don't stop! It's working...");
        Assert.Equal("don't stop it's working", result);
    }

    [Fact]
    public void NormalizeText_CollapsesWhitespace()
    {
        Assert.Equal("a b c", SrtParser.NormalizeText("  a   b   c  "));
    }

    [Fact]
    public void NormalizeText_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal("", SrtParser.NormalizeText(""));
    }

    [Fact]
    public void NormalizeText_OnlyTags_ReturnsEmpty()
    {
        Assert.Equal("", SrtParser.NormalizeText("<i></i>{\\an8}"));
    }

    // -------------------------------------------------------------------
    // ToComparisonText
    // -------------------------------------------------------------------

    [Fact]
    public void ToComparisonText_JoinsAndNormalizes()
    {
        var entries = new List<SrtEntry>
        {
            new(1, TimeSpan.Zero, TimeSpan.FromSeconds(2), "Hello World!"),
            new(2, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), "How are you?"),
        };

        var result = SrtParser.ToComparisonText(entries);

        Assert.Equal("hello world how are you", result);
    }

    [Fact]
    public void ToComparisonText_EmptyEntries_ReturnsEmpty()
    {
        var result = SrtParser.ToComparisonText(new List<SrtEntry>());
        Assert.Equal("", result);
    }

    // -------------------------------------------------------------------
    // Parse (with temp files)
    // -------------------------------------------------------------------

    [Fact]
    public void Parse_ValidSrt_ReturnsEntries()
    {
        var srt = "1\n00:00:01,000 --> 00:00:03,000\nHello World\n\n2\n00:00:04,000 --> 00:00:06,000\nGoodbye World\n";
        var path = WriteTempSrt(srt);

        try
        {
            var entries = SrtParser.Parse(path);
            Assert.Equal(2, entries.Count);
            Assert.Equal("Hello World", entries[0].Text);
            Assert.Equal("Goodbye World", entries[1].Text);
            Assert.Equal(1, entries[0].Index);
            Assert.Equal(2, entries[1].Index);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_TimestampsAreParsedCorrectly()
    {
        var srt = "1\n01:23:45,678 --> 02:34:56,789\nTest\n";
        var path = WriteTempSrt(srt);

        try
        {
            var entries = SrtParser.Parse(path);
            Assert.Single(entries);

            var e = entries[0];
            Assert.Equal(new TimeSpan(0, 1, 23, 45, 678), e.Start);
            Assert.Equal(new TimeSpan(0, 2, 34, 56, 789), e.End);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_DotSeparator_AlsoWorks()
    {
        // Some SRTs use period instead of comma for milliseconds
        var srt = "1\n00:00:01.000 --> 00:00:03.500\nDot format\n";
        var path = WriteTempSrt(srt);

        try
        {
            var entries = SrtParser.Parse(path);
            Assert.Single(entries);
            Assert.Equal("Dot format", entries[0].Text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_MultiLineDialogue_JoinedWithSpace()
    {
        var srt = "1\n00:00:01,000 --> 00:00:03,000\nLine one\nLine two\n\n";
        var path = WriteTempSrt(srt);

        try
        {
            var entries = SrtParser.Parse(path);
            Assert.Single(entries);
            Assert.Equal("Line one Line two", entries[0].Text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_MissingIndex_StillParses()
    {
        // No numeric index line before timestamp
        var srt = "00:00:01,000 --> 00:00:03,000\nNo index line\n\n";
        var path = WriteTempSrt(srt);

        try
        {
            var entries = SrtParser.Parse(path);
            Assert.Single(entries);
            Assert.Equal("No index line", entries[0].Text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_HtmlTags_StrippedFromText()
    {
        var srt = "1\n00:00:01,000 --> 00:00:03,000\n<i>Italic text</i>\n\n";
        var path = WriteTempSrt(srt);

        try
        {
            var entries = SrtParser.Parse(path);
            Assert.Single(entries);
            Assert.Equal("Italic text", entries[0].Text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_MaxSeconds_TruncatesEntries()
    {
        var srt =
            "1\n00:00:01,000 --> 00:00:02,000\nFirst\n\n" +
            "2\n00:00:05,000 --> 00:00:06,000\nSecond\n\n" +
            "3\n00:00:15,000 --> 00:00:16,000\nThird\n\n";
        var path = WriteTempSrt(srt);

        try
        {
            var entries = SrtParser.Parse(path, maxSeconds: 10);
            Assert.Equal(2, entries.Count);
            Assert.Equal("First", entries[0].Text);
            Assert.Equal("Second", entries[1].Text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsEmptyList()
    {
        var path = WriteTempSrt("");

        try
        {
            var entries = SrtParser.Parse(path);
            Assert.Empty(entries);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_NonexistentFile_ReturnsEmptyList()
    {
        var entries = SrtParser.Parse(@"C:\nonexistent\file.srt");
        Assert.Empty(entries);
    }

    [Fact]
    public void Parse_ConsecutiveDuplicateCues_Deduplicated()
    {
        var srt =
            "1\n00:00:01,000 --> 00:00:02,000\nHello World\n\n" +
            "2\n00:00:03,000 --> 00:00:04,000\nHello World\n\n" +
            "3\n00:00:05,000 --> 00:00:06,000\nHello World\n\n" +
            "4\n00:00:07,000 --> 00:00:08,000\nDifferent text\n\n";
        var path = WriteTempSrt(srt);

        try
        {
            var entries = SrtParser.Parse(path);
            Assert.Equal(2, entries.Count);
            Assert.Equal("Hello World", entries[0].Text);
            Assert.Equal("Different text", entries[1].Text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_MalformedTimestamp_SkipsEntry()
    {
        var srt =
            "1\nNOT A TIMESTAMP\nBad entry\n\n" +
            "2\n00:00:01,000 --> 00:00:02,000\nGood entry\n\n";
        var path = WriteTempSrt(srt);

        try
        {
            var entries = SrtParser.Parse(path);
            Assert.Single(entries);
            Assert.Equal("Good entry", entries[0].Text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_BlankTextLines_Skipped()
    {
        // Entry with only whitespace text should be omitted
        var srt = "1\n00:00:01,000 --> 00:00:02,000\n   \n\n2\n00:00:03,000 --> 00:00:04,000\nReal text\n\n";
        var path = WriteTempSrt(srt);

        try
        {
            var entries = SrtParser.Parse(path);
            Assert.Single(entries);
            Assert.Equal("Real text", entries[0].Text);
        }
        finally { File.Delete(path); }
    }

    // -------------------------------------------------------------------
    // EpisodeNameFrom
    // -------------------------------------------------------------------

    [Fact]
    public void EpisodeNameFrom_SrtFile_ReturnsFileNameWithoutExtension()
    {
        Assert.Equal("My Episode S01E03", SrtParser.EpisodeNameFrom(@"C:\subs\My Episode S01E03.srt"));
    }

    [Fact]
    public void EpisodeNameFrom_NonZipFile_ReturnsFileNameWithoutExtension()
    {
        Assert.Equal("episode", SrtParser.EpisodeNameFrom(@"D:\folder\episode.txt"));
    }

    // -------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------

    private static string WriteTempSrt(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.srt");
        File.WriteAllText(path, content);
        return path;
    }
}
