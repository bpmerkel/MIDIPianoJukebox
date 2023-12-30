using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MIDIPianoJukebox.Pages;

public partial class Picker
{
    [Parameter] public List<Playlist> Playlists { get; set; }
    [Parameter] public string UrlBase { get; set; } = string.Empty;
    protected MudChip selectedPlaylist;
}