using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace MIDIPianoJukebox.Pages;

public partial class Index
{
    [Inject] NavigationManager NavigationManager { get; set; }
    [Inject] JukeboxService JukeboxService { get; set; }
    [Parameter] public string Playlist { get; set; }
    bool Shuffle = false;
    MudDataGrid<Tune> dg;
    string lastSort = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await JukeboxService.GetJukeboxAsync();
        JukeboxService.ProgressChanged = p => InvokeAsync(StateHasChanged);
        JukeboxService.ReadyToPlayNext += (s, a) => InvokeAsync(() => DoPlayNext(null));
        NavigationManager.LocationChanged += async (s, e) => await DoNavTo();
        await DoNavTo();
    }

    protected async Task DoNavTo()
    {
        if (!JukeboxService.Loaded) return;

        if (string.IsNullOrWhiteSpace(Playlist))
        {
            Playlist = JukeboxService.Playlists
                .OrderByDescending(p => p.Tunes?.Count ?? 0)
                .Select(p => p.Name)
                .FirstOrDefault();
        }

        var p = JukeboxService.Playlists.FirstOrDefault(p => p.Name.Equals(Playlist, StringComparison.CurrentCultureIgnoreCase));
        if (p != null)
        {
            JukeboxService.DequeueAll();
            JukeboxService.EnqueueAll(p.Tunes);
        }
        await InvokeAsync(StateHasChanged);
    }

    protected async Task DoRatingChanged(Tune t, float rating, bool next = false)
    {
        t.Rating = rating;
        JukeboxService.SaveTune(t);
        if (next)
        {
            await DoPlayNext(null);
            await InvokeAsync(StateHasChanged);
        }
    }

    protected async Task DoPlay(MouseEventArgs e)
    {
        JukeboxService.ResumePlayer();
        await InvokeAsync(StateHasChanged);
    }

    protected async Task DoPlayNext(MouseEventArgs e)
    {
        if (dg != null && dg.FilteredItems.Any())
        {
            var key = string.Join(":", dg.SortDefinitions.Select(s => $"{s.Key}{s.Value.Descending}"));
            if (key != lastSort)
            {
                lastSort = key;
                JukeboxService.Queue.Clear();
                JukeboxService.Queue.AddRange(dg.FilteredItems);
            }
        }
        JukeboxService.PlayNext(Shuffle);
        await InvokeAsync(StateHasChanged);
    }

    protected async Task DoPause(MouseEventArgs e)
    {
        JukeboxService.PausePlayer();
        await InvokeAsync(StateHasChanged);
    }

    protected async Task DoSkip(MouseEventArgs e)
    {
        JukeboxService.SkipPlayer(10_000);
        await InvokeAsync(StateHasChanged);
    }

    protected async Task DoReplay(MouseEventArgs e)
    {
        JukeboxService.SkipPlayerTo(0);
        await InvokeAsync(StateHasChanged);
    }

    protected async Task DoStop(MouseEventArgs e)
    {
        JukeboxService.StopPlayer();
        await InvokeAsync(StateHasChanged);
    }
}