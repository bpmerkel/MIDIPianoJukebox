using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MIDIPianoJukebox.Pages;

public partial class Playlists
{
    [Inject] NavigationManager NavigationManager { get; set; }
    [Inject] JukeboxService JukeboxService { get; set; }
    [Inject] IDialogService DialogService { get; set; }

    [Parameter] public string Playlist { get; set; }
    [Parameter] public string Tag { get; set; }
    List<Tune> Tunes;

    protected override async Task OnInitializedAsync()
    {
        NavigationManager.LocationChanged += (s, e) => DoNavTo();
        await JukeboxService.GetJukeboxAsync();
        DoNavTo();
        await base.OnInitializedAsync();
    }

    // quick filter
    bool QuickFilter(Tune tune)
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

    protected void DoSearch()
    {
        if (string.IsNullOrWhiteSpace(Playlist)) return;
        Tunes = JukeboxService.Tunes
            .Where(t => t.Tags.Any(tag => tag.Contains(Playlist, StringComparison.CurrentCultureIgnoreCase))
                || (t.Name?.StartsWith(Playlist, StringComparison.CurrentCultureIgnoreCase) ?? false))
            .ToList();
        StateHasChanged();
    }

    protected void CreatePlaylist()
    {
        // if a new playlist is requested, add it
        if (!string.IsNullOrWhiteSpace(Playlist) && !JukeboxService.Playlists.Any(p => p.Name.Equals(Playlist, StringComparison.CurrentCultureIgnoreCase)))
        {
            var p = new Data.Playlist { Name = ToTitleCase(Playlist), ID = ObjectId.NewObjectId(), Tunes = Tunes };
            JukeboxService.Playlists.Add(p);
            JukeboxService.SavePlaylist(p);
            StateHasChanged();
        }
    }

    protected void ClearPlaylist()
    {
        JukeboxService.ClearPlaylist(Playlist);
        Playlist = null;
        StateHasChanged();
    }

    protected string ToTitleCase(string input) => Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(input);

    protected void OpenDialog()
    {
        var parameters = new DialogParameters<AlignToPlaylist>
        {
            { x => x.Tunes, Tunes }
        };
        DialogService.Show<AlignToPlaylist>("Align", parameters);
    }
}
