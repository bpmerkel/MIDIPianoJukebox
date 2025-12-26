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

    /// <summary>
    /// Gets or sets the BrowserViewportService used to observe viewport changes.
    /// </summary>
    [Inject] IBrowserViewportService BrowserViewportService { get; set; }

    /// <summary>
    /// Gets or sets the Playlist.
    /// </summary>
    [Parameter] public string Playlist { get; set; }

    /// <summary>
    /// Gets or sets the Tag.
    /// </summary>
    [Parameter] public string Tag { get; set; }

    /// <summary>
    /// Gets or sets the Instrument.
    /// </summary>
    [Parameter] public string Instrument { get; set; }

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

    /// <summary>
    /// Represents the selected Tunes.
    /// </summary>
    private HashSet<Tune> SelectedTunes { get; set; }

    protected override Task OnParametersSetAsync()
    {
        Tunes = JukeboxService.Tunes;

        var playlist = JukeboxService.Playlists.FirstOrDefault(p => p.Name.Equals(Playlist, StringComparison.CurrentCultureIgnoreCase));
        SelectedTunes = playlist != null
            ? Tunes.Intersect(playlist.Tunes, new Tune()).ToHashSet()   // use intersect logic so SelectedTunes are same objects
            : Tunes.ToHashSet();

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
    /// Filters the tunes based on the Playlist.
    /// </summary>
    /// <param name="tune">The tune to filter.</param>
    /// <returns>True if the tune matches the filter, false otherwise.</returns>
    protected bool QuickFilter(Tune tune)
    {
        if (Playlist != null)
        {
            if (tune.Name?.Contains(Playlist, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                return true;
            }

            if (tune.Tags?.Any(tag => Playlist.Contains(tag, StringComparison.CurrentCultureIgnoreCase)) ?? false)
            {
                return true;
            }

            if (tune.Instruments?.Any(instrument => Playlist.Contains(instrument, StringComparison.CurrentCultureIgnoreCase)) ?? false)
            {
                return true;
            }

            return false;
        }
        return true;
    }

    /// <summary>
    /// Searches for tunes based on the Playlist.
    /// </summary>
    protected void DoSearch()
    {
        if (!string.IsNullOrWhiteSpace(Playlist)
            && !Playlist.StartsWith("tag:", StringComparison.CurrentCultureIgnoreCase)
            && !Playlist.StartsWith("instrument:", StringComparison.CurrentCultureIgnoreCase))
        {
            Tunes = JukeboxService.Tunes
                .Where(t => t.Tags.Any(tag => tag.Contains(Playlist, StringComparison.CurrentCultureIgnoreCase))
                    || (t.Name?.StartsWith(Playlist, StringComparison.CurrentCultureIgnoreCase) ?? false))
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(Tag))
        {
            Playlist = $"tag:{Tag}";
            Tunes = JukeboxService.Tunes
                .Where(t => t.Tags.Any(tag => tag.Contains(Tag, StringComparison.CurrentCultureIgnoreCase)))
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(Instrument))
        {
            Playlist = $"instrument:{Instrument}";
            Tunes = JukeboxService.Tunes
                .Where(t => t.Instruments.Any(i => i.Contains(Instrument, StringComparison.CurrentCultureIgnoreCase)))
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
        Tag = tag;
        DoSearch();
    }

    protected void SelectInstrument(string instrument)
    {
        Instrument = instrument;
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
            JukeboxService.SavePlaylist(new Playlist
            {
                Name = ToTitleCase(Playlist),
                ID = ObjectId.NewObjectId(),
                Tunes = Tunes
            });
            await OnUpdate.InvokeAsync(false);
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
            if (playlist != null)
            {
                var hasdiffs = Tunes.Except(SelectedTunes).Any();
                if (hasdiffs)
                {
                    playlist.Tunes = SelectedTunes.ToList();
                    JukeboxService.SavePlaylist(playlist);
                    await OnUpdate.InvokeAsync(false);
                }
            }
        }
    }

    private async Task DoDeletePlaylist(MouseEventArgs args)
    {
        JukeboxService.ClearPlaylist(Playlist);
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
        var dialog = DialogService.ShowAsync<AlignToPlaylist>("Align", parameters);
        var result = await dialog;
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
        browserHeight -= 64 + 72 + 41 + 50; // subtract heights of app bar, height of player, grid header, height of pager
        var rows = browserHeight / 41; // Assuming each row is approximately 41px tall
        dg.SetRowsPerPageAsync(rows);
        return InvokeAsync(StateHasChanged);
    }
}