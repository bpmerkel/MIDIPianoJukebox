using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MIDIPianoJukebox.Pages;

/// <summary>
/// Represents the main layout of the application.
/// </summary>
public partial class MainLayout
{
    /// <summary>
    /// Gets or sets the DialogService.
    /// </summary>
    [Inject] IDialogService DialogService { get; set; }

    /// <summary>
    /// Opens the About dialog.
    /// </summary>
    void OpenAboutDialog() => DialogService.Show<About>("About MIDI Piano Jukebox");

    /// <summary>
    /// Opens the Settings dialog.
    /// </summary>
    void OpenSettingsDialog() => DialogService.Show<Settings>("Settings");
}
