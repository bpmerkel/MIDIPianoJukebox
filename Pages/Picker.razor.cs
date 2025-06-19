namespace MIDIPianoJukebox.Pages;

/// <summary>
/// Represents the Picker page.
/// </summary>
public partial class Picker
{
    /// <summary>
    /// Gets or sets the list of Playlists.
    /// </summary>
    private List<Playlist> Playlists { get; set; }
    /// <summary>
    /// Gets or sets the JukeboxService.
    /// </summary>
    [Inject] JukeboxService JukeboxService { get; set; }
    /// <summary>
    /// Gets or sets the NavigationManager.
    /// </summary>
    [Inject] NavigationManager NavigationManager { get; set; }
    [Inject] IDialogService DialogService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        DoRefresh();
        await base.OnInitializedAsync();
    }

    protected void NavTo(Playlist p) => NavigationManager.NavigateTo($"/{p.Name}");

    private void DoRefresh()
    {
        Playlists = JukeboxService.Playlists
            .Where(pp => pp.Tunes.Count > 0)
            .OrderBy(pp => pp.Name)
            .ToList();
    }

    // open the Playlist dialog box
    protected async Task AddNew()
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraLarge
        };

        var parameters = new DialogParameters
        {
            { "OnUpdate", EventCallback.Factory.Create(this, DoRefresh) }
        };

        var result = await DialogService.ShowAsync<Playlists>("Add new playlist", parameters, options);
        DoRefresh();
        await InvokeAsync(StateHasChanged);
    }
}