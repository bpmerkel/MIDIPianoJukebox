using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MatBlazor;
using System.Net.Http;
using MIDIPianoJukebox.Data;
using EmbeddedBlazorContent;

namespace MIDIPianoJukebox;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddScoped<HttpClient>();
        services.AddSingleton<JukeboxService>();
        services.AddMatToaster(config =>
        {
            config.Position = MatToastPosition.BottomRight;
            config.PreventDuplicates = true;
            config.NewestOnTop = true;
            config.ShowCloseButton = true;
            config.MaximumOpacity = 95;
            config.VisibleStateDuration = 3000;
        });
        services.Configure<IISServerOptions>(options =>
        {
            options.AutomaticAuthentication = false;
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            // The default HSTS value is 30 days.
            // You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseStaticFiles();
        app.UseEmbeddedBlazorContent(typeof(MatBlazor.BaseMatComponent).Assembly);
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });
    }
}
