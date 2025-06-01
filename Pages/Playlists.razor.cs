namespace MIDIPianoJukebox.Pages;

/// <summary>
/// Represents the Playlists page.
/// </summary>
public partial class Playlists
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
    /// Gets or sets the DialogService.
    /// </summary>
    [Inject] IDialogService DialogService { get; set; }

    /// <summary>
    /// Gets or sets the Playlist.
    /// </summary>
    [Parameter] public string Playlist { get; set; }

    /// <summary>
    /// Gets or sets the Tag.
    /// </summary>
    [Parameter] public string Tag { get; set; }

    /// <summary>
    /// Represents the list of Tunes.
    /// </summary>
    private List<Tune> Tunes;

    /// <summary>
    /// Called when the component is initialized.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        NavigationManager.LocationChanged += (s, e) => DoNavTo();
        await JukeboxService.GetJukeboxAsync();
        DoNavTo();
        await base.OnInitializedAsync();
    }

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
                return true;
            if (tune.Tags?.Any(tag => Playlist.Contains(tag, StringComparison.CurrentCultureIgnoreCase)) ?? false)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Navigates to the specified Playlist or Tag.
    /// </summary>
    protected void DoNavTo()
    {
        if (!JukeboxService.Loaded) return;

        if (!string.IsNullOrWhiteSpace(Tag))
        {
            Playlist = Tag;
            Tunes = JukeboxService.Tunes
                .Where(t => t.Tags.Any(tag => tag.Equals(tag, StringComparison.CurrentCultureIgnoreCase))
                        || (t.Name?.StartsWith(Tag, StringComparison.CurrentCultureIgnoreCase) ?? false))
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(Playlist))
        {
            var p = JukeboxService.Playlists.FirstOrDefault(p => p.Name.Equals(Playlist, StringComparison.CurrentCultureIgnoreCase));
            if (p != null)
            {
                Tunes = p.Tunes;
            }
            else if (Playlist.Equals("all", StringComparison.CurrentCultureIgnoreCase))
            {
                Tunes = JukeboxService.Tunes;
            }
            else if (Playlist.Equals("orphan", StringComparison.CurrentCultureIgnoreCase))
            {
                var comparer = new Tune();
                var pltunes = JukeboxService.Playlists.SelectMany(p => p.Tunes).Distinct(comparer);
                Tunes = JukeboxService.Tunes.Except(pltunes, comparer).Where(t => t.Durationms > 0).ToList();
            }
        }
        else
        {
            // default to ALL
            Playlist = "All";
            Tunes = JukeboxService.Tunes;
        }

        StateHasChanged();
    }

    /// <summary>
    /// Searches for tunes based on the Playlist.
    /// </summary>
    protected void DoSearch()
    {
        if (string.IsNullOrWhiteSpace(Playlist)) return;
        Tunes = JukeboxService.Tunes
            .Where(t => t.Tags.Any(tag => tag.Contains(Playlist, StringComparison.CurrentCultureIgnoreCase))
                || (t.Name?.StartsWith(Playlist, StringComparison.CurrentCultureIgnoreCase) ?? false))
            .ToList();
        StateHasChanged();
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
}