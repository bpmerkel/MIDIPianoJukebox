using System.Threading.Tasks;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace MIDIPianoJukebox
{
    /// <summary>
    /// Icons are appropriate for buttons that allow a user to take actions or make a selection, such as adding or removing a star to an item.
    /// </summary>
    public class ToggleButtonBase : BaseMatDomComponent
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Default Button Icon
        /// </summary>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// *Not available yet
        /// </summary>
        [Parameter]
        public string Target { get; set; }

        /// <summary>
        /// Icon to use when Button is clicked
        /// </summary>
        [Parameter]
        public string ToggleIcon { get; set; }

        [Parameter]
        public bool Toggled { get; set; }

        /// <summary>
        /// Button is disabled
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        public ToggleButtonBase()
        {
            ClassMapper
                .Add("mdc-icon-button");
        }

        /// <summary>
        ///  Event occurs when the user clicks on an element.
        /// </summary>
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnMouseDown { get; set; }

        protected async override Task OnFirstAfterRenderAsync()
        {
            await base.OnFirstAfterRenderAsync();
            await JsInvokeAsync<object>("matBlazor.matIconButton.init", Ref);
        }

        protected void OnClickHandler(MouseEventArgs ev)
        {
            Toggled = !Toggled;
            OnClick.InvokeAsync(ev);
        }
    }
}