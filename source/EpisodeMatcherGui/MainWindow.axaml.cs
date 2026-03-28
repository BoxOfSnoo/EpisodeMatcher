using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using EpisodeMatcherCore;
using EpisodeMatcherGui.ViewModels;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;

namespace EpisodeMatcherGui;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;

        // Initialize LibVLC
        Core.Initialize();
        _libVlc = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVlc);
        
        var videoView = this.FindControl<VideoView>("VideoView");
        if (videoView is not null)
            videoView.MediaPlayer = _mediaPlayer;

        // Wire up drag-and-drop on the drop zone border
        var dropZone = this.FindControl<Border>("DropZone")!;
        DragDrop.SetAllowDrop(dropZone, true);
        dropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        dropZone.AddHandler(DragDrop.DropEvent, OnDrop);

        // Also allow drop anywhere on the window
        DragDrop.SetAllowDrop(this, true);
        this.AddHandler(DragDrop.DropEvent, OnDrop);

        // Watch for video path changes - just stop if cleared
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_vm.SelectedVideoPath) && _mediaPlayer is not null)
            {
                if (string.IsNullOrEmpty(_vm.SelectedVideoPath))
                {
                    _mediaPlayer.Stop();
                }
            }
        };

        // Cleanup when window closes
        Closing += (s, e) =>
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVlc?.Dispose();
        };
    }

    // -----------------------------------------------------------------------
    // Video preview
    // -----------------------------------------------------------------------

    private void OnPlayPreview(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_vm.SelectedVideoPath) && _mediaPlayer is not null && _libVlc is not null)
        {
            var media = new Media(_libVlc, _vm.SelectedVideoPath, FromType.FromPath);
            _mediaPlayer.Play(media);
        }
    }

    private void OnStopPreview(object? sender, RoutedEventArgs e)
    {
        _mediaPlayer?.Stop();
    }

    // -----------------------------------------------------------------------
    // Drag & Drop
    // -----------------------------------------------------------------------

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;

        var files = e.Data.GetFiles();
        if (files is null) return;

        var paths = files
            .Select(f => f.TryGetLocalPath())
            .OfType<string>();

        // Expand any dropped folders
        var expanded = paths.SelectMany(p =>
            Directory.Exists(p)
                ? Directory.GetFiles(p, "*.*", SearchOption.TopDirectoryOnly)
                : (IEnumerable<string>)new[] { p });

        _vm.AddFiles(expanded);
        e.Handled = true;
    }

    // -----------------------------------------------------------------------
    // Button handlers
    // -----------------------------------------------------------------------

    private async void OnAddFiles(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Add video files",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Video files")
                {
                    Patterns = new[] { "*.mkv", "*.mp4", "*.avi", "*.m4v", "*.mov" }
                }
            }
        });

        _vm.AddFiles(files
            .Select(f => f.TryGetLocalPath())
            .OfType<string>());
    }

    private async void OnBrowseSrt(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select reference SRT folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
            _vm.SrtFolder = folders[0].TryGetLocalPath() ?? "";
    }

    private void OnClearAll(object? sender, RoutedEventArgs e)       => _vm.ClearAll();
    private void OnRemoveUnchecked(object? sender, RoutedEventArgs e) => _vm.RemoveSelected();
    private void OnRefreshTools(object? sender, RoutedEventArgs e)    => _vm.RefreshToolStatus();
    private void OnCancel(object? sender, RoutedEventArgs e)          => _vm.Cancel();

    private async void OnRun(object? sender, RoutedEventArgs e)
    {
        await _vm.RunAsync();
    }

    // -----------------------------------------------------------------------
    // Episode selection for preview
    // -----------------------------------------------------------------------

    private void OnEpisodeSelected(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border) return;
        if (border.DataContext is not EpisodeItemViewModel ep) return;

        // Highlight selected row
        if (_lastSelectedBorder is not null)
            _lastSelectedBorder.Background = new SolidColorBrush(Color.Parse("#1e1e2e"));
        
        border.Background = new SolidColorBrush(Color.Parse("#3f4754"));
        _lastSelectedBorder = border;
        
        _vm.SelectedVideoPath = ep.FilePath;
    }

    private Border? _lastSelectedBorder;

    private void OnSelectCandidate(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.Tag is not MatchCandidate candidate) return;

        // Walk up the visual tree to find the parent Border with the DataContext (episode row)
        var parent = button.Parent;
        while (parent is not null)
        {
            if (parent is Border border && border.DataContext is EpisodeItemViewModel ep)
            {
                ep.SelectCandidate(candidate);
                return;
            }
            parent = (parent as Control)?.Parent;
        }
    }

    private void OnCandidatePointerEnter(object? sender, PointerEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.Tag is not MatchCandidate candidate) return;

        // Find the episode item and update hover text with SRT snippet
        var parent = button.Parent;
        while (parent is not null)
        {
            if (parent is Grid grid && grid.DataContext is EpisodeItemViewModel ep)
            {
                ep.HoveredCandidateText = GetSrtSnippet(candidate.Episode);
                break;
            }
            parent = (parent as Control)?.Parent;
        }
    }

    private void OnCandidatePointerLeave(object? sender, PointerEventArgs e)
    {
        if (sender is not Button button) return;

        // Find the episode item and clear hover text
        var parent = button.Parent;
        while (parent is not null)
        {
            if (parent is Grid grid && grid.DataContext is EpisodeItemViewModel ep)
            {
                ep.HoveredCandidateText = "";
                break;
            }
            parent = (parent as Control)?.Parent;
        }
    }

    private static string GetSrtSnippet(EpisodeReference episode, int maxLength = 150)
    {
        if (episode.Entries.Count == 0) return episode.EpisodeName;
        
        var text = string.Join(" ", episode.Entries.Select(e => e.Text))
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Trim();
        
        return text.Length > maxLength ? text[..maxLength] + "…" : text;
    }
}
