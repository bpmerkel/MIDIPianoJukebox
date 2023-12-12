using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MIDIPianoJukebox.Pages;

public partial class Playlists
{
    [Parameter] public string playlist { get; set; }
    [Parameter] public string tag { get; set; }
    List<Tune> Tunes;

    protected override async Task OnInitializedAsync()
    {
        NavigationManager.LocationChanged += (s, e) => doNavTo();
        await JukeboxService.GetJukeboxAsync();
        doNavTo();
        await base.OnInitializedAsync();
    }

    // quick filter
    bool quickFilter(Tune tune)
    {
        if (playlist != null)
        {
            if (tune.Name?.Contains(playlist, StringComparison.OrdinalIgnoreCase) ?? false)
                return true;
            if (tune.Tags?.Any(tag => playlist.Contains(tag, StringComparison.CurrentCultureIgnoreCase)) ?? false)
                return true;
        }
        return false;
    }

    protected void doNavTo()
    {
        if (!JukeboxService.Loaded) return;

        if (!string.IsNullOrWhiteSpace(tag))
        {
            playlist = tag;
            Tunes = JukeboxService.Tunes
                .Where(t => t.Tags.Any(tag => tag.Equals(tag, StringComparison.CurrentCultureIgnoreCase))
                        || (t.Name?.StartsWith(tag, StringComparison.CurrentCultureIgnoreCase) ?? false))
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(playlist))
        {
            var p = JukeboxService.Playlists.FirstOrDefault(p => p.Name.Equals(playlist, StringComparison.CurrentCultureIgnoreCase));
            if (p != null)
            {
                Tunes = p.Tunes;
            }
            else if (playlist.Equals("all", StringComparison.CurrentCultureIgnoreCase))
            {
                Tunes = JukeboxService.Tunes;
            }
            else if (playlist.Equals("orphan", StringComparison.CurrentCultureIgnoreCase))
            {
                var comparer = new Tune();
                var pltunes = JukeboxService.Playlists.SelectMany(p => p.Tunes).Distinct(comparer);
                Tunes = JukeboxService.Tunes.Except(pltunes, comparer).Where(t => t.Durationms > 0).ToList();
            }
        }
        else
        {
            // default to ALL
            playlist = "All";
            Tunes = JukeboxService.Tunes;
        }

        StateHasChanged();
    }

    protected void doSearch()
    {
        if (string.IsNullOrWhiteSpace(playlist)) return;
        Tunes = JukeboxService.Tunes
            .Where(t => t.Tags.Any(tag => tag.Contains(playlist, StringComparison.CurrentCultureIgnoreCase))
                || (t.Name?.StartsWith(playlist, StringComparison.CurrentCultureIgnoreCase) ?? false))
            .ToList();
        StateHasChanged();
    }

    protected void createPlaylist()
    {
        // if a new playlist is requested, add it
        if (!string.IsNullOrWhiteSpace(playlist) && !JukeboxService.Playlists.Any(p => p.Name.Equals(playlist, StringComparison.CurrentCultureIgnoreCase)))
        {
            var p = new Data.Playlist { Name = toTitleCase(playlist), ID = ObjectId.NewObjectId(), Tunes = Tunes };
            JukeboxService.Playlists.Add(p);
            JukeboxService.SavePlaylist(p);
            StateHasChanged();
        }
    }

    protected void clearPlaylist()
    {
        JukeboxService.ClearPlaylist(playlist);
        playlist = null;
        StateHasChanged();
    }

    protected string toTitleCase(string input) => System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(input);

    protected void openDialog()
    {
        var parameters = new DialogParameters<AlignToPlaylist>();
        parameters.Add(x => x.Tunes, Tunes);
        DialogService.Show<AlignToPlaylist>("Align", parameters);
    }
}
