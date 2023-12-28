using Microsoft.AspNetCore.Components;

namespace MIDIPianoJukebox.Pages;

public partial class TagList
{
    [Parameter] public Tune Tune { get; set; }
}