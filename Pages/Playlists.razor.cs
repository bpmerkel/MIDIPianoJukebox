namespace MIDIPianoJukebox.Pages;

/// <summary>
/// Represents the Playlists page.
/// </summary>
public partial class Playlists : IBrowserViewportObserver, IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the JukeboxService.
    /// </summary>
    [Inject] JukeboxService JukeboxService { get; set; }

    /// <summary>
    /// Gets or sets the DialogService.
    /// </summary>
    [Inject] IDialogService DialogService { get; set; }

    [Inject] ISnackbar Snackbar { get; set; }

    /// <summary>
    /// Gets or sets the BrowserViewportService used to observe viewport changes.
    /// </summary>
    [Inject] IBrowserViewportService BrowserViewportService { get; set; }

    /// <summary>
    /// Gets or sets the Playlist.
    /// </summary>
    [Parameter] public string Playlist { get; set; }

    /// <summary>
    /// Gets or sets the callback that is invoked when the playlist is updated.
    /// </summary>
    [Parameter] public EventCallback<bool> OnUpdate { get; set; }

    /// <summary>
    /// Represents the DataGrid of Tunes.
    /// </summary>
    private MudDataGrid<Tune> dg;

    /// <summary>
    /// Represents the list of Tunes.
    /// </summary>
    private List<Tune> Tunes { get; set; }

    protected override Task OnParametersSetAsync()
    {
        Tunes = JukeboxService.Tunes;
        return base.OnParametersSetAsync();
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
    /// Filters the visible tunes based on the Playlist.
    /// </summary>
    /// <param name="tune">The tune to filter.</param>
    /// <returns>True if the tune matches the filter, false otherwise.</returns>
    protected bool QuickFilter(Tune tune)
    {
        var show = false;

        if (string.IsNullOrWhiteSpace(Playlist))
        {
            show = true;
        }
        else if (Playlist?.StartsWith("tag:", StringComparison.CurrentCultureIgnoreCase) ?? false)
        {
            var tag = Playlist[4..];
            show = tune.Tags.Any(t => t.Contains(tag, StringComparison.CurrentCultureIgnoreCase));
        }
        else if (Playlist?.StartsWith("instrument:", StringComparison.CurrentCultureIgnoreCase) ?? false)
        {
            var instrument = Playlist[11..];
            show = tune.Instruments.Any(i => i?.Contains(instrument, StringComparison.CurrentCultureIgnoreCase) ?? false);
        }
        else
        {
            show = tune.Tags.Any(tag => tag.Contains(Playlist, StringComparison.CurrentCultureIgnoreCase))
                || tune.Instruments.Any(i => i?.Contains(Playlist, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (tune.Name?.Contains(Playlist, StringComparison.CurrentCultureIgnoreCase) ?? false);
        }

        return show;
    }

    /// <summary>
    /// Searches for tunes based on the Playlist.
    /// </summary>
    protected void DoSearch()
    {
        if (Playlist?.StartsWith("tag:", StringComparison.CurrentCultureIgnoreCase) ?? false)
        {
            var tag = Playlist[4..];
            Tunes = JukeboxService.Tunes
                .Where(t => t.Tags.Any(t => t.Contains(tag, StringComparison.CurrentCultureIgnoreCase)))
                .ToList();
        }
        else if (Playlist?.StartsWith("instrument:", StringComparison.CurrentCultureIgnoreCase) ?? false)
        {
            var instrument = Playlist[11..];
            Tunes = JukeboxService.Tunes
                .Where(t => t.Instruments.Any(i => i?.Contains(instrument, StringComparison.CurrentCultureIgnoreCase) ?? false))
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(Playlist))
        {
            Tunes = JukeboxService.Tunes
                .Where(t => t.Tags.Any(tag => tag.Contains(Playlist, StringComparison.CurrentCultureIgnoreCase))
                    || t.Instruments.Any(i => i?.Contains(Playlist, StringComparison.CurrentCultureIgnoreCase) ?? false)
                    || (t.Name?.Contains(Playlist, StringComparison.CurrentCultureIgnoreCase) ?? false))
                .ToList();
        }
        else
        {
            Tunes = JukeboxService.Tunes;
        }

        StateHasChanged();
    }

    protected void SelectTag(string tag)
    {
        Playlist = $"tag:{tag}";
        DoSearch();
    }

    protected void SelectInstrument(string instrument)
    {
        Playlist = $"instrument:{instrument}";
        DoSearch();
    }

    /// <summary>
    /// Creates a new Playlist.
    /// </summary>
    protected async Task CreatePlaylist()
    {
        // if a new playlist is requested, add it
        if (!string.IsNullOrWhiteSpace(Playlist) && !JukeboxService.Playlists.Any(p => p.Name.Equals(Playlist, StringComparison.CurrentCultureIgnoreCase)))
        {
            var playlist = new Playlist
            {
                Name = ToTitleCase(Playlist),
                ID = ObjectId.NewObjectId(),
                Tunes = Tunes
            };
            JukeboxService.SavePlaylist(playlist);
            Snackbar.Add($"Playlist '{playlist.Name}' created.", Severity.Success);
            await OnUpdate.InvokeAsync(true);
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Save changes to a Playlist.
    /// </summary>
    protected async Task SavePlaylist()
    {
        if (!string.IsNullOrWhiteSpace(Playlist))
        {
            var playlist = JukeboxService.Playlists.FirstOrDefault(p => p.Name.Equals(Playlist, StringComparison.CurrentCultureIgnoreCase));
            playlist ??= new Playlist { Name = Playlist, ID = new ObjectId() };
            playlist.Tunes = dg.FilteredItems.ToList();
            JukeboxService.SavePlaylist(playlist);
            Snackbar.Add($"Playlist '{playlist.Name}' saved.", Severity.Success);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task DoDeletePlaylist(MouseEventArgs args)
    {
        JukeboxService.ClearPlaylist(Playlist);
        Snackbar.Add($"Playlist '{Playlist}' deleted.", Severity.Success);
        await OnUpdate.InvokeAsync(true);
        await InvokeAsync(StateHasChanged);
    }

    private bool PlaylistDoesntExist(string playlist) => !JukeboxService.Playlists.Exists(p => string.Equals(p.Name, playlist, StringComparison.CurrentCultureIgnoreCase));

    /// <summary>
    /// Converts the input string to title case.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The converted string.</returns>
    protected string ToTitleCase(string input) => Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(input);

    /// <summary>
    /// Opens a dialog to align the Playlist.
    /// </summary>
    protected async void OpenDialog()
    {
        var parameters = new DialogParameters<AlignToPlaylist>
        {
            { x => x.Tunes, Tunes }
        };
        await DialogService.ShowAsync<AlignToPlaylist>("Align", parameters);
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

        var browserHeight = browserViewportEventArgs.BrowserWindowSize.Height;
        browserHeight -= 48 + 32 + 68 + 150; // subtract heights of app bar, height of player, grid header, height of pager
        var rows = browserHeight / 90; // Assuming each row is approximately 80px tall
        dg.SetRowsPerPageAsync(rows);
        return InvokeAsync(StateHasChanged);
    }
}