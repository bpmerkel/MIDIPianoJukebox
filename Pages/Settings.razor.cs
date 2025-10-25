namespace MIDIPianoJukebox.Pages;

/// <summary>
/// Represents the settings page of the application.
/// </summary>
public partial class Settings
{
    /// <summary>
    /// Gets or sets the JukeboxService.
    /// </summary>
    [Inject] protected JukeboxService JukeboxService { get; set; }

    /// <summary>
    /// Gets or sets the progress of the database refresh operation.
    /// </summary>
    protected double Progress { get; set; } = 0d;

    /// <summary>
    /// Gets or sets a value indicating whether a database refresh operation is in progress.
    /// </summary>
    protected bool Processing { get; set; } = false;

    /// <summary>
    /// Gets or sets the path to the MIDI files.
    /// </summary>
    protected string MIDIPath { get { return JukeboxService.Settings.MIDIPath; } set { JukeboxService.Settings.MIDIPath = value; } }

    /// <summary>
    /// Initializes the settings page.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await JukeboxService.GetJukeboxAsync();
        await base.OnInitializedAsync();
    }

    /// <summary>
    /// Adds a log message.
    /// </summary>
    /// <param name="msg">The log message.</param>
    async void AddLog(string msg)
    {
        await JukeboxService.AddLog(msg);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Updates the progress of the database refresh operation.
    /// </summary>
    /// <param name="prg">The progress value.</param>
    async void UpdateProgress(double prg)
    {
        Progress = prg;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Performs a database refresh operation.
    /// </summary>
    async Task DoDatabaseRefresh()
    {
        Processing = true;
        JukeboxService.SaveSettings();
        await JukeboxService.RefreshDatabaseAsync(msg => AddLog(msg), prg => UpdateProgress(prg));
        Processing = false;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Changes the output device.
    /// </summary>
    /// <param name="sel">The selected output device.</param>
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