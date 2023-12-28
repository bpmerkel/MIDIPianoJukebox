using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MIDIPianoJukebox.Pages;

public partial class AlignToPlaylist
{
    [Inject] protected JukeboxService JukeboxService { get; set; }
    [CascadingParameter] protected MudDialogInstance MudDialog { get; set; }
    [Parameter] public List<Tune> Tunes { get; set; }
    private readonly Dictionary<Playlist, bool> isSelected = [];

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