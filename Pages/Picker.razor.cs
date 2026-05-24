namespace MIDIPianoJukebox.Pages;

/// <summary>
/// Represents the Picker page.
/// </summary>
public partial class Picker
{
    /// <summary>
    /// Gets or sets the callback that is invoked when a playlist is added.
    /// </summary>
    [Parameter] public EventCallback OnUpdate { get; set; }

    /// <summary>
    /// Gets or sets the list of Playlists.
    /// </summary>
    [Parameter] public List<Playlist> Playlists { get; set; } = [];

    /// <summary>
    /// Gets or sets the NavigationManager.
    /// </summary>
    [Inject] NavigationManager NavigationManager { get; set; }

    /// <summary>
    /// Gets or sets the DialogService.
    /// </summary>
    [Inject] IDialogService DialogService { get; set; }

    protected void NavTo(Playlist p) => NavigationManager.NavigateTo($"/{p.Name}");

    // open the Playlist dialog box
    protected async Task AddNew()
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraLarge
        };

        var parameters = new DialogParameters
        {
            { "OnUpdate", EventCallback.Factory.Create<bool>(this, async b =>
                {
                    await OnUpdate.InvokeAsync();
                    await InvokeAsync(StateHasChanged);
                })
            }
        };

        var result = await DialogService.ShowAsync<Playlists>("Add new playlist", parameters, options);
        await InvokeAsync(StateHasChanged);
    }
}