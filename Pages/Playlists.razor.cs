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

    [Inject] IBrowserViewportService BrowserViewportService { get; set; }

    /// <summary>
    /// Gets or sets the Playlist.
    /// </summary>
    [Parameter] public string Playlist { get; set; }

    /// <summary>
    /// Gets or sets the Tag.
    /// </summary>
    [Parameter] public string Tag { get; set; }

    [Parameter] public EventCallback OnUpdate { get; set; }

    /// <summary>
    /// Represents the DataGrid of Tunes.
    /// </summary>
    MudDataGrid<Tune> dg;

    /// <summary>
    /// Represents the list of Tunes.
    /// </summary>
    private List<Tune> Tunes { get; set; }

    protected override Task OnInitializedAsync()
    {
        Tunes = JukeboxService.Tunes;
        return base.OnInitializedAsync();
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

            return false;
        }
        return true;
    }

    /// <summary>
    /// Searches for tunes based on the Playlist.
    /// </summary>
    protected void DoSearch()
    {
        if (!string.IsNullOrWhiteSpace(Playlist) && !Playlist.StartsWith("tag:", StringComparison.CurrentCultureIgnoreCase))
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

    /// <summary>
    /// Creates a new Playlist.
    /// </summary>
    protected void CreatePlaylist()
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
            OnUpdate.InvokeAsync();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Clears the current Playlist.
    /// </summary>
    protected void ClearPlaylist()
    {
        JukeboxService.ClearPlaylist(Playlist);
        Playlist = null;
        StateHasChanged();
    }

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