namespace MIDIPianoJukebox.Pages;

/// <summary>
/// Represents the AlignToPlaylist page.
/// </summary>
public partial class AlignToPlaylist
{
    /// <summary>
    /// Gets or sets the JukeboxService.
    /// </summary>
    [Inject] protected JukeboxService JukeboxService { get; set; }

    /// <summary>
    /// Gets or sets the MudDialogInstance.
    /// </summary>
    [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; }

    /// <summary>
    /// Gets or sets the list of Tunes.
    /// </summary>
    [Parameter] public List<Tune> Tunes { get; set; }

    /// <summary>
    /// Represents the selected Playlists.
    /// </summary>
    private readonly Dictionary<Playlist, bool> IsSelected = new();

    /// <summary>
    /// Saves the selected Playlists.
    /// </summary>
    protected async Task DoSavePlaylist()
    {
        if (Tunes.Count == 0)
        {
            return;
        }

        // get each playlist in IsSelected and add all tunes to them
        foreach (var entry in IsSelected)
        {
            var playlist = JukeboxService.Playlists.First(p => p.ID == entry.Key.ID);

            if (entry.Value)
            {
                playlist.Tunes = playlist.Tunes.Union(Tunes, new Tune()).OrderBy(t => t.Name).ToList();
            }
            else
            {
                // subtract from these!
                playlist.Tunes = playlist.Tunes.Except(Tunes, new Tune()).OrderBy(t => t.Name).ToList();
            }

            JukeboxService.SavePlaylist(playlist);

            // TODO: signal caller to update playlist list
        }

        // clear the IsSelected list
        IsSelected.Clear();
        MudDialog.Close(DialogResult.Ok(true));
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Toggles the selected state of a Playlist.
    /// </summary>
    /// <param name="playlist">The Playlist to toggle.</param>
    protected async Task ToggleSelected(Playlist playlist)
    {
        if (IsSelected.TryGetValue(playlist, out bool value))
        {
            IsSelected[playlist] = !value;
        }
        else
        {
            IsSelected.Add(playlist, true);
        }

        await InvokeAsync(StateHasChanged);
    }
}