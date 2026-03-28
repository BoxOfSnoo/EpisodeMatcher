using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EpisodeMatcherCore;

namespace EpisodeMatcherGui.ViewModels;

public enum EpisodeStatus
{
    Pending,
    Processing,
    Matched,
    Renamed,
    Ambiguous,
    NoMatch,
    Error
}

public class EpisodeItemViewModel : INotifyPropertyChanged
{
    private string _filePath = "";
    private string _fileName = "";
    private EpisodeStatus _status = EpisodeStatus.Pending;
    private string _matchedName = "";
    private double _score;
    private string _statusText = "Pending";
    private bool _isSelected = true;
    private string _extractedTextSnippet = "";
    private string _extractedText = "";
    private string _candidatesText = "";
    private List<MatchCandidate> _topCandidates = new();
    private string _hoveredCandidateText = "";
    private MatchCandidate? _selectedCandidate;

    public string FilePath
    {
        get => _filePath;
        set { _filePath = value; OnPropertyChanged(); }
    }

    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }

    public EpisodeStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusIcon));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(IsProcessing));
        }
    }

    public string MatchedName
    {
        get => _matchedName;
        set { _matchedName = value; OnPropertyChanged(); }
    }

    public double Score
    {
        get => _score;
        set { _score = value; OnPropertyChanged(); OnPropertyChanged(nameof(ScoreText)); }
    }

    public string ScoreText => _score > 0 ? $"{_score:P0}" : "";

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public string ExtractedTextSnippet
    {
        get => _extractedTextSnippet;
        set { _extractedTextSnippet = value; OnPropertyChanged(); }
    }

    public string ExtractedText
    {
        get => _extractedText;
        set { _extractedText = value; OnPropertyChanged(); }
    }

    public string CandidatesText
    {
        get => _candidatesText;
        set { _candidatesText = value; OnPropertyChanged(); }
    }

    public List<MatchCandidate> TopCandidates
    {
        get => _topCandidates;
        set { _topCandidates = value; OnPropertyChanged(); }
    }

    public string HoveredCandidateText
    {
        get => _hoveredCandidateText;
        set { _hoveredCandidateText = value; OnPropertyChanged(); }
    }

    public MatchCandidate? SelectedCandidate
    {
        get => _selectedCandidate;
        set { _selectedCandidate = value; OnPropertyChanged(); }
    }

    public void SelectCandidate(MatchCandidate candidate)
    {
        SelectedCandidate = candidate;
        MatchedName = candidate.Episode.EpisodeName;
        Score = candidate.Score;
        HoveredCandidateText = candidate.Episode.EpisodeName;
        
        // Show what the file will be renamed to
        var ext = System.IO.Path.GetExtension(FileName);
        StatusText = $"→ {candidate.Episode.EpisodeName}{ext}";
    }

    public bool IsProcessing => Status == EpisodeStatus.Processing;

    public string StatusIcon => Status switch
    {
        EpisodeStatus.Pending    => "⏳",
        EpisodeStatus.Processing => "🔄",
        EpisodeStatus.Matched    => "✅",
        EpisodeStatus.Renamed    => "✅",
        EpisodeStatus.Ambiguous  => "⚠️",
        EpisodeStatus.NoMatch    => "❌",
        EpisodeStatus.Error      => "💥",
        _                        => "❓"
    };

    public string StatusColor => Status switch
    {
        EpisodeStatus.Matched or EpisodeStatus.Renamed => "#27ae60",
        EpisodeStatus.Ambiguous                         => "#e67e22",
        EpisodeStatus.NoMatch or EpisodeStatus.Error    => "#e74c3c",
        EpisodeStatus.Processing                        => "#3498db",
        _                                               => "#95a5a6"
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
