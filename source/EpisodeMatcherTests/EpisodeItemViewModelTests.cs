using EpisodeMatcherCore;
using EpisodeMatcherGui.ViewModels;

namespace EpisodeMatcherTests;

public class EpisodeItemViewModelTests
{
    // -------------------------------------------------------------------
    // StatusIcon mapping
    // -------------------------------------------------------------------

    [Theory]
    [InlineData(EpisodeStatus.Pending,    "⏳")]
    [InlineData(EpisodeStatus.Processing, "🔄")]
    [InlineData(EpisodeStatus.Matched,    "✅")]
    [InlineData(EpisodeStatus.Renamed,    "✅")]
    [InlineData(EpisodeStatus.Ambiguous,  "⚠️")]
    [InlineData(EpisodeStatus.NoMatch,    "❌")]
    [InlineData(EpisodeStatus.Error,      "💥")]
    public void StatusIcon_ReturnsCorrectEmoji(EpisodeStatus status, string expected)
    {
        var vm = new EpisodeItemViewModel { Status = status };
        Assert.Equal(expected, vm.StatusIcon);
    }

    // -------------------------------------------------------------------
    // StatusColor mapping
    // -------------------------------------------------------------------

    [Theory]
    [InlineData(EpisodeStatus.Matched,   "#27ae60")]
    [InlineData(EpisodeStatus.Renamed,   "#27ae60")]
    [InlineData(EpisodeStatus.Ambiguous, "#e67e22")]
    [InlineData(EpisodeStatus.NoMatch,   "#e74c3c")]
    [InlineData(EpisodeStatus.Error,     "#e74c3c")]
    [InlineData(EpisodeStatus.Processing,"#3498db")]
    [InlineData(EpisodeStatus.Pending,   "#95a5a6")]
    public void StatusColor_ReturnsCorrectHex(EpisodeStatus status, string expected)
    {
        var vm = new EpisodeItemViewModel { Status = status };
        Assert.Equal(expected, vm.StatusColor);
    }

    // -------------------------------------------------------------------
    // ScoreText formatting
    // -------------------------------------------------------------------

    [Fact]
    public void ScoreText_ZeroScore_ReturnsEmpty()
    {
        var vm = new EpisodeItemViewModel { Score = 0 };
        Assert.Equal("", vm.ScoreText);
    }

    [Fact]
    public void ScoreText_PositiveScore_FormatsAsPercent()
    {
        var vm = new EpisodeItemViewModel { Score = 0.85 };
        Assert.Equal("85%", vm.ScoreText);
    }

    // -------------------------------------------------------------------
    // IsProcessing
    // -------------------------------------------------------------------

    [Fact]
    public void IsProcessing_TrueOnlyWhenProcessing()
    {
        var vm = new EpisodeItemViewModel();

        vm.Status = EpisodeStatus.Processing;
        Assert.True(vm.IsProcessing);

        vm.Status = EpisodeStatus.Matched;
        Assert.False(vm.IsProcessing);
    }

    // -------------------------------------------------------------------
    // SelectCandidate
    // -------------------------------------------------------------------

    [Fact]
    public void SelectCandidate_UpdatesMatchedNameAndScore()
    {
        var vm = new EpisodeItemViewModel
        {
            MatchedName = "OldName",
            Score = 0.50
        };

        var candidate = new MatchCandidate(
            new EpisodeReference("ep.srt", "NewEpisode", new()),
            0.92
        );

        vm.SelectCandidate(candidate);

        Assert.Equal("NewEpisode", vm.MatchedName);
        Assert.Equal(0.92, vm.Score);
        Assert.Equal("NewEpisode", vm.HoveredCandidateText);
    }

    // -------------------------------------------------------------------
    // Property change notifications
    // -------------------------------------------------------------------

    [Fact]
    public void PropertyChanged_FiresOnStatusChange()
    {
        var vm = new EpisodeItemViewModel();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.Status = EpisodeStatus.Matched;

        Assert.Contains("Status", changed);
        Assert.Contains("StatusIcon", changed);
        Assert.Contains("StatusColor", changed);
        Assert.Contains("IsProcessing", changed);
    }

    [Fact]
    public void PropertyChanged_FiresOnScoreChange()
    {
        var vm = new EpisodeItemViewModel();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.Score = 0.75;

        Assert.Contains("Score", changed);
        Assert.Contains("ScoreText", changed);
    }

    // -------------------------------------------------------------------
    // Default values
    // -------------------------------------------------------------------

    [Fact]
    public void Defaults_AreCorrect()
    {
        var vm = new EpisodeItemViewModel();

        Assert.True(vm.IsSelected);
        Assert.Equal(EpisodeStatus.Pending, vm.Status);
        Assert.Equal("", vm.FilePath);
        Assert.Equal("", vm.FileName);
        Assert.Equal("", vm.MatchedName);
        Assert.Equal(0, vm.Score);
    }
}
