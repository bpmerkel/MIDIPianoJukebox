using Microsoft.AspNetCore.Components;

namespace MIDIPianoJukebox.Pages;

public partial class TuneCard
{
    [Parameter] public Tune Tune { get; set; }
    [Parameter] public EventCallback<Tune> OnDequeue { get; set; }
    [Parameter] public EventCallback<Tune> OnEnqueue { get; set; }
    [Parameter] public EventCallback<float> OnRatingChanged { get; set; }

    protected async void DoEnqueue() => await OnEnqueue.InvokeAsync(Tune);
    protected async void DoDequeue() => await OnDequeue.InvokeAsync(Tune);

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