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
    /// Gets or sets the event callback for when a tune is dequeued.
    /// </summary>
    [Parameter] public EventCallback<Tune> OnDequeue { get; set; }

    /// <summary>
    /// Gets or sets the event callback for when a tune is enqueued.
    /// </summary>
    [Parameter] public EventCallback<Tune> OnEnqueue { get; set; }

    /// <summary>
    /// Gets or sets the event callback for when the rating of a tune changes.
    /// </summary>
    [Parameter] public EventCallback<float> OnRatingChanged { get; set; }

    /// <summary>
    /// Enqueues the tune.
    /// </summary>
    protected async void DoEnqueue() => await OnEnqueue.InvokeAsync(Tune);

    /// <summary>
    /// Dequeues the tune.
    /// </summary>
    protected async void DoDequeue() => await OnDequeue.InvokeAsync(Tune);

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
            StateHasChanged();
        }
    }
}