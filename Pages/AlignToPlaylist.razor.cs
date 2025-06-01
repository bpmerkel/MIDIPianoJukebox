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
    private readonly Dictionary<Playlist, bool> isSelected = [];

    /// <summary>
    /// Saves the selected Playlists.
    /// </summary>
    protected void DoSavePlaylist()
    {
        if (Tunes.Count == 0) return;

        // get each playlist in isSelected and add all tunes to them
        foreach (var entry in isSelected)
        {
            var playlist = JukeboxService.Playlists.FirstOrDefault(p => p.ID == entry.Key.ID);
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
        }

        // clear the isSelected list
        isSelected.Clear();
        MudDialog.Close(DialogResult.Ok(true));
        StateHasChanged();
    }

    /// <summary>
    /// Toggles the selected state of a Playlist.
    /// </summary>
    /// <param name="playlist">The Playlist to toggle.</param>
    protected void ToggleSelected(Data.Playlist playlist)
    {
        if (isSelected.TryGetValue(playlist, out bool value))
        {
            isSelected[playlist] = isSelected[playlist] = !value;
        }
        else
        {
            isSelected.Add(playlist, true);
        }
        StateHasChanged();
    }
}