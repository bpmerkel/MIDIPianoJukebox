using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MIDIPianoJukebox.Pages;

public partial class MainLayout
{
    [Inject] IDialogService DialogService { get; set; }

    void OpenAboutDialog() => DialogService.Show<About>("About MIDI Piano Jukebox");
    void OpenSettingsDialog() => DialogService.Show<Settings>("Settings");
}