namespace MIDIPianoJukebox.Pages;

/// <summary>
/// Represents a list of tags for a tune in the application.
/// </summary>
public partial class TagList
{
    /// <summary>
    /// Gets or sets the tune.
    /// </summary>
    [Parameter] public Tune Tune { get; set; }
}
