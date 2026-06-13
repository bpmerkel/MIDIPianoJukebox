namespace MIDIPianoJukebox.Pages;

/// <summary>
/// Represents the Index page.
/// </summary>
public partial class Index: IBrowserViewportObserver, IAsyncDisposable
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
    /// Gets or sets the BrowserViewportService for tracking browser window size changes.
    /// </summary>
    [Inject] IBrowserViewportService BrowserViewportService { get; set; }

    /// <summary>
    /// Gets or sets the DialogService.
    /// </summary>
    [Inject] IDialogService DialogService { get; set; }

    /// <summary>
    /// Gets or sets the Playlist.
    /// </summary>
    [Parameter] public string Playlist { get; set; }

    private List<Playlist> Playlists => JukeboxService.Playlists
        .DistinctBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
        .OrderBy(p => p.Name)
        .ToList();

    /// <summary>
    /// Represents the Shuffle state.
    /// </summary>
    private bool Shuffle = false;

    /// <summary>
    /// Represents the DataGrid of Tunes.
    /// </summary>
    private MudDataGrid<Tune> dg;

    /// <summary>
    /// Represents the last sort order.
    /// </summary>
    private string lastSort = string.Empty;

    /// <summary>
    /// Called when the component is initialized.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await JukeboxService.GetJukeboxAsync();
        JukeboxService.ProgressChanged = p => InvokeAsync(StateHasChanged);
        JukeboxService.ReadyToPlayNext += (s, a) => InvokeAsync(() => DoPlayNext(null));
        NavigationManager.LocationChanged += async (s, e) => await DoNavTo();
        await DoNavTo();
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await BrowserViewportService.SubscribeAsync(this, fireImmediately: true);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    public async ValueTask DisposeAsync()
    {
        await BrowserViewportService.UnsubscribeAsync(this);
        GC.SuppressFinalize(this);
    }

    Guid IBrowserViewportObserver.Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Navigates to the specified Playlist.
    /// </summary>
    protected async Task DoNavTo()
    {
        if (!JukeboxService.Loaded)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Playlist))
        {
            Playlist = Playlists
                .OrderByDescending(p => p.Tunes?.Count ?? 0)
                .Select(p => p.Name)
                .FirstOrDefault();
        }

        var p = Playlists.FirstOrDefault(p => p.Name.Equals(Playlist, StringComparison.CurrentCultureIgnoreCase));
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
    protected async Task DoRatingChanged(Tune tune, float rating, bool next = false)
    {
        tune.Rating = rating;
        JukeboxService.SaveTune(tune);

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
        await JukeboxService.ResumePlayerAsync();
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
        await JukeboxService.PlayNextAsync(Shuffle);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Pauses the current tune.
    /// </summary>
    protected async Task DoPause(MouseEventArgs e)
    {
        await JukeboxService.PausePlayerAsync();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Skips the current tune.
    /// </summary>
    protected async Task DoSkip(MouseEventArgs e)
    {
        await JukeboxService.SkipPlayerAsync(10_000);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Replays the current tune.
    /// </summary>
    protected async Task DoReplay(MouseEventArgs e)
    {
        await JukeboxService.SkipPlayerToAsync(0);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Stops the current tune.
    /// </summary>
    protected async Task DoStop(MouseEventArgs e)
    {
        await JukeboxService.StopPlayerAsync();
        await InvokeAsync(StateHasChanged);
    }

    protected async Task SelectTune(Tune tune)
    {
        if (tune == null)
        {
            return;
        }
        await JukeboxService.PlayAsync(tune);
        await InvokeAsync(StateHasChanged);
    }

    ResizeOptions IBrowserViewportObserver.ResizeOptions { get; } = new()
    {
        ReportRate = 100,
        NotifyOnBreakpointOnly = false
    };

    Task IBrowserViewportObserver.NotifyBrowserViewportChangeAsync(BrowserViewportEventArgs browserViewportEventArgs)
    {
        if (dg == null)
        {
            return Task.CompletedTask;
        }

        //_width = browserViewportEventArgs.BrowserWindowSize.Width;
        var browserHeight = browserViewportEventArgs.BrowserWindowSize.Height;
        browserHeight -= 48 + 32 + 68 + 120; // subtract heights of app bar, height of player, grid header, height of pager
        var rows = browserHeight / 40; // Assuming each row is approximately 40px tall
        dg.SetRowsPerPageAsync(rows);
        return InvokeAsync(StateHasChanged);
    }

    private async Task DoEditPlaylist(MouseEventArgs args)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraLarge, CloseOnEscapeKey = true, CloseButton = true, BackdropClick = true
        };

        var parameters = new DialogParameters
        {
            { "Playlist", Playlist },
            { "OnUpdate", EventCallback.Factory.Create<bool>(this, async b =>
                {
                    if (b)
                    {
                        NavigationManager.NavigateTo("/", true);
                    }
                    else
                    {
                        await DoNavTo();
                    }
                })
            }
        };

        var dialog = await DialogService.ShowAsync<Playlists>("Edit playlist", parameters, options);
        var result = await dialog.Result;

        await InvokeAsync(StateHasChanged);
    }
}