namespace MIDIPianoJukebox.Pages;

/// <summary>
/// Represents the Index page.
/// </summary>
public partial class Index
{
    /// <summary>
    /// Gets or sets the NavigationManager.
    /// </summary>
    [Inject] NavigationManager NavigationManager { get; set; }

    /// <summary>
    /// Gets or sets the JukeboxService.
    /// </summary>
    [Inject] JukeboxService JukeboxService { get; set; }

    /// <summary>
    /// Gets or sets the Playlist.
    /// </summary>
    [Parameter] public string Playlist { get; set; }

    /// <summary>
    /// Represents the Shuffle state.
    /// </summary>
    bool Shuffle = false;

    /// <summary>
    /// Represents the DataGrid of Tunes.
    /// </summary>
    MudDataGrid<Tune> dg;

    /// <summary>
    /// Represents the last sort order.
    /// </summary>
    string lastSort = string.Empty;

    /// <summary>
    /// Called when the component is initialized.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await JukeboxService.GetJukeboxAsync();
        JukeboxService.ProgressChanged = p => InvokeAsync(StateHasChanged);
        JukeboxService.ReadyToPlayNext += (s, a) => InvokeAsync(() => DoPlayNext(null));
        NavigationManager.LocationChanged += async (s, e) => await DoNavTo();
        await DoNavTo();
    }

    /// <summary>
    /// Navigates to the specified Playlist.
    /// </summary>
    protected async Task DoNavTo()
    {
        if (!JukeboxService.Loaded) return;

        if (string.IsNullOrWhiteSpace(Playlist))
        {
            Playlist = JukeboxService.Playlists
                .OrderByDescending(p => p.Tunes?.Count ?? 0)
                .Select(p => p.Name)
                .FirstOrDefault();
        }

        var p = JukeboxService.Playlists.FirstOrDefault(p => p.Name.Equals(Playlist, StringComparison.CurrentCultureIgnoreCase));
        if (p != null)
        {
            JukeboxService.DequeueAll();
            JukeboxService.EnqueueAll(p.Tunes);
        }
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Handles the RatingChanged event.
    /// </summary>
    protected async Task DoRatingChanged(Tune t, float rating, bool next = false)
    {
        t.Rating = rating;
        JukeboxService.SaveTune(t);
        if (next)
        {
            await DoPlayNext(null);
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Plays the current tune.
    /// </summary>
    protected async Task DoPlay(MouseEventArgs e)
    {
        JukeboxService.ResumePlayer();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Plays the next tune.
    /// </summary>
    protected async Task DoPlayNext(MouseEventArgs e)
    {
        if (dg != null && dg.FilteredItems.Any())
        {
            var key = string.Join(":", dg.SortDefinitions.Select(s => $"{s.Key}{s.Value.Descending}"));
            if (key != lastSort)
            {
                lastSort = key;
                JukeboxService.Queue.Clear();
                JukeboxService.Queue.AddRange(dg.FilteredItems);
            }
        }
        JukeboxService.PlayNext(Shuffle);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Pauses the current tune.
    /// </summary>
    protected async Task DoPause(MouseEventArgs e)
    {
        JukeboxService.PausePlayer();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Skips the current tune.
    /// </summary>
    protected async Task DoSkip(MouseEventArgs e)
    {
        JukeboxService.SkipPlayer(10_000);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Replays the current tune.
    /// </summary>
    protected async Task DoReplay(MouseEventArgs e)
    {
        JukeboxService.SkipPlayerTo(0);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Stops the current tune.
    /// </summary>
    protected async Task DoStop(MouseEventArgs e)
    {
        JukeboxService.StopPlayer();
        await InvokeAsync(StateHasChanged);
    }
}