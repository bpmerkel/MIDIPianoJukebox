namespace MIDIPianoJukebox.Pages;

/// <summary>
/// Represents a card for a tune in the application.
/// </summary>
public partial class TuneCard
{
    /// <summary>
    /// Gets or sets the tune.
    /// </summary>
    [Parameter] public Tune Tune { get; set; }

    /// <summary>
    /// Gets or sets the event callback for when the rating of a tune changes.
    /// </summary>
    [Parameter] public EventCallback<float> OnRatingChanged { get; set; }

    /// <summary>
    /// Changes the rating of the tune.
    /// </summary>
    /// <param name="rating">The new rating.</param>
    protected async void DoRatingChanged(int rating)
    {
        if (Tune.Rating != rating)
        {
            Tune.Rating = rating;
            await OnRatingChanged.InvokeAsync(rating);
            await InvokeAsync(StateHasChanged);
        }
    }
}