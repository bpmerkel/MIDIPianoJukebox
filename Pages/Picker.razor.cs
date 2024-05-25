using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MIDIPianoJukebox.Pages;

/// <summary>
/// Represents the Picker page.
/// </summary>
public partial class Picker
{
    /// <summary>
    /// Gets or sets the list of Playlists.
    /// </summary>
    [Parameter] public List<Playlist> Playlists { get; set; }

    /// <summary>
    /// Gets or sets the base URL.
    /// </summary>
    [Parameter] public string UrlBase { get; set; } = string.Empty;

    /// <summary>
    /// Represents the selected Playlist.
    /// </summary>
    protected MudChip selectedPlaylist;
}
