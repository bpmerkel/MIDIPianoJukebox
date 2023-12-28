using Microsoft.AspNetCore.Components;

namespace MIDIPianoJukebox.Pages;

public partial class Settings
{
    [Inject] protected JukeboxService JukeboxService { get; set; }

    protected double Progress { get; set; } = 0d;
    protected bool Processing { get; set; } = false;
    protected bool DoUpdatesToo { get; set; } = true;
    protected string MIDIPath { get { return JukeboxService.Settings.MIDIPath; } set { JukeboxService.Settings.MIDIPath = value; } }

    protected override async Task OnInitializedAsync()
    {
        await JukeboxService.GetJukeboxAsync();
        await base.OnInitializedAsync();
    }

    async void AddLog(string msg)
    {
        await JukeboxService.AddLog(msg);
        await InvokeAsync(StateHasChanged);
    }

    async void UpdateProgress(double prg)
    {
        Progress = prg;
        await InvokeAsync(StateHasChanged);
    }

    async Task DoDatabaseRefresh()
    {
        Processing = true;
        JukeboxService.SaveSettings();
        await JukeboxService.RefreshDatabaseAsync(DoUpdatesToo, msg => AddLog(msg), prg => UpdateProgress(prg));
        Processing = false;
        await InvokeAsync(StateHasChanged);
    }

    void DoChangeOutputDevice(string sel)
    {
        if (sel == "refresh")
        {
            StateHasChanged();
        }
        else
        {
            JukeboxService.Settings.OutputDevice = sel;
            JukeboxService.SaveSettings();
        }
    }
}